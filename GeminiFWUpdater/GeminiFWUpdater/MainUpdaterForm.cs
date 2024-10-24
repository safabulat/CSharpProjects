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
using System.Runtime.InteropServices;

using GeminiFWUpdater;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;
using static GeminiFWUpdater.GeminiLib;

namespace GeminiReaderUpdaterGUI
{
    struct FirmwareInfo
    {
        public string MainFirmwareVersiyon;
        public string IssueFirmwareVersiyon;
        public string FirmwareBuildTime;

        public override string ToString()
        {
            return MainFirmwareVersiyon + "_" + IssueFirmwareVersiyon + "_" + FirmwareBuildTime;
        }
    }

    public partial class MainUpdaterForm : Form
    {
        #region Program Variables
        //Flags
        private CancellationTokenSource _cancellationTokenSource;
        private bool isForceBLLoopRunning = false;
        private TaskCompletionSource<bool> isBootloaderActivated;
        private TaskCompletionSource<bool> isFirmwareUpdateInProgress;

        //UI Elements
        private Label programInfoLabel, lblUpdateStatus;
        private TextBox deviceUIDTextBox, consoleTextBox;
        private ComboBox comboBoxPorts;
        private Timer readerStateTimer;

        //Global Variables
        AppConfigParameters appCParams = new AppConfigParameters();
        GeminiLib geminiHandler = null;
        HttpHelper httpHelper = null;
        string geminiReaderUID;
        string geminiReaderFWMainVersion, geminiReaderFWIssueVersion, geminiReaderFWBuildTime;
        string geminiReaderLibraryName = "GemTagAPIThin64.dll";
        FirmwareInfo old_firmwareInfo, new_firmwareInfo;
        string connectionComPort;

        //BLHost Variables
        private string vidPidSn, firmwareFile;
        private string getPropertyCommand, getDeviceUIDCommand, resetCommand, firmwareUpdateCommand;

        //Path Variables
        private string blhostPath, geminiReaderLibrariesPath, firmwareFolderPath;
        #endregion

        #region EntryPoint
        public MainUpdaterForm()
        {
            InitializeComponent();
            LoadProgramVariables();
            InitializeTimer();
        }
        #endregion

        #region LoadProgramStuff
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                DisplayInfoMessage("Waiting for the reader to be connected...");

            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred during initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }
        private void InitializeTimer()
        {
            // Inside InitializeComponent method
            readerStateTimer = new Timer(500); // timer ms
            readerStateTimer.Elapsed += OnTimedEvent;
            readerStateTimer.AutoReset = true;
            readerStateTimer.Start();
        }
        private void LoadProgramVariables()
        {
            string json = File.ReadAllText(GetFolderAndFilePath("conf", "appConf.json"));
            appCParams = JsonConvert.DeserializeObject<AppConfigParameters>(json);
            appCParams.printParams();

            //Print Params Working. TODO: make all parametric !!!!!!!!!!!!!!!!!!!!!!!!!
            httpHelper = new HttpHelper(appCParams.ServiceIP, appCParams.ServicePORT, appCParams.ServiceEndpoint, appCParams.ServiceTimeout);

            CreateFolderIfNotExists("firmware");
            blhostPath = GetFolderAndFilePath("blhost", "blhost.exe");
            firmwareFolderPath = GetFolderAndFilePath("firmware");
            geminiReaderLibrariesPath = GetFolderAndFilePath("geminiLib");

            geminiHandler = new GeminiLib(geminiReaderLibrariesPath, geminiReaderLibraryName);
            geminiHandler.currentReaderState = ReaderState.DisconnectedAndReady;

            vidPidSn = "0x1394,0xbc00";
            getPropertyCommand = $"-u {vidPidSn} get-property 1";
            getDeviceUIDCommand = $"-u {vidPidSn} get-property 18";
            resetCommand = $"-u {vidPidSn} reset";
            firmwareUpdateCommand = $"-u {vidPidSn} receive-sb-file ";

            // Configure Serilog to write to the console and a file
            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.Console()
            //    //.WriteTo.File("geminiReaderFW.log")
            //    .CreateLogger();

            // Get available COM ports
            string[] ports = SerialPort.GetPortNames();

            // Clear ComboBox
            comboBoxPorts.Items.Clear();

            if (comboBoxPorts.InvokeRequired)
            {
                comboBoxPorts.Invoke(new Action(() => comboBoxPorts.Items.AddRange(ports)));
            }
            else
            {
                comboBoxPorts.Items.AddRange(ports);
            }
            if (ports.Length > 0)
            {
                comboBoxPorts.SelectedIndex = ports.Length -1;
            }

            UpdateReaderUID("", "", "", "");
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.statusCheckTimer.Stop();
            //this.statusCheckTimer.Dispose();
        }
        #endregion

