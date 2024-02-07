using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Management;
using System.Threading.Tasks;
using System.Linq;

namespace GeminiUIDReader
{
    public partial class Form1 : Form
    {
        private string vidPidSn, getDeviceUIDCommand;
        private bool isReaderConnected = false;

        private Label lblStatus;
        private TextBox deviceUID, console;
        private System.Windows.Forms.Timer statusCheckTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();

            vidPidSn = "0x1394,0xbc00";
            getDeviceUIDCommand = $"-u {vidPidSn} get-property 18";
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
            }
            catch (Exception ex)
            {
                DisplayInfoMessage($"An error occurred during initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.statusCheckTimer.Stop();
            this.statusCheckTimer.Dispose();
        }

        private void DisplayInfoMessage(string message)
        {
            lblStatus.Text = message;
        }

        private void DisplayConsoleMessage(string message)
        {
            // Ensure that UI updates are done on the UI thread
            if (console.InvokeRequired)
            {
                if (!console.Text.Contains(message + Environment.NewLine))
                {
                    console.Invoke(new Action(() =>
                    {
                        console.AppendText(message + Environment.NewLine);
                        //SaveUIDsToFile(message.Replace(" ", "").Trim());
                    }));
                }
            }
            else
            {
                if (!console.Text.Contains(message + Environment.NewLine))
                {
                    console.AppendText(message + Environment.NewLine);
                    //SaveUIDsToFile(message.Replace(" ", "").Trim());
                }
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

        private async Task<(int, string)> ExecuteBlhostCommandAsync(string arguments)
        {
            string blhostPath = GetBlhostPath();

            if (string.IsNullOrEmpty(blhostPath))
            {
                DisplayInfoMessage("blhost.exe not found. Make sure it exists at the specified location.");
                return (-1, null);
            }

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
                        capturedOutput += e.Data + Environment.NewLine;
                    }
                };


                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode != 0)
                {
                    DisplayInfoMessage("Error occurred while executing blhost command. Exiting...");
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

            // Get Unique Device ID
            string uniqueDeviceId = await GetUniqueDeviceIdAsync();

            // Update the UI with the Unique Device ID
            if (uniqueDeviceId != null)
            {
                DisplayInfoMessage($"Reader connected. UID: {uniqueDeviceId}");
                DisplayConsoleMessage(uniqueDeviceId.Replace(" ", ""));
                SaveUIDsToFile(uniqueDeviceId.Replace(" ", ""));
                UpdateReaderUID(uniqueDeviceId);
            }
        }

        void ToggleReaderDisconnected()
        {
            isReaderConnected = false;
            DisplayInfoMessage("\nReader disconnected. Waiting for the reader to be connected...\n");
            UpdateReaderUID("");
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

        void SaveUIDsToFile(string uid)
        {
            string filePath = "GeminiReaderUIDs.txt";

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                // If the file doesn't exist, create it and write the UID to it
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(uid);
                }

                DisplayInfoMessage($"UID saved to file.");
            }
            else
            {
                // Check if the UID already exists in the file
                if (!File.ReadAllLines(filePath).Contains(uid))
                {
                    // If the UID doesn't exist, append it to the file
                    using (StreamWriter sw = File.AppendText(filePath))
                    {
                        sw.WriteLine(uid);
                    }

                    DisplayInfoMessage($"UID saved to file.");
                }
            }
        }


    }
}
