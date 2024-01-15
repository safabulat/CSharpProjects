using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;
using System.Timers;

namespace GeminiReaderUpdaterGUI
{
    public partial class Form1 : Form
    {
        private string vidPidSn, firmwareFile;

        private string getPropertyCommand, getDeviceUIDCommand, resetCommand, firmwareUpdateCommand;

        private bool isReaderConnected = false;
        private bool isFirmwareUpdateInProgress = false;

        private Label lblStatus, lblUpdateStatus;
        private Button btnUpdateFirmware;
        private TextBox deviceUID, console;
        private Panel status;
        private System.Windows.Forms.Timer statusCheckTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();

            vidPidSn = "0x1394,0xbc00";
            getPropertyCommand = $"-u {vidPidSn} get-property 1";
            getDeviceUIDCommand = $"-u {vidPidSn} get-property 18";
            resetCommand = $"-u {vidPidSn} reset";
            firmwareUpdateCommand = $"-u {vidPidSn} receive-sb-file";

            // Configure Serilog to write to the console and a file
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                //.WriteTo.File("geminiReaderFW.log")
                .CreateLogger();
        }

        private void InitializeTimer()
        {
            // Inside InitializeComponent method
            this.statusCheckTimer = new System.Windows.Forms.Timer();
            this.statusCheckTimer.Interval = 1000; // Set the interval to your desired value (in milliseconds)
            this.statusCheckTimer.Tick += new EventHandler(this.StatusCheckTimer_Tick);
            this.statusCheckTimer.Start();
        }

        private async void StatusCheckTimer_Tick(object sender, EventArgs e)
        {
            await CheckForReaderConnectionAsync();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                DisplayInfoMessage("Waiting for the reader to be connected...");
                //CheckForReaderConnection();
                //CheckForReaderConnectionAsync();

            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred during initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.statusCheckTimer.Stop();
            this.statusCheckTimer.Dispose();
        }

        private void btnUpdateFirmware_Click(object sender, EventArgs e)
        {
            try
            {
                FirmwareUpdate();
            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred during initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DisplayInfoMessage(string message)
        {
            lblStatus.Text = message;
        }

        private void DisplayUpdateStatusMessage(string message)
        {
            lblUpdateStatus.Text = message;
        }
        private void DisplayConsoleMessage(string message)
        {
            // Ensure that UI updates are done on the UI thread
            if (console.InvokeRequired)
            {
                console.Invoke(new Action(() => console.AppendText(message + Environment.NewLine)));
            }
            else
            {
                console.AppendText(message + Environment.NewLine);
            }
        }

        private void clearConsole_Click(object sender, EventArgs e)
        {
            console.Clear();
        }

        private void UpdateReaderUID(string uid)
        {
            // Update the UI with the Unique Device ID
            deviceUID.Text = $"{uid}";
        }

        private void status_Paint(object sender, PaintEventArgs e)
        {
            // Draw the circle based on the status
            using (SolidBrush brush = new SolidBrush(isReaderConnected ? Color.Green : Color.Red))
            {
                e.Graphics.FillEllipse(brush, 0, 0, status.Width, status.Height);
            }
        }

        private async Task<string> GetUniqueDeviceId()
        {
            // Execute blhost command to get Unique Device ID
            var (exitCode, output) = await ExecuteBlhostCommandAsync(getDeviceUIDCommand);

            // Parse the output to get the Unique Device ID
            string deviceId = ParseOutput(output, "Unique Device ID");

            return deviceId;
        }

        private async Task<string> GetUniqueDeviceIdAsync()
        {
            var (exitCode, output) = await ExecuteBlhostCommandAsync(getDeviceUIDCommand);

            // Parse the output to get the Unique Device ID
            string deviceId = ParseOutput(output, "Unique Device ID");

            return deviceId;
        }

        private string ParseOutput(string blhostOutput, string desiredParse)
        {
            // Split the output into lines
            string[] lines = blhostOutput.Split('\n');

            // Find the line containing Unique Device ID
            string uniqueDeviceIdLine = lines.FirstOrDefault(line => line.Contains(desiredParse));

            // Extract the Unique Device ID value
            string deviceId = uniqueDeviceIdLine?.Split('=')[1].Trim();

            return deviceId;
        }

        private void GetUpdateFile(string readerUID)
        {
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string firmwareDir = Path.Combine(currentDirectory, "firmware");

                // Get file contains that UID
                string[] firmwareFiles = Directory.GetFiles(firmwareDir, $"*{readerUID.Replace(" ", "")}*FW.sb");
                if (firmwareFiles.Length == 0)
                {
                    // Disable the firmware update button
                    btnUpdateFirmware.Enabled = false;

                    DisplayInfoMessage("No firmware file (.sb) found.");
                    DisplayConsoleMessage("No firmware file (.sb) found.");
                    return;
                }

                firmwareFile = firmwareFiles[0];
                firmwareUpdateCommand = firmwareUpdateCommand + $" \"{Path.GetFullPath(firmwareFile)}\"";

                DisplayInfoMessage("Firmware file found: " + Path.GetFileName(firmwareFile));
                DisplayConsoleMessage("Firmware file found: " + Path.GetFileName(firmwareFile));

                DisplayInfoMessage("Firmware file path: " + Path.GetFullPath(firmwareFile));
                DisplayConsoleMessage("Firmware file path: " + Path.GetFullPath(firmwareFile));

                // Enable the firmware update button
                btnUpdateFirmware.Enabled = true;
            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async void RestartReader()
        {          
            DisplayConsoleMessage("Resetting the device...");
            var (exitCode, output) = await ExecuteBlhostCommandAsync(resetCommand);
        }

        private async void FirmwareUpdate()
        {
            try
            {
                // Check if a firmware update is already in progress
                if (isFirmwareUpdateInProgress)
                {
                    DisplayConsoleMessage("Firmware update is already in progress. Please wait...");
                    return;
                }

                // Set the flag to indicate that a firmware update is in progress
                isFirmwareUpdateInProgress = true;
                btnUpdateFirmware.Enabled = false;

                // Wait for the firmware update to finish
                DisplayConsoleMessage("Waiting for firmware update to complete...");
                DisplayUpdateStatusMessage("Update in progress. Do NOT disconnect the reader.");

                // Ensure the firmwareUpdateCommand contains the correct file path only once
                //string commandWithFilePath = $"{firmwareUpdateCommand} \"{Path.GetFullPath(firmwareFile)}\"";
                var (exitCode, output) = await ExecuteBlhostCommandAsync(firmwareUpdateCommand);

                if(exitCode != 0)
                {
                    DisplayConsoleMessage($"An error occurred. Resetting the operation.");
                    ToggleReaderDisconnected();
                    return;
                }

                DisplayConsoleMessage("Firmware update completed.");
                DisplayUpdateStatusMessage("Firmware update finished.");
                // Delete the firmware file after a successful update
                DisplayConsoleMessage("Deleting the firmware file...");
                //File.Delete(firmwareFile);

                // Reset the device
                RestartReader();

                DisplayConsoleMessage("Gemini reader firmware update completed.");

                await Task.Delay(3000); //Don't catch the same device after restart
                DisplayUpdateStatusMessage("");
            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Reset the flag when the firmware update process is complete or encounters an error
                isFirmwareUpdateInProgress = false;
            }
        }

        private async Task<(int, string)> ExecuteBlhostCommandAsync(string arguments)
        {
            string blhostPath = GetBlhostPath();

            if (string.IsNullOrEmpty(blhostPath))
            {
                Log.Error("blhost.exe not found. Make sure it exists at the specified location.");
                return (-1, null);
            }

            Log.Information("Executing command: " + arguments);
            DisplayConsoleMessage("Executing command: " + arguments);

            string capturedOutput = null;
            int exitCode = -1;

            using (Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = blhostPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.Information("blhost output: {Output}", e.Data);
                        DisplayConsoleMessage("blhost output: " + e.Data);
                        capturedOutput += e.Data + Environment.NewLine;
                    }
                };

                process.ErrorDataReceived += (sender, e) => Log.Information("blhost error: {Error}", e.Data);

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                Log.Information("blhost process exit code: {ExitCode}", process.ExitCode);
                DisplayConsoleMessage("blhost process exit code:" + process.ExitCode);

                if (process.ExitCode != 0)
                {
                    Log.Error("Error occurred while executing blhost command. Exiting...");
                    DisplayConsoleMessage("Error occurred while executing blhost command. Exiting...");
                }

                exitCode = process.ExitCode;
            }
            return (exitCode, capturedOutput);
        }

        

        private string GetBlhostPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Path.Combine(currentDirectory, "blhost", "blhost.exe");
        }