        #region ButtonClicks
        private void toggleTheReaderConnectionAppBTN_Click(object sender, EventArgs e) // Toggle This
        {
            if(geminiHandler.currentReaderState == ReaderState.DisconnectedAndReady)
            {
                ConnectToReader();
            }
            else if(geminiHandler.currentReaderState >= ReaderState.ConnectedToApp)
            {
                DisconnectFromReader();
            }
        }
        private async void forceTheBootloaderBTN_Click(object sender, EventArgs e)
        {
            //Toggle Force BL
            if (isForceBLLoopRunning)
            {
                // Stop the loop if it's running
                ForceBootloader_StopLoop();
            }
            else
            {
                geminiHandler.currentReaderState = ReaderState.ConnectingToBootloader;
                DisplayInfoMessage("Sending Soft Reset");
                int ret = -1;
                while (ret != 0)
                {
                    ret = geminiHandler.PerformSoftReset();
                    if (ret != 0)
                        Console.WriteLine("ret: " + ret);
                }
                DisplayConsoleMessage("Soft Reset Success!!");

                // Start the loop if it's not running
                await ForceBootloader_StartLoopAsync();
            }
        }
        private void clearConsoleBTN_Click(object sender, EventArgs e)
        {
            consoleTextBox.Clear();
        }
        private void reloadComPortsBTN_Click(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            // Clear ComboBox
            comboBoxPorts.Items.Clear();

            if (comboBoxPorts.InvokeRequired)
            {
                comboBoxPorts.Invoke(new Action(() => comboBoxPorts.Items.AddRange(ports)));
            }
            else
            {
                comboBoxPorts.Items.AddRange(ports);
            }
            if (ports.Length > 0)
            {
                comboBoxPorts.SelectedIndex = ports.Length - 1;
            }
        }
        private void disconnectFromTheBootloaderBTN_Click(object sender, EventArgs e)
        {
            RestartReader();
            Thread.Sleep(1000);
        }
        private void readTestCardUIDBTN_Click(object sender, EventArgs e)
        {
            DisplayConsoleMessage("Reading Card UID:");
            readTestCardUIDBTN.Enabled = false;
            string cardUID = "";
            if (geminiHandler == null)
                geminiHandler = new GeminiLib(geminiReaderLibrariesPath, geminiReaderLibraryName);
            int attemps = 0;
            while (attemps < 50)
            {
                int ret = geminiHandler.GetCardUID(out cardUID);
                if (ret == 0)
                {
                    DisplayConsoleMessage("Card UID: " + cardUID);
                    break;
                }
                else
                {
                    Console.WriteLine(ret + " - " + cardUID);
                    attemps++;
                }
                Thread.Sleep(100);
            }
            readTestCardUIDBTN.Enabled = true;
        }
        #endregion

        #region UIRealtedUpdates
        private void DisplayInfoMessage(string message)
        {
            programInfoLabel.Text = message;
        }
        private void DisplayReaderState(string message)
        {
            readerStatusLabel.Text = message;
        }
        private void DisplayUpdateStatusMessage(string message) // Delete this
        {
            lblUpdateStatus.Text = message;
        }
        private void DisplayConsoleMessage(string message)
        {
            // Ensure that UI updates are done on the UI thread
            if (consoleTextBox.InvokeRequired)
            {
                consoleTextBox.Invoke(new Action(() => consoleTextBox.AppendText(message + Environment.NewLine)));
            }
            else
            {
                consoleTextBox.AppendText(message + Environment.NewLine);
            }
        }
        private void UpdateReaderUID(string mainFWV, string issueFWV, string fwBuildDateTime, string uid)
        {
            deviceUIDTextBox.Text = $"MainFWVersion:\t{mainFWV}" + Environment.NewLine +
                             $"IssFWVersion: \t{issueFWV}" + Environment.NewLine +
                             $"FW BuildTime: \t{fwBuildDateTime}" + Environment.NewLine +
                             $"Reader UID  : \t{uid}" + Environment.NewLine ;
        }
        private void UpdateFirmwareInfo(string mainFWV, string issueFWV, string fwBuildDateTime)
        {
            //if old null-> old , ifnot new
            if (old_firmwareInfo.MainFirmwareVersiyon == string.Empty)
            {
                old_firmwareInfo.MainFirmwareVersiyon = mainFWV;
                old_firmwareInfo.IssueFirmwareVersiyon = issueFWV;
                old_firmwareInfo.FirmwareBuildTime = fwBuildDateTime;
            }
            else
            {
                new_firmwareInfo.MainFirmwareVersiyon = mainFWV;
                new_firmwareInfo.IssueFirmwareVersiyon = issueFWV;
                new_firmwareInfo.FirmwareBuildTime = fwBuildDateTime;
            }
        }


        private void UpdateUIBasedOnState(ReaderState state)
        {
            Console.WriteLine("Reader State: " + state);
            DisplayReaderState("Reader State: " + state.ToString());

            switch (state)
            {
                case ReaderState.NotAttached:
                case ReaderState.ConnectingToApp:
                case ReaderState.FirmwareUpdateInProgress:
                case ReaderState.FailedToFirmwareUpdate:
                    connectToTheReaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = false;
                    break;

                case ReaderState.DisconnectedAndReady:
                    connectToTheReaderBTN.Enabled = true;
                    connectToTheReaderBTN.Text = "Connect";
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = false;
                    break;

                case ReaderState.FailedToConnectApp:
                    readerStateTimer.Stop();
                    connectToTheReaderBTN.Enabled = true;
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = false;

                    if (MessageBox.Show("Choose Correct Port And Try Again.", "Can not connected to selected Port.", MessageBoxButtons.OK)  == DialogResult.OK)
                    {
                        geminiHandler.currentReaderState = ReaderState.DisconnectedAndReady;
                        readerStateTimer.Start();
                    }

                    break;

                case ReaderState.ConnectedToApp:
                    connectToTheReaderBTN.Enabled = true;
                    connectToTheReaderBTN.Text = "Disconnect";
                    forceTheBootloaderBTN.Enabled = true;
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = true;
                    break;

                case ReaderState.ConnectingToBootloader:
                    connectToTheReaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Enabled = true;
                    forceTheBootloaderBTN.Text = "Stop Force BL";
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = false;
                    break;

                case ReaderState.FailedToConnectBootloader:
                    readerStateTimer.Stop();
                    if (MessageBox.Show("Restart the Application and Try Again?", "Can't connect to the Bootloader.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        RestartTheApplication();
                    }
                    //RestartTheApplication();
                    break;

                case ReaderState.ConnectedToBootloader:
                    connectToTheReaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Text = "Connect BL";
                    disconnectFromTheBootloaderBTN.Enabled = true;
                    readTestCardUIDBTN.Enabled = false;
                    break;

                case ReaderState.NoFirmwareFileFound:
                    readerStateTimer.Stop();
                    connectToTheReaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = true;
                    readTestCardUIDBTN.Enabled = false;
                    if (MessageBox.Show("Disconnect & Detach The reader...", "Firmware file not Found.", MessageBoxButtons.OK) == DialogResult.OK)
                    {
                        //readerStateTimer.Start();
                    }
                    break;

                case ReaderState.FirmwareFileFound:
                    readerStateTimer.Stop();
                    connectToTheReaderBTN.Enabled = false;
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = true;
                    readTestCardUIDBTN.Enabled = false;
                    if (MessageBox.Show("Update the Firmware?", "Firmware file Found.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        DisplayConsoleMessage("Firmware Update Starts.");
                        geminiHandler.currentReaderState = ReaderState.FirmwareUpdateInProgress;
                        readerStateTimer.Start();
                        FirmwareUpdate();
                    } 
                    break;

                case ReaderState.FirmwareUpdateFinished:
                    connectToTheReaderBTN.Enabled = true;
                    forceTheBootloaderBTN.Enabled = false;
                    disconnectFromTheBootloaderBTN.Enabled = false;
                    readTestCardUIDBTN.Enabled = false;
                    break;


                default:
                    break;
            }
        }
        #endregion

        #region FLOW
        private async void StartApplicationFlow(string comPort, int baudRate)
        {
            InitReader(comPort, baudRate);
            DisplayConsoleMessage("Reader is ready for Bootloader.");

            await WaitForBootloaderFlagAsync();
            Thread.Sleep(200);

            DisplayConsoleMessage("BL Connected, Retrieving reader UID.");

            await GetReaderUID();
            Thread.Sleep(200);

            DisplayConsoleMessage(geminiReaderUID);

            if (!await CheckFirmwareFile())
            {
                geminiHandler.currentReaderState = ReaderState.NoFirmwareFileFound;
                DisplayInfoMessage("No Firmware File, Quit.");
                return;
            }
            else
            {
                geminiHandler.currentReaderState = ReaderState.FirmwareFileFound;
            }
            Thread.Sleep(200);

            DisplayInfoMessage("Start Firmware Update ?");

            await WaitForFirmwareUpdateFlagAsync();
            Thread.Sleep(1000);

            DisplayConsoleMessage("Firmware Update Finished. You Can Detach the reader.");
            //Thread.Sleep(100);
            //Clean All the buffers, re-run the program.
        }
        public void InitReader(string comPort, int baudRate)
        {
            DisplayConsoleMessage("Connecting the reader(app)");

            if(geminiHandler == null)
                geminiHandler = new GeminiLib(geminiReaderLibrariesPath, geminiReaderLibraryName);

            bool isConnected = false, getVersFlag = false;
            string readerVersM = "", readerVersI = "";
            int attempts = 0;
            while (attempts < 4)
            {
                if (!isConnected)
                {
                    isConnected = geminiHandler.ConnectReaderSeq(comPort, baudRate);
                    Thread.Sleep(50);
                }

                if (!getVersFlag)
                {
                    getVersFlag = geminiHandler.GetReaderVersion(ref readerVersM, ref readerVersI);
                    Thread.Sleep(50);
                }

                if (isConnected && getVersFlag)
                {
                    Console.WriteLine("VersI: " + readerVersI);
                    Console.WriteLine("VersM: " + readerVersM);
                    break;
                }
                else
                {
                    attempts++;
                }

            }
            if(!isConnected || !getVersFlag)
            {
                geminiHandler.currentReaderState = ReaderState.FailedToConnectApp;
                return;
            }

            DisplayInfoMessage("Reader Connected (Application/Reader Mode).");

            ParseFirmwareInfo(readerVersM, readerVersI);

            UpdateReaderUID(geminiReaderFWMainVersion, geminiReaderFWIssueVersion, geminiReaderFWBuildTime, geminiReaderUID);
            UpdateFirmwareInfo(geminiReaderFWMainVersion, geminiReaderFWIssueVersion, geminiReaderFWBuildTime);

            Thread.Sleep(100);
            geminiHandler.currentReaderState = ReaderState.ConnectedToApp;
        }
        public void ConnectToReader()
        {
            connectionComPort = comboBoxPorts.SelectedItem.ToString();
            Console.WriteLine("Connecting to " + connectionComPort + ":" + 115200);
            geminiHandler.currentReaderState = ReaderState.ConnectingToApp;
            StartApplicationFlow(connectionComPort, 115200);
        }
        public void DisconnectFromReader()
        {
            geminiHandler.currentReaderState = ReaderState.ConnectingToApp;
            if (geminiHandler.DisconnectReaderSeq())
            {
                geminiHandler.currentReaderState = ReaderState.DisconnectedAndReady;
            }
            else
            {
                Console.WriteLine("Can'T disconnect?");
            }
        }
        public async Task<bool> CheckFirmwareFile()
        {
            bool ret = CheckLocalFirmwareFile();
            if (!ret)
            {
                ret = await OnlineCheckFirmwareFile();
            }
            if (firmwareFile == "") throw new Exception("No Firmware File");
            
            return ret;
        }
        public bool CheckLocalFirmwareFile()
        {
            // Ensure the firmware folder exists
            if (string.IsNullOrEmpty(firmwareFolderPath) || !Directory.Exists(firmwareFolderPath))
            {
                DisplayConsoleMessage("Firmware folder does not exist or is invalid.");
                return false;
            }

            // Search for files in the folder that contain the UID and have .sb extension
            string[] files = Directory.GetFiles(firmwareFolderPath, "*.sb");

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                // Check if the file name contains the UID
                if (fileName.Contains(geminiReaderUID))
                {
                    firmwareFile = fileName; // Set the global firmwareFileName with the found file
                    DisplayConsoleMessage("Firmware file found: " + firmwareFolderPath);
                    return true;
                }
            }

            // No file found
            DisplayConsoleMessage("No local firmware file found containing the UID.");
            return false;
        }
        public async Task<bool> OnlineCheckFirmwareFile()
        {
            DisplayConsoleMessage("Requesting for FirmwareFile to " + appCParams.ServiceIP + ":" + appCParams.ServicePORT + "/" + appCParams.ServiceEndpoint);

            var postData = new
            {
                uid = geminiReaderUID,
                geminiVersion = geminiReaderFWMainVersion,
                geminiIssueNumber = geminiReaderFWIssueVersion,
                buildDate = geminiReaderFWMainVersion
            };

            DisplayConsoleMessage(postData.ToString());

            if (httpHelper == null)
            {
                Console.WriteLine("Error: httpHelper is not initialized.");
                return false;
            }

            string jsonResponse = await httpHelper.PostJsonAsync(postData);

            if (jsonResponse == null)
            {
                DisplayInfoMessage("HTTP Request Failed.");
                DisplayConsoleMessage("HTTP Request Failed.");
                return false;
            }

            DisplayConsoleMessage($"POST response: {jsonResponse}");

            VersionManagerResponseModel responseModel = JsonConvert.DeserializeObject<VersionManagerResponseModel>(jsonResponse);
            //responseModel.displayResponse();

            if (responseModel.statusCode != 0)
            {
                DisplayInfoMessage("HTTP Request Failed.");
                DisplayConsoleMessage("HTTP Request Failed.");
                return false;
            }
            if (!responseModel.isUpdateRequired)
            {
                DisplayInfoMessage("Latest Firmware Already Installed.");
                DisplayConsoleMessage("Latest Firmware Already Installed. No newer versiyon to update.");
                return false;
            }

            return await DownloadFirmwareFile(responseModel.url, responseModel.mD5);
        }
        public async Task<bool> DownloadFirmwareFile(string url, string md5)
        {
            DisplayInfoMessage("Downloading the firmware file");

            firmwareFile = GetFileNameFromURL(url);
            
            string downloadLocation = Path.Combine(firmwareFolderPath, firmwareFile);

            Console.WriteLine("url: " + url);
            
            Console.WriteLine("dwl: " + downloadLocation);

            await httpHelper.DownloadFileAsync(url, downloadLocation);

            DisplayConsoleMessage("File downloaded successfully.");
            Thread.Sleep(200);

            bool ret = CompareMD5Files(downloadLocation, md5);

            if (ret)
            {
                //TODO: progress to the firmware update
                DisplayConsoleMessage("md5 Checked. Ready for firmware update.");
                return true;
            }
            else
            {
                //TODO: nop
                DisplayConsoleMessage("md5 didn't match. File Not-Ready for firmware update.");
                return false;
            }
        }
        private async Task WaitForFirmwareUpdateFlagAsync()
        {
            isFirmwareUpdateInProgress = new TaskCompletionSource<bool>();
            await isFirmwareUpdateInProgress.Task;
        }
        private async Task WaitForBootloaderFlagAsync()
        {
            isBootloaderActivated = new TaskCompletionSource<bool>();
            await isBootloaderActivated.Task;
        }
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                ChangeStateOnPhysicalConnection();
                UpdateUIBasedOnState(geminiHandler.currentReaderState);
            });
        }
        private void RestartTheApplication()
        {
            Application.Restart();
            Environment.Exit(0);
        }
        #endregion

        #region BLHOST_CMD
        private async Task<(int, string)> ExecuteBlhostCommandAsync(string arguments, bool isLooping = false, int attempts = 0, int taskDelay = 1)
        {
            DisplayConsoleMessage("Excecuting BLHost CMD: " + arguments);
            bool isAttempsReacheced = false;
            int attemptsIndex = 0;
            if(attempts == 0) isAttempsReacheced = true;

            if (string.IsNullOrEmpty(blhostPath))
            {
                Log.Error("blhost.exe not found. Make sure it exists at the specified location.");
                return (-1, null);
            }

            int exitCode = -1;
            string capturedOutput = null;

            do
            {
                using (Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = blhostPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,  // Only redirect if not fast
                        RedirectStandardError = true,   // Only redirect if not fast
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

                    process.ErrorDataReceived += (sender, e) => Log.Information("blhost error: {Error}", e.Data);

                    // Start the process
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for the process to finish execution
                    await Task.Run(() => process.WaitForExit());
                    await Task.Delay(taskDelay);

                    // Display or log exit code regardless of isFast
                    DisplayConsoleMessage("blhost process exit code:" + process.ExitCode);

                    exitCode = process.ExitCode;

                    if (exitCode != 0)
                    {
                        DisplayConsoleMessage("Error occurred while executing blhost command. Exiting...");
                    }
                }
                if (++attemptsIndex > attempts) isAttempsReacheced = true;
                
            } while (isLooping && exitCode != 0 && !isAttempsReacheced);  // Loop if isLooping is true and exitCode is not 0

            return (exitCode, capturedOutput);
        }
        private async Task ForceBootloader_StartLoopAsync()
        {
            DisplayInfoMessage("Forcing the Bootloader.");
            // Set the running flag to true
            isForceBLLoopRunning = true;


            // Create a new CancellationTokenSource for this run
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Run the loop asynchronously in a separate thread
                await Task.Run(() => ForceBootloaderMode_Loop(_cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                DisplayConsoleMessage("Loop canceled.");
            }
        }
        private void ForceBootloader_StopLoop()
        {  
            _cancellationTokenSource?.Cancel();
            geminiHandler.currentReaderState = ReaderState.FailedToConnectBootloader;
            Thread.Sleep(100);
        }
        private async void ForceBootloaderMode_Loop(CancellationToken token)
        {
            int exitCode;
            string output;
            int attempt = 1;
            try
            {
                while (true)
                {
                    // Check if cancellation is requested
                    token.ThrowIfCancellationRequested();

                    // Execute your async command
                    (exitCode, output) = await ExecuteBlhostCommandAsync(getPropertyCommand, false);
                    if (exitCode == 0)
                    {
                        isBootloaderActivated?.SetResult(true);
                        geminiHandler.currentReaderState = ReaderState.ConnectedToBootloader;
                        break;
                    }
                    else
                    {
                        DisplayConsoleMessage("attempt: " + attempt++);
                    }
                    await Task.Delay(appCParams.TaskDelayTime, token);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Task was canceled inside the task.");
            }
            DisplayConsoleMessage("Loop finished.");  
        }
        private async Task GetReaderUID()
        {
            // Execute blhost command to get Unique Device ID
            var (exitCode, output) = await ExecuteBlhostCommandAsync(getDeviceUIDCommand,true, 50, appCParams.TaskDelayTime);
            if(exitCode == 0)
            {
                geminiReaderUID = ParseOutput(output, "Unique Device ID");
                UpdateReaderUID(geminiReaderFWMainVersion, geminiReaderFWIssueVersion, geminiReaderFWBuildTime, geminiReaderUID);
            }
            else
            {
                DisplayInfoMessage("Error Getting Reader UID");
                throw new Exception("No Reader UID");
            }
            // Parse the output to get the Unique Device ID
            
        }
        private async void FirmwareUpdate()
        {
            try
            {
                // Wait for the firmware update to finish
                DisplayConsoleMessage("Waiting for firmware update to complete...");
                DisplayUpdateStatusMessage("Update in progress. Do NOT disconnect the reader.");

                // Ensure the firmwareUpdateCommand contains the correct file path only once
                string updateFirmwareCommandWithFilePath = firmwareUpdateCommand + Path.Combine(firmwareFolderPath, firmwareFile);

                var (exitCode, output) = await ExecuteBlhostCommandAsync(updateFirmwareCommandWithFilePath, true, 5, 500);

                if (exitCode != 0)
                {
                    DisplayConsoleMessage($"An error occurred. Resetting the operation.");
                    geminiHandler.currentReaderState = ReaderState.FailedToFirmwareUpdate;
                    //ToggleReaderDisconnected();
                    return;
                }

                DisplayConsoleMessage("Firmware update completed.");
                geminiHandler.currentReaderState = ReaderState.FirmwareUpdateFinished;
                DisplayUpdateStatusMessage("Firmware update finished.");
                // Delete the firmware file after a successful update
                DisplayConsoleMessage("Deleting the firmware file...");
                //File.Delete(firmwareFile);

                // Reset the device
                RestartReader();

                DisplayConsoleMessage("Gemini reader firmware update completed.");

                //await Task.Delay(2000); //Don't catch the same device after restart
                DisplayUpdateStatusMessage("");
            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Reset the flag when the firmware update process is complete or encounters an error
                isFirmwareUpdateInProgress.SetResult(true);
            }
        }
        private async void RestartReader()
        {
            DisplayConsoleMessage("Resetting the device...");
            var (exitCode, output) = await ExecuteBlhostCommandAsync(resetCommand, false, 0, appCParams.TaskDelayTime);
            if (exitCode != 0)
            {
                DisplayConsoleMessage("BL-Reset error: " + exitCode + " - " + output);
            }
            Thread.Sleep(2000);
            //geminiHandler.currentReaderState = ReaderState.DisconnectedAndReady;
            ShowVersionChange();
        }
        public void ShowVersionChange()
        {
            // Create a new form for showing the version changes
            Form versionChangeForm = new Form
            {
                Text = "Firmware Version Change",
                Size = new System.Drawing.Size(340, 190)
            };

            // Create a TableLayoutPanel
            TableLayoutPanel table = new TableLayoutPanel
            {
                RowCount = 6,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                AutoSize = true
            };

            // Add headers to the table
            table.Controls.Add(new Label { Text = "", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 0);
            table.Controls.Add(new Label { Text = "Old Firmware Versions", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 1, 0);
            table.Controls.Add(new Label { Text = "New Firmware Versions", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 2, 0);

            // Add rows for Main Version
            table.Controls.Add(new Label { Text = "Main Firmware Version", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 1);
            table.Controls.Add(new Label { Text = old_firmwareInfo.MainFirmwareVersiyon, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 1, 1);
            table.Controls.Add(new Label { Text = new_firmwareInfo.MainFirmwareVersiyon, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 2, 1);

            // Add rows for Issue Version
            table.Controls.Add(new Label { Text = "Issue Firmware Version", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 2);
            table.Controls.Add(new Label { Text = old_firmwareInfo.IssueFirmwareVersiyon, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 1, 2);
            table.Controls.Add(new Label { Text = new_firmwareInfo.IssueFirmwareVersiyon, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 2, 2);

            // Add rows for Build Time
            table.Controls.Add(new Label { Text = "Firmware Build Time", TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 0, 3);
            table.Controls.Add(new Label { Text = old_firmwareInfo.FirmwareBuildTime, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 1, 3);
            table.Controls.Add(new Label { Text = new_firmwareInfo.FirmwareBuildTime, TextAlign = System.Drawing.ContentAlignment.MiddleCenter }, 2, 3);

            // New Label for Card UID
            Label uidLabel = new Label()
            {
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            };
            table.Controls.Add(uidLabel, 1, 4);

            // Create a button to close the form
            Button closeButton = new Button
            {
                Text = "Close",
                Anchor = AnchorStyles.None, // Center the button
                AutoSize = true
            };

            // Add the Close button event handler
            closeButton.Click += (sender, e) =>
            {
                versionChangeForm.Close();
            };

            Button readCard = new Button
            {
                Text = "ReadCardUID",
                Anchor = AnchorStyles.None,
                AutoSize = true
            };

            // Add the Close button event handler
            readCard.Click += (sender, e) =>
            {
                DisplayConsoleMessage("Reading Card UID:");
                readCard.Enabled = false;
                string cardUID = "";
                if (geminiHandler == null)
                    geminiHandler = new GeminiLib(geminiReaderLibrariesPath, geminiReaderLibraryName);
                int attemps = 0;
                while (attemps < 20)
                {
                    int ret = geminiHandler.GetCardUID(out cardUID);
                    if (ret == 0)
                    {
                        uidLabel.Text = cardUID;
                        break;
                    }
                    else
                    {
                        uidLabel.Text = ret + "-" + cardUID;
                        attemps++;
                    }
                    Thread.Sleep(100);
                }
                readCard.Enabled = true;
            };

            table.Controls.Add(readCard, 0, 5);
            table.Controls.Add(closeButton, 2, 5);

            // Add the table to the form
            versionChangeForm.Controls.Add(table);

            // Show the form as a dialog (modal window)
            versionChangeForm.ShowDialog();
        }
        #endregion

        #region ReaderPhysicalConnection
        public void ChangeStateOnPhysicalConnection()
        {
            switch (ReaderPhysicalConnectionMode())
            {
                case 1:     //Normal reader connected, 
                    geminiHandler.currentReaderState = ReaderState.DisconnectedAndReady;
                    break;
                case 2:     //Bricked-Recovery reader connected,
                    geminiHandler.currentReaderState = ReaderState.ConnectedToApp;
                    break;
                default:    //Reader not connected (physically)
                    geminiHandler.currentReaderState = ReaderState.NotAttached;
                    break;
            }
        }

        public int ReaderPhysicalConnectionMode()
        {
            USBDeviceChecker checker = new USBDeviceChecker();

            if (checker.IsReaderPhysicallyConnected("VID_1394", "PID_C021"))
                return 1;
            if (checker.IsReaderPhysicallyConnected("VID_1394", "PID_BC00"))
                return 2;
            return -1;
        }
        #endregion

        #region UTILS
        public void ParseFirmwareInfo(string fwVersion, string fwVersionISS)
        {
            DisplayConsoleMessage("Parsing the Firmware Info.");

            // Find the position of the word "build" and extract the datetime part
            int buildPos = fwVersion.IndexOf("build");
            if (buildPos != -1)
            {
                string dateTime = fwVersion.Substring(buildPos + 6, 20); // Extract "MMM DD YYYY HH:MM:SS"

                // Split datetime into components
                string[] dateTimeParts = dateTime.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string month = dateTimeParts[0];
                string day = dateTimeParts[1];
                string year = dateTimeParts[2];
                string time = dateTimeParts[3].Replace(":", ""); // Remove colons from time

                // Map month to its number
                Dictionary<string, string> monthMap = new Dictionary<string, string>
                {
                    {"Jan", "01"}, {"Feb", "02"}, {"Mar", "03"}, {"Apr", "04"},
                    {"May", "05"}, {"Jun", "06"}, {"Jul", "07"}, {"Aug", "08"},
                    {"Sep", "09"}, {"Oct", "10"}, {"Nov", "11"}, {"Dec", "12"}
                };

                // Convert to the desired format: YYYYMMDDHHMMSS
                geminiReaderFWBuildTime = year + monthMap[month] + (day.Length == 1 ? "0" + day : day) + time;
            }

            // Extract the version part "0.7.1" and "046"
            string mainFWStr = fwVersionISS.Substring(0, 5);
            string issueFWStr = fwVersionISS.Substring(fwVersionISS.LastIndexOf('-') + 1);

            // Remove the dots from the version part
            geminiReaderFWMainVersion = mainFWStr.Replace(".", "");
            geminiReaderFWIssueVersion = issueFWStr;
        }
        private string ParseOutput(string blhostOutput, string desiredParse)
        {
            // Split the output into lines
            string[] lines = blhostOutput.Split('\n');

            // Find the line containing Unique Device ID
            string deviceId = lines.FirstOrDefault(line => line.Contains(desiredParse));

            // Extract the Unique Device ID value
            deviceId = deviceId?.Split('=')[1].Trim();

            // Delete spaces
            deviceId = deviceId.Replace(" ", "");

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
                    DisplayInfoMessage("No firmware file (.sb) found.");
                    DisplayConsoleMessage("No firmware file (.sb) found.");
                    return;
                }

                firmwareFolderPath = firmwareFiles[0];
                firmwareUpdateCommand = firmwareUpdateCommand + $" \"{Path.GetFullPath(firmwareFolderPath)}\"";

                DisplayInfoMessage("Firmware file found: " + Path.GetFileName(firmwareFolderPath));
                DisplayConsoleMessage("Firmware file found: " + Path.GetFileName(firmwareFolderPath));

                DisplayInfoMessage("Firmware file path: " + Path.GetFullPath(firmwareFolderPath));
                DisplayConsoleMessage("Firmware file path: " + Path.GetFullPath(firmwareFolderPath));
            }
            catch (Exception ex)
            {
                DisplayConsoleMessage($"An error occurred: {ex.Message}\n{ex.StackTrace}");
            }
        }
        static string GetFileNameFromURL(string url)
        {
            Uri uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);  // Extracts the filename from the URL
        }
        public bool CompareMD5Files(string downloadedFilePath, string md5Response)
        {
            // Calculate the MD5 hash of the file
            string fileMD5 = CalculateMD5(downloadedFilePath);

            // Compare the calculated MD5 hash with the given md5Response (case-insensitive)
            return string.Equals(fileMD5, md5Response, StringComparison.OrdinalIgnoreCase);
        }
        private string CalculateMD5(string filePath)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    // Convert byte array to a hexadecimal string
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2")); // Convert each byte to a 2-digit hexadecimal value
                    }
                    return sb.ToString();
                }
            }
        }
        private string GetFolderAndFilePath(string folderName, string fileName = "")
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            if (fileName == "")
                return Path.Combine(currentDirectory, folderName);
            else
                return Path.Combine(currentDirectory, folderName, fileName);
        }
        public void CreateFolderIfNotExists(string folderPath)
        {
            // Check if the folder exists
            if (!Directory.Exists(folderPath))
            {
                // Create the folder if it doesn't exist
                Directory.CreateDirectory(folderPath);
                Console.WriteLine($"Folder created: {folderPath}");
            }
            else
            {
                Console.WriteLine($"Folder already exists: {folderPath}");
            }
        }
        public static string FixDeviceUID(string uid)
        {
            // Split the string into 2-byte (4-character) blocks
            string[] blocks = new string[uid.Length / 4];

            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = uid.Substring(i * 4, 4);
            }

            // Reverse the order of the 2-byte blocks
            Array.Reverse(blocks);

            // Reverse the byte order within each block
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = ReverseByteOrder(blocks[i]);
            }

            // Recombine the blocks into the final string
            return string.Join("", blocks);
        }

        private static string ReverseByteOrder(string block)
        {
            // Reverse the order of every 2 characters (1 byte in hex)
            char[] chars = block.ToCharArray();

            // Swap the first and second byte within the block
            return new string(new[] { chars[2], chars[3], chars[0], chars[1] });
        }
        #endregion

    }
}
