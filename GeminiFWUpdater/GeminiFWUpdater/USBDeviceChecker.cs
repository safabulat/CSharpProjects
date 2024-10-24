using System;
using System.Runtime.InteropServices;
using System.Text;

public class USBDeviceChecker
{
    // Define constants used by the Windows API
    private const int DIGCF_PRESENT = 0x00000002;
    private const int DIGCF_DEVICEINTERFACE = 0x00000010;
    private const int SPDRP_HARDWAREID = 0x00000001;

    // GUID for USB devices (set as normal static instead of readonly for ref use)
    private static Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, uint Property, out uint PropertyRegDataType, byte[] PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    // Main function to check if reader is connected
    public bool IsReaderPhysicallyConnected(string vid, string pid)
    {
        // Create a local copy of the GUID to pass by ref
        Guid deviceGuid = GUID_DEVINTERFACE_USB_DEVICE;

        // Get a handle to the device information set for all present USB devices
        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref deviceGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if (deviceInfoSet == IntPtr.Zero)
        {
            throw new Exception("Failed to get device information set for USB devices.");
        }

        try
        {
            // Loop through the devices
            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

            uint index = 0;
            while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
            {
                index++;

                // Get the hardware ID of the device
                uint regDataType;
                byte[] buffer = new byte[1024];
                uint requiredSize;
                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, SPDRP_HARDWAREID, out regDataType, buffer, (uint)buffer.Length, out requiredSize))
                {
                    string hardwareId = Encoding.Unicode.GetString(buffer).Trim('\0');

                    // Case-insensitive check for VID and PID using IndexOf instead of Contains
                    if (hardwareId.IndexOf(vid, StringComparison.OrdinalIgnoreCase) >= 0 && hardwareId.IndexOf(pid, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true; // Device found
                    }
                }
            }
        }
        finally
        {
            // Clean up the device info list
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        // Device not found
        return false;
    }
}
