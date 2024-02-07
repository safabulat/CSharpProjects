using System.Diagnostics;
using System.Management;

namespace GeminiUIDReader
{
    public partial class Form1 : Form
    {
        private string vidPidSn, getDeviceUIDCommand;

        private TextBox deviceUID, console;

        private System.Windows.Forms.Timer readerConnectionCheckTimer;
        private bool isReaderConnected = false;

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();

            vidPidSn = "0x1394,0xbc00";
            getDeviceUIDCommand = $"-u {vidPidSn} get-property 18";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void InitializeTimer()
        {
            // Inside InitializeComponent method
            this.readerConnectionCheckTimer = new System.Windows.Forms.Timer();
            this.readerConnectionCheckTimer.Interval = 1000; // Set the interval to your desired value (in milliseconds)
            this.readerConnectionCheckTimer.Tick += new EventHandler(this.StatusCheckTimer_Tick);
            this.readerConnectionCheckTimer.Start();
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

        private string GetBlhostPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Path.Combine(currentDirectory, "blhost", "blhost.exe");
        }

        private async Task<(int, string)> ExecuteBlhostCommandAsync(string arguments)
        {
            string blhostPath = GetBlhostPath();

            if (string.IsNullOrEmpty(blhostPath))
            {
                DisplayConsoleMessage("blhost.exe not found. Make sure it exists at the specified location.");
                return (-1, "");
            }

            DisplayConsoleMessage("Executing command: " + arguments);

            string capturedOutput = "";
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
                        DisplayConsoleMessage("blhost output: " + e.Data);
                        capturedOutput += e.Data + Environment.NewLine;
                    }
                };


                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                DisplayConsoleMessage("blhost process exit code:" + process.ExitCode);

                if (process.ExitCode != 0)
                {
                    DisplayConsoleMessage("Error occurred while executing blhost command. Exiting...");
                }

                exitCode = process.ExitCode;
            }
            return (exitCode, capturedOutput);
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
                    isReaderConnected = false;
                    UpdateReaderUID("");
                }
            }
        }

        private async void StatusCheckTimer_Tick(object sender, EventArgs e)
        {
            await CheckForReaderConnectionAsync();
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
                else if (isReaderConnected)
                {
                    isReaderConnected = false;
                    UpdateReaderUID("");
                }

                await Task.Delay(50); // Wait for 50 milliseconds before checking again
            }
        }

        async void ToggleReaderConnected()
        {

            isReaderConnected = true;

            // Get Unique Device ID
            string uniqueDeviceId = await GetUniqueDeviceIdAsync();
            DisplayConsoleMessage($"Reader connected. UID: {uniqueDeviceId}");

            // Update the UI with the Unique Device ID
            UpdateReaderUID(uniqueDeviceId);
        }

        private void UpdateReaderUID(string uid)
        {
            // Update the UI with the Unique Device ID
            deviceUID.Text = $"{uid}";
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
    
    }
}