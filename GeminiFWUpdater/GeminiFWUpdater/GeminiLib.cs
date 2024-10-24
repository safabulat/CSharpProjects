using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GeminiFWUpdater
{
    public class GeminiLib
    {
        #region Class Var
        public string DLLPath { get; set; }
        private IntPtr hModule;
        private IntPtr hSession;
        public enum ReaderState
        {
            NotAttached, //TODO: How to implement this???
            DisconnectedAndReady,
            ConnectingToApp,
            FailedToConnectApp,
            ConnectedToApp,
            ConnectingToBootloader,
            FailedToConnectBootloader,
            ConnectedToBootloader,
            NoFirmwareFileFound,
            FirmwareFileFound,
            FirmwareUpdateInProgress,
            FailedToFirmwareUpdate,
            FirmwareUpdateFinished,
        }
        public ReaderState currentReaderState { get; set; }

        public GeminiLib(string dllPath, string dllName)
        {
            DLLPath = Path.Combine(dllPath, dllName);
            hModule = LoadLibrary(DLLPath);

            if (hModule == IntPtr.Zero)
            {
                Console.WriteLine("dllPath: " + dllPath);
                throw new Exception("Failed to load the DLL: " + DLLPath);
            }

            currentReaderState = ReaderState.DisconnectedAndReady;

            // Retrieve the function pointers
            CreateSession = GetFunction<CreateSessionDelegate>("CreateSession");
            ReleaseSession = GetFunction<ReleaseSessionDelegate>("ReleaseSession");
            ConnectReader = GetFunction<ConnectReaderDelegate>("ConnectReader");
            DisconnectReader = GetFunction<DisconnectReaderDelegate>("DisconnectReader");
            SendDirectCommand = GetFunction<SendDirectCommandDelegate>("SendDirectCommand");
        }

        #endregion

        #region LoadLibrary and GetProcAddress

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private T GetFunction<T>(string functionName) where T : Delegate
        {
            IntPtr procAddress = GetProcAddress(hModule, functionName);
            if (procAddress == IntPtr.Zero)
            {
                throw new Exception($"Failed to get address for function: {functionName}");
            }
            return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
        }

        #endregion

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateSessionDelegate(int reserved);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ReleaseSessionDelegate(IntPtr hSession, int reserved);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ConnectReaderDelegate(IntPtr hSession, int interfaceType, string pszReaderName, int readerType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr DisconnectReaderDelegate(IntPtr hSession);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SendDirectCommandDelegate(
            IntPtr hSession, byte CMDCode, byte[] pCMDData, ulong CMDDataLen, byte[] pRespData, ref ulong pRespDataLen);

        #endregion

        #region Function Pointers

        private readonly CreateSessionDelegate CreateSession;
        private readonly ReleaseSessionDelegate ReleaseSession;
        private readonly ConnectReaderDelegate ConnectReader;
        private readonly DisconnectReaderDelegate DisconnectReader;
        private readonly SendDirectCommandDelegate SendDirectCommand;

        #endregion

        #region FuncitonAPIs
        public int ConnectToTheReader(string portName, int baudRate)
        {
            return (int)ConnectReader(hSession,1 , portName, baudRate);
        }
        public int DisconnectToTheReader()
        {
            return (int)DisconnectReader(hSession);
        }
        public int SendDirectCMD(byte cmd, out string output)
        {
            int ret = -1;
            byte[] respData = new byte[256];
            ulong respDataLen = (ulong)respData.Length;

            Console.WriteLine("SendDirectCMD: " + cmd);

            ret = (int)SendDirectCommand(hSession, cmd, null, (ulong)0, respData, ref respDataLen);
            if (ret == 0)
            {
                output = Encoding.ASCII.GetString(respData, 0, (int)respDataLen);
            }
            else
            {
                output = "";
                throw new Exception("Err SendDirectCommand. cmd: " + cmd + " - err: " + ret);
            }

            return ret;
        }

        public int PerformSoftReset()
        {
            int ret = -1;
            ulong respDataLen = 0;
            ret = (int)SendDirectCommand(hSession, 0x66, null, (ulong)0, null, ref respDataLen);
            if (ret != 0)
            {
                throw new Exception("Err Performing SoftReset: " + ret);
            }

            return ret;
        }
        #endregion

        #region ReaderSequences
        public bool ConnectReaderSeq(string portName, int baudRate)
        {
            Console.WriteLine("CreateSession");

            ReleaseSession(hSession, 0);
            hSession = IntPtr.Zero;

            hSession = CreateSession(0);
            if (hSession == IntPtr.Zero)
            {
                throw new Exception("Session Can't Created!");
            }

            Console.WriteLine("Connect the reader");
            int ret = ConnectToTheReader(portName, baudRate);
            if (ret != 0)
            {
                //throw new Exception("Reader Can't Connected! - " + ret);
                return false;
            }

            return true;
        }

        public bool DisconnectReaderSeq()
        {
            Console.WriteLine("Disconnect the reader");
            int ret = DisconnectToTheReader();
            if (ret != 0)
            {
                throw new Exception("Reader Can't Disconnected! - " + ret);
            }

            Console.WriteLine("Destroy the session");
            ret = (int)ReleaseSession(hSession, 0);
            if (ret != 0)
            {
                throw new Exception("Session Can't destroyed! - " + ret);
            }

            return true;
        }
        
        public bool GetReaderVersion(ref string firmwareVersion, ref string firmwareVersionISS)
        {
            if (hSession == IntPtr.Zero)
            {
                Console.WriteLine("Session Lost, Now Creating.");
                hSession = CreateSession(0);
            }

            if (hSession == IntPtr.Zero)
            {
                Console.WriteLine("Session Can't Created");
                return false;
            }

            int ret = -1;
            byte commandCode;
            bool isErr = false;

            byte[] respData;
            ulong respDataLen;

            firmwareVersion = "";
            firmwareVersionISS = "";

            Console.WriteLine("Step 1. GetReaderVersion ISS");
            commandCode = 0x8A;
            respData = new byte[256];
            respDataLen = (ulong)respData.Length;
            ret = (int)SendDirectCommand(hSession, commandCode, null, (ulong)0, respData, ref respDataLen);
            if (ret == 0)
            {
                firmwareVersionISS = Encoding.ASCII.GetString(respData, 0, (int)respDataLen);
                firmwareVersionISS = firmwareVersionISS.Replace("FW: ", "");

                Console.WriteLine("FWvI: " + firmwareVersionISS);
            }
            else
            {
                isErr = true;
                //throw new Exception("Err Sending 0x8A: " + ret);
            }

            ret = -1;
            Thread.Sleep(100);

            Console.WriteLine("Step 2. GetReaderVersion Main");
            commandCode = 0x63;
            respData = new byte[256];
            respDataLen = (ulong)respData.Length;
            ret = (int)SendDirectCommand(hSession, commandCode, null, (ulong)0, respData, ref respDataLen);
            if (ret == 0)
            {
                firmwareVersion = Encoding.ASCII.GetString(respData, 0, (int)respDataLen);
                firmwareVersion = firmwareVersion.Replace("\0", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("/", "");
                Console.WriteLine("FWvM: " + firmwareVersion);
            }
            else
            {
                isErr = true;
                //throw new Exception("Err Sending 0x63: " + ret);
            }

            return !isErr;
        }
        

        public int GetCardUID(out string output)
        {
            ulong respDataLen = 256;
            byte[] respData = new byte[respDataLen];
            output = string.Empty;

            // Send Get Report command (0x0E) after polling
            int ret = (int)SendDirectCommand(hSession, 0x0E, null, 0, respData, ref respDataLen);

            if (ret != 0)
            {
                // No card in field or error occurred
                if (respDataLen == 0 || ret == 8001)
                {
                    output = "NO_CARD_IN_RF.";
                    return ret;
                }
                if (respDataLen == 8051)
                {
                    output = "MY_RETRIES_OVER";
                    return ret;
                }
                //throw new Exception("Error: " + ret);
            }

            // Check if card was detected (0x8B response)
            if (respData[0] != 0x8B)
            {
                output = "Card not detected.";
                return -1;
            }

            // Get the UID from the response
            int snLen = respData[3]; // UID length
            byte[] cardUID = new byte[snLen];
            Array.Copy(respData, 4, cardUID, 0, snLen);

            // Convert UID to hex string
            output = BitConverter.ToString(cardUID).Replace("-", "");

            return ret;
        }
        #endregion

        #region DLL
        // Ensure that the library is unloaded properly
        public void UnloadDll()
        {
            if (hModule != IntPtr.Zero)
            {
                FreeLibrary(hModule);
                hModule = IntPtr.Zero;
            }
        }
        #endregion
    }
}
