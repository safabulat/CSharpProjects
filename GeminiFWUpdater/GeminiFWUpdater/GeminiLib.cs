using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
            byte[] atq = new byte[2];        // ATQA
            byte sak = 0x00;                 // SAK
            byte [] uid = new byte[10];      // Card UID
            int uidLen = 0;

            if (GetFirstTCLCardA(out atq, out sak, out uid, out uidLen))
            {
                output = BitConverter.ToString(uid, 0, uidLen);
                return 0;
            }
            else
            {
                output = "";
                return -1;
            }
        }

        int SetPollingMode(bool mode)
        {
            byte[] cmdBuff = new byte[128];
            ulong respDataLen = 266;
            byte[] respData = new byte[respDataLen];  //Buffer contains the Reader response data
            int res = 0;

            cmdBuff[0] = mode ? (byte)0x01 : (byte)0x00; // polling on/off
            cmdBuff[1] = 0x03; // mask --> bit position for different technologies 0x01 type a 0x02 type a 0x03 both
            cmdBuff[2] = 0x00; // baudrate A --> For Type A (codded according to ISO 14443-4). 
            cmdBuff[3] = 0x00; // baudrate B --> For Type b (codded according to ISO 14443-4). 
            cmdBuff[4] = 0x01; // abort --> ON/OFF abort on any collision 
            cmdBuff[5] = 0x01; // EMV check --> for EMV card: 0-no looking for EMV card, 1- looking for EMV card 
            int length = 6;

            //Execute direct command and get the result
            res = (int)SendDirectCommand(hSession, 0x05, cmdBuff, (ulong)length, respData, ref respDataLen);

            if(res != 0)
                Console.WriteLine("SendDirectCommand(0x05) SetPollingMode=" + mode + " ErrCode: " + res + " respLen: " + respDataLen);

            return res;
        }

        private static int pollCounter = 0;
        private static int errCounter = 0;
        private static bool isPollingModeActive = false;
        private const int POLL_RESET_COUNTER = 10; // Adjust as necessary

        public bool GetFirstTCLCardA(out byte[] atq, out byte sak, out byte[] sn, out int snLen)
        {
            byte[] respBuff = new byte[266];
            bool result = false;
            ulong respLen = (ulong)respBuff.Length;
            int res = 0;
            atq = new byte[2];
            sak = 0;
            sn = new byte[10];
            snLen = 0;

            if (hSession == IntPtr.Zero)
                throw new Exception("GeminiReader communication error");

            if ((pollCounter++) > POLL_RESET_COUNTER)
            {
                pollCounter = 0;
                SetPollingMode(false);
            }
            if (!isPollingModeActive)
            {
                SetPollingMode(true);
            }

            res = (int)SendDirectCommand(hSession, 0x0E, null, 0, respBuff, ref respLen);
            Console.WriteLine("Res: " + res + " rL: " + respLen);
            if (res == 0 && respLen > 0)
            {
                PrintHex("GetReport: ", respBuff, (int)respLen);
            }

            if (res == 0 && respLen == 0)
            {
                Thread.Sleep(10);
                return false;
            }

            isPollingModeActive = false;

            if (res != 0)
            {
                errCounter++;
                if (errCounter > 2)
                {
                    throw new Exception("Command send error");
                }
                pollCounter = POLL_RESET_COUNTER + 1;
                Thread.Sleep(100);
                return false;
            }

            if (respBuff[0] == 0x81 && respBuff[1] == 0x40)
            {
                throw new Exception("Collusion detected");
            }

            if (respBuff[0] != 0x8B)
            {
                Thread.Sleep(100);
                return false;
            }

            errCounter = 0;

            Thread.Sleep(100);
            res = RfReset(hSession, 25);
            if (res > 0)
                throw new Exception("RfReset error");

            respLen = (ulong)respBuff.Length;
            Array.Clear(respBuff, 0, respBuff.Length);
            res = (int)SendDirectCommand(hSession, 0x4A, null, 0, respBuff, ref respLen);
            if (res != 0 && res != -1)
                throw new Exception("Command error");

            result = res == 0;
            Thread.Sleep(100);

            if (result)
            {
                //Array.Copy(respBuff, 0, atq, 0, 2);
                //sak = respBuff[2];
                snLen = respBuff[3];
                Array.Copy(respBuff, 4, sn, 0, snLen);

                //PrintHex("atq: ", atq, atq.Length);
                //PrintHex("PICCuid: ", sn, snLen);

                if (res != 0)
                    throw new Exception("Incorrect CT in UID");
            }

            return result;
        }
        private void PrintHex(string prefix, byte[] data, int length)
        {
            Console.Write(prefix);
            for (int i = 0; i < length; i++)
            {
                Console.Write($"{data[i]:X2} ");
            }
            Console.WriteLine();
        }
        public int RfReset(IntPtr session, int milliseconds)
        {
            byte commandCode = 0x20;
            byte[] data = BitConverter.GetBytes(milliseconds); // Convert milliseconds to a 4-byte array
            ulong responseLen = 0;

            // Call SendDirectCommand with the prepared command and data
            int ret = (int)SendDirectCommand(session, commandCode, data, (ulong)data.Length, null, ref responseLen);

            // Handle response or check result as needed
            if (ret != 0) // Assuming 0 represents MI_OK
                Console.WriteLine($"Failed to reset radio field. Error code: {ret}");

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
