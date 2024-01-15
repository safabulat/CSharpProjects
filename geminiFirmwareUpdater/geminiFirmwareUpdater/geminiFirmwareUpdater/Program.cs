using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using Serilog;

namespace GeminiReaderUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            // Update with the correct values for your Gemini reader
            string vidPidSn = "0x1394,0xbc00";

            // Commands
            string getPropertyCommand = $"-u {vidPidSn} get-property 1";
            string resetCommand = $"-u {vidPidSn} reset";
            string firmwareUpdateCommand = $"-u {vidPidSn} receive-sb-file";

            // Configure Serilog to write to the console and a file
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("geminiReaderFW.log")
                .CreateLogger();

            Log.Information("Gemini reader version control script");

            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string blhostPath = Path.Combine(currentDirectory, "blhost", "blhost.exe");
                string firmwareDir = Path.Combine(currentDirectory, "firmware");

                // Check if a .sb file exists in the firmware directory
                string[] firmwareFiles = Directory.GetFiles(firmwareDir, "*.sb");
                if (firmwareFiles.Length == 0)
                {
                    Log.Error("No firmware file (.sb) found in {0}. Exiting...", firmwareDir);
                    Environment.Exit(1);
                }

                string firmwareFile = firmwareFiles[0];
                firmwareUpdateCommand += $" \"{Path.GetFullPath(firmwareFile)}\"";
                Log.Information("Firmware file found: " + firmwareFile);

                Log.Information("Waiting for the reader to be connected...");

                // Wait until the reader is connected
                while(!IsReaderConnected())
                {
                    System.Threading.Thread.Sleep(50); // Wait for 1 second before checking again
                }

                Log.Information("Reader connected. Starting firmware update...");

                // Force reader to stay in the bootloader
                ExecuteBlhostCommand(getPropertyCommand);

                // Wait for the firmware update to finish
                Log.Information("Waiting for firmware update to complete...");

                ExecuteBlhostCommand(firmwareUpdateCommand);

                //do
                //{
                //    Log.Information("go");
                //    ExecuteBlhostCommand(firmwareUpdateCommand);
                //    int firmwareUpdateStatus = Process.GetProcessesByName("blhost")[0].ExitCode;

                //    if (firmwareUpdateStatus == 0)
                //        break;

                //    System.Threading.Thread.Sleep(1000);

                //} while (true);

                Log.Information("Firmware update completed.");

                // Delete the firmware file after a successful update
                Log.Information("Deleting the firmware file...");
                //File.Delete(firmwareFile);

                // Reset the device
                Log.Information("Resetting the device...");
                ExecuteBlhostCommand(resetCommand);

                Log.Information("Gemini reader firmware update completed.");
                Log.Information("Press any key to quit.");
                Console.ReadKey(); // Add this line to keep the console window open

            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Close and flush the log
                Log.CloseAndFlush();
            }
        }

        static void ExecuteBlhostCommand(string arguments)
        {
            // Set the path to blhost.exe
            string currentDirectory = Directory.GetCurrentDirectory();
            string blhostPath = Path.Combine(currentDirectory, "blhost", "blhost.exe");

            // Check if blhost.exe exists
            if (!File.Exists(blhostPath))
            {
                Log.Error("blhost.exe not found at {BlhostPath}. Make sure it exists at the specified location.", blhostPath);
                return;
            }

            Log.Information("Executing command: " + arguments);

            // Set up the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = blhostPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, e) => Log.Information("blhost output: {Output}", e.Data);
                process.ErrorDataReceived += (sender, e) => Log.Information("blhost error: {Error}", e.Data);

                // Start the process
                process.Start();

                // Begin asynchronous read operations on the output and error streams
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for the process to exit
                process.WaitForExit();

                // Log information about the blhost process
                Log.Information("blhost process exit code: {ExitCode}", process.ExitCode);

                // Check if the process exit code is non-zero
                if (process.ExitCode != 0)
                {
                    Log.Error("Error occurred while executing blhost command. Exiting...");
                    Environment.Exit(1);
                }
            }
        }


        static bool IsReaderConnected()
        {
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                string deviceId = device.GetPropertyValue("DeviceID").ToString();

                // Modify the condition based on your reader's specific Vendor ID and Product ID
                if (deviceId.Contains("VID_1394") && deviceId.Contains("PID_BC00"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