        private async Task CheckForReaderConnectionAsync()
        {
            bool isConnected = await IsReaderConnectedAsync();

            status.Invalidate(); // Trigger the Paint event

            if (isConnected)
            {
                if (!isReaderConnected)
                {
                    ToggleReaderConnected();
                }
            }
            else
            {
                if (isReaderConnected)
                {
                    ToggleReaderDisconnected();
                }
            }
        }

        async void ToggleReaderConnected()
        {

            isReaderConnected = true;

            // Force reader to stay in the bootloader
            var (exitCode, output) = await ExecuteBlhostCommandAsync(getPropertyCommand);

            if (exitCode == 0)
            {
                // Get Unique Device ID
                string uniqueDeviceId = await GetUniqueDeviceIdAsync();
                DisplayInfoMessage($"Reader connected. UID: {uniqueDeviceId}");
                DisplayConsoleMessage($"Reader connected. UID: {uniqueDeviceId}");

                // Update the UI with the Unique Device ID
                UpdateReaderUID(uniqueDeviceId);

                // Search for the firmware file with the Unique Device ID
                GetUpdateFile(uniqueDeviceId);
            }
        }

        void ToggleReaderDisconnected()
        {
            isReaderConnected = false;
            status.Invalidate(); // Trigger the Paint event
            DisplayInfoMessage("\nReader disconnected. Waiting for the reader to be connected...\n");
            DisplayConsoleMessage("\nReader disconnected. Waiting for the reader to be connected...\n");

            UpdateReaderUID("");
            btnUpdateFirmware.Enabled = false;
            firmwareUpdateCommand = $"-u {vidPidSn} receive-sb-file";
        }

        private async Task<bool> IsReaderConnectedAsync()
        {
            while (true)
            {
                bool isConnected = await Task.Run(() =>
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
                });

                if (isConnected)
                {
                    return true;
                }
                else if(isReaderConnected)
                {
                    ToggleReaderDisconnected();
                }

                await Task.Delay(50); // Wait for 50 milliseconds before checking again
            }
        }
    }
}
