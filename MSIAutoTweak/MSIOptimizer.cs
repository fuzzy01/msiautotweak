using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Devices.Properties;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.System.SystemInformation;
using System.Diagnostics;

namespace MSIAutoTweak
{
    public class MSIOptimizer
    {

        public DEVPROPKEY DEVPKEY_PciDevice_InterruptSupport = new DEVPROPKEY
        {
            // Windows GUID
            fmtid = new Guid(0x3ab22e31, unchecked((short)0x8264), 0x4b4e, new byte[] { 0x9a, 0xf5, 0xa8, 0xd2, 0xd8, 0xe3, 0x3e, 0x62 }),
            // Property ID
            pid = 14
        };

        public DEVPROPKEY DEVPKEY_PciDevice_InterruptMessageMaximum = new DEVPROPKEY
        {
            // Windows GUID
            fmtid = new Guid(0x3ab22e31, unchecked((short)0x8264), 0x4b4e, new byte[] { 0x9a, 0xf5, 0xa8, 0xd2, 0xd8, 0xe3, 0x3e, 0x62 }),
            // Property ID
            pid = 15
        };

        public DEVPROPKEY DEVPKEY_Device_DevNodeStatus = new DEVPROPKEY
        {
            // Windows GUID
            fmtid = new Guid(0x4340a6c5, unchecked((short)0x93fa), 0x4706, new byte[] { 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7 }),
            // Property ID
            pid = 2
        };

        public enum DIREG : uint
        {
            DIREG_DEV = 0x00000001,
            DIREG_DRV = 0x00000002,
            DIREG_BOTH = 0x00000004
        }

        public enum KEY_ACCESS : uint
        {
            KEY_READ = 0x20019,
            KEY_WRITE = 0x20006,
            KEY_ALL_ACCESS = 0xF003F
        }

        private readonly List<Device> _devices;

        public MSIOptimizer()
        {
            _devices = new List<Device>();
        }

        public IReadOnlyList<Device> Devices => _devices.AsReadOnly();

        public unsafe void LoadDevices()
        {
            var hDevInfo = PInvoke.SetupDiGetClassDevs((Guid?)null, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_ALLCLASSES);
            if (hDevInfo.IsInvalid)
            {
                throw new Exception("Failed to get device information.");
            }

            try
            {
                _devices.Clear();

                var devInfoData = new SP_DEVINFO_DATA
                {
                    cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
                };

                for (uint index = 0; PInvoke.SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData); index++)
                {
                    var device = new Device();

                    var buffer = new Span<byte>(new byte[1024]);
                    uint requiredSize = 0;

                    // Get Interrupt Support 
                    DEVPROPTYPE propType;
                    if (PInvoke.SetupDiGetDeviceProperty(hDevInfo, in devInfoData, DEVPKEY_PciDevice_InterruptSupport, out propType, buffer, &requiredSize, 0))
                    {
                        device.InterruptSupport = propType == DEVPROPTYPE.DEVPROP_TYPE_UINT32 ? BitConverter.ToUInt32(buffer) : 0;

                        if (PInvoke.SetupDiGetDeviceProperty(hDevInfo, in devInfoData, DEVPKEY_PciDevice_InterruptMessageMaximum, out propType, buffer, &requiredSize, 0))
                        {
                            device.InterruptMessageMaximum = propType == DEVPROPTYPE.DEVPROP_TYPE_UINT32 ? BitConverter.ToUInt32(buffer) : 0;
                        }
                    }
                    else
                    {
                        continue; // Skip if we cannot get interrupt support
                    }

                    if (device.InterruptSupport == 0)
                    {
                        continue; // Skip if no interrupt support
                    }

                    // Get device description
                    if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, in devInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC, null, buffer, &requiredSize))
                    {
                        device.DeviceDesc = Encoding.Unicode.GetString(buffer[..((int)requiredSize - 2)]);
                    }
                    else
                    {
                        continue; // Skip if we cannot get device description
                    }

                    // Get friendly name
                    if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, in devInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME, null, buffer, &requiredSize))
                    {
                        device.FriendlyName = Encoding.Unicode.GetString(buffer[..((int)requiredSize - 2)]);
                    }

                    // Get Location information
                    if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, in devInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION, null, buffer, &requiredSize))
                    {
                        device.LocationInfo = Encoding.Unicode.GetString(buffer[..((int)requiredSize - 2)]);
                    }

                    // Get Device Object Name
                    if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, in devInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_PHYSICAL_DEVICE_OBJECT_NAME, null, buffer, &requiredSize))
                    {
                        device.DeviceObjectName = Encoding.Unicode.GetString(buffer[..((int)requiredSize - 2)]);
                    }

                    // Get Device Type
                    if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, in devInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CLASS, null, buffer, &requiredSize))
                    {
                        device.Class = Encoding.Unicode.GetString(buffer[..((int)requiredSize - 2)]);
                    }

                    // Get Instance ID
                    var idBuffer = new Span<char>(new char[1024]);
                    if (PInvoke.SetupDiGetDeviceInstanceId(hDevInfo, in devInfoData, idBuffer, &requiredSize))
                    {
                        device.InstanceId = idBuffer.Slice(0, (int)requiredSize - 1).ToString();
                    }

                    // Get registry flags
                    var hKey = PInvoke.SetupDiOpenDevRegKey(hDevInfo, in devInfoData, (uint)SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL, 0, (uint)DIREG.DIREG_DEV, (uint)KEY_ACCESS.KEY_READ);
                    if (hKey.IsInvalid)
                    {
                        // If we cannot open the registry key, skip this device
                        continue;
                    }

                    RegistryKey regKey = RegistryKey.FromHandle(hKey);
                    RegistryKey? msiKey = null, affinityKey = null;

                    try
                    {
                        msiKey = regKey.OpenSubKey(@"Interrupt Management\MessageSignaledInterruptProperties");
                        if (msiKey != null)
                        {
                            // Check if the device has a specific affinity policy set
                            device.MSISupported = (int)msiKey.GetValue("MSISupported", 0);
                            device.MessageNumberLimit = (int)msiKey.GetValue("MessageNumberLimit", -1);
                        }

                        affinityKey = regKey.OpenSubKey(@"Interrupt Management\Affinity Policy");
                        if (affinityKey != null)
                        {
                            // Check if the device has a specific affinity policy set
                            device.DevicePolicy = (int)affinityKey.GetValue("DevicePolicy", -1);
                            device.DevicePriority = (int)affinityKey.GetValue("DevicePriority", -1);
                            var assignmentSetOverride = affinityKey.GetValue("AssignmentSetOverride", null);
                            if (assignmentSetOverride is null)
                            {
                                device.AssignmentSetOverride = 0;
                            }
                            else if (assignmentSetOverride is byte[] arrayValue)
                            {
                                if (arrayValue.Length < 8)
                                {
                                    var t = new byte[8];
                                    Array.Copy(arrayValue, t, arrayValue.Length);
                                    arrayValue = t;
                                }
                                device.AssignmentSetOverride = BitConverter.ToUInt64(arrayValue, 0);
                            }
                            else if (assignmentSetOverride is UInt64 uintValue)
                            {
                                device.AssignmentSetOverride = uintValue;
                            }
                        }
                    }
                    finally
                    {
                        msiKey?.Dispose();
                        affinityKey?.Dispose();
                        regKey.Dispose();
                    }

                    _devices.Add(device);
                }
            }
            finally
            {
                hDevInfo.Dispose();
            }
        }


        public unsafe void Optimize(bool restartDevices = true)
        {
            (int pCoreCount, int eCoreCount, bool hyperThreadingEnabled) = GetCPUInformation();

            if (eCoreCount == 0 && ((hyperThreadingEnabled && pCoreCount < 12) || (!hyperThreadingEnabled && pCoreCount < 6)))
            {
                // If no E-cores detected and number of P-cores is less than 6, throw an exception
                throw new Exception("No E-cores detected and number of P-cores is less than 6.");
            }

            var hDevInfo = PInvoke.SetupDiGetClassDevs((Guid?)null, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_ALLCLASSES);
            if (hDevInfo.IsInvalid)
            {
                throw new Exception("Failed to get device information.");
            }

            try
            {
                var availableCores = new List<int>();

                if (eCoreCount != 0)
                {
                    // Use only E-cores if available
                    for (int i = pCoreCount + eCoreCount - 1; i >= pCoreCount; i--)
                    {
                        availableCores.Add(i);
                    }
                }
                else
                {
                    // We have no E-cores, reserve at lear 4 P-cores for apps
                    for (int i = 4; i < pCoreCount; i++)
                    {
                        availableCores.Add(i);
                    }
                }

                // Skip NVMe and SATA controllers
                var devices = _devices.ToList().Where(d => d.IsMSISupported && d.Class != "SCSIAdapter" && d.Class != "HDC").ToList();

                var usbDevices = devices.FindAll(d => d.Class == "USB" || d.Class == "HIDClass");
                foreach (var device in usbDevices)
                {
                    OptimizeDevice(hDevInfo, device, availableCores);
                    devices.Remove(device);
                }

                var videoDevices = _devices.FindAll(d => d.Class == "Display");
                foreach (var device in videoDevices)
                {
                    OptimizeDevice(hDevInfo, device, availableCores);
                    devices.Remove(device);
                }

                // Audio devices have no proper class, so we filter by device description
                var audioDevices = devices.FindAll(d => d.DeviceDesc == "High Definition Audio Controller");
                foreach (var device in audioDevices)
                {
                    OptimizeDevice(hDevInfo, device, availableCores);
                    devices.Remove(device);
                }

                var netDevices = devices.FindAll(d => d.Class == "Net");
                int maxMessageNumberLimit = 0;
                foreach (var device in netDevices)
                {
                    var res = OptimizeDevice(hDevInfo, device, availableCores, reuseCores: true);
                    if (res != OptimizationResult.NotEnoughCores)
                        maxMessageNumberLimit = Math.Max(maxMessageNumberLimit, device.MessageNumberLimit);
                    devices.Remove(device);
                }
                availableCores.RemoveRange(0, maxMessageNumberLimit);

                // Remaining devices will be assigned to the remaining cores
                bool reuseCores = availableCores.Count == 1;
                foreach (var device in devices)
                {
                    OptimizeDevice(hDevInfo, device, availableCores, reuseCores: reuseCores);
                }

            }
            finally
            {
                hDevInfo.Dispose();
            }

        }

        enum OptimizationResult
        {
            AlreadyOptimized,
            SuccessfullyOptimized,
            NotEnoughCores
        }

        private OptimizationResult OptimizeDevice(SafeHandle hDevInfo, Device device, List<int> availableCores, bool restartDevice = true, bool reuseCores = false)
        {
            var messageNumberLimit = Math.Max(device.MessageNumberLimit, 1);
            if (availableCores.Count >= messageNumberLimit)
            {
                UInt64 affinityMask = 0;
                for (int i = 0; i < messageNumberLimit; i++)
                {
                    int coreIndex = availableCores[i];
                    Debug.WriteLine($"Assign {device.Class} Device {device.DeviceDesc} to Core {coreIndex}");
                    affinityMask |= 1UL << coreIndex;
                }

                if (!reuseCores)
                    availableCores.RemoveRange(0, messageNumberLimit);

                if (device.MSISupported == 1 && device.DevicePolicy == (int)Device.IRQ_DEVICE_POLICY.IrqPolicySpecifiedProcessors && device.AssignmentSetOverride == affinityMask)
                    return OptimizationResult.AlreadyOptimized;

                SetDeviceParameters(hDevInfo, device, Device.IRQ_DEVICE_POLICY.IrqPolicySpecifiedProcessors, affinityMask);

                if (restartDevice)
                {
                    RestartDevice(hDevInfo, device);
                }

                return OptimizationResult.SuccessfullyOptimized;
            }
            else
            {
                Debug.WriteLine($"Not enough cores available for {device.Class} Device {device.DeviceDesc}. Available: {availableCores.Count}, Required: {messageNumberLimit}");

                // Set it back to machine default
                if (device.MSISupported == 1 && device.DevicePolicy == (int)Device.IRQ_DEVICE_POLICY.IrqPolicyMachineDefault)
                    return OptimizationResult.NotEnoughCores;

                SetDeviceParameters(hDevInfo, device, Device.IRQ_DEVICE_POLICY.IrqPolicyMachineDefault, 0L);

                if (restartDevice)
                {
                    RestartDevice(hDevInfo, device);
                }

                return OptimizationResult.NotEnoughCores;
            }
        }

        private void SetDeviceParameters(SafeHandle hDevInfo, Device device, Device.IRQ_DEVICE_POLICY devicePolicy, UInt64 affinityMask)
        {
            var devInfoData = GetDeviceInfo(hDevInfo, device);

            var hKey = PInvoke.SetupDiOpenDevRegKey(hDevInfo, in devInfoData, (uint)SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL, 0, (uint)DIREG.DIREG_DEV, (uint)KEY_ACCESS.KEY_ALL_ACCESS);
            if (hKey.IsInvalid)
                throw new Exception($"Failed to open registry key for {device.DeviceDesc}.");

            RegistryKey regKey = RegistryKey.FromHandle(hKey);
            RegistryKey? msiKey = null, affinityKey = null;

            try
            {
                msiKey = regKey.CreateSubKey(@"Interrupt Management\MessageSignaledInterruptProperties", writable: true);
                msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);

                affinityKey = regKey.CreateSubKey(@"Interrupt Management\Affinity Policy", writable: true);
                affinityKey.SetValue("DevicePolicy", (int)devicePolicy, RegistryValueKind.DWord);
                if (devicePolicy == Device.IRQ_DEVICE_POLICY.IrqPolicySpecifiedProcessors)
                {
                    affinityKey.SetValue("AssignmentSetOverride", BitConverter.GetBytes(affinityMask), RegistryValueKind.Binary);
                }
                else
                {
                    affinityKey.DeleteValue("AssignmentSetOverride", false);
                }
            }
            finally
            {
                msiKey?.Dispose();
                affinityKey?.Dispose();
                regKey.Dispose();
            }

            device.MSISupported = 1;
            device.DevicePolicy = (int)devicePolicy;
            device.AssignmentSetOverride = affinityMask;
        }

        public unsafe void RestartDevice(SafeHandle hDevInfo, Device device)
        {
            var devInfoData = GetDeviceInfo(hDevInfo, device);

            var buffer = new Span<byte>(new byte[1024]);
            uint requiredSize = 0;
            DEVPROPTYPE propType;
            if (PInvoke.SetupDiGetDeviceProperty(hDevInfo, in devInfoData, DEVPKEY_Device_DevNodeStatus, out propType, buffer, &requiredSize, 0))
            {
                var nodeStatus = propType == DEVPROPTYPE.DEVPROP_TYPE_UINT32 ? BitConverter.ToUInt32(buffer) : 0;
                if ((nodeStatus & (uint)CM_DEVNODE_STATUS_FLAGS.DN_STARTED) == 0)
                {
                    // Device is not started (probably disabled), no need to restart
                    Debug.WriteLine($"Device {device.DeviceDesc} is not started (probably disabled), skipping restart.");
                    return;
                }
            }

            SP_PROPCHANGE_PARAMS propChangeParams = new SP_PROPCHANGE_PARAMS
            {
                ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                {
                    cbSize = (uint)Marshal.SizeOf<SP_CLASSINSTALL_HEADER>(),
                    InstallFunction = DI_FUNCTION.DIF_PROPERTYCHANGE
                },
                StateChange = SETUP_DI_STATE_CHANGE.DICS_PROPCHANGE,
                Scope = SETUP_DI_PROPERTY_CHANGE_SCOPE.DICS_FLAG_GLOBAL,
                HwProfile = 0
            };

            Debug.WriteLine($"Restarting device {device.DeviceDesc}...");

            // This a workaround for a bug in CSWin32 wrapper for SetupDiSetClassInstallParams
            // if (!PInvoke.SetupDiSetClassInstallParams(hDevInfo, deviceInfoData, propChangeParams.ClassInstallHeader, (uint)Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
            var hDevInfo2 = new HDEVINFO(hDevInfo.DangerousGetHandle());
            if (!PInvoke.SetupDiSetClassInstallParams(hDevInfo2, &devInfoData, &propChangeParams.ClassInstallHeader, (uint)Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
            {
                throw new Exception($"Failed to set class install params for {device.DeviceDesc}. Error: {Marshal.GetLastWin32Error()}");
            }
            if (!PInvoke.SetupDiCallClassInstaller(DI_FUNCTION.DIF_PROPERTYCHANGE, hDevInfo, devInfoData))
            {
                throw new Exception($"Failed to call class installer for {device.DeviceDesc}. Error: {Marshal.GetLastWin32Error()}");
            }
        }

        private unsafe SP_DEVINFO_DATA GetDeviceInfo(SafeHandle hDevInfo, Device device)
        {
            var devInfoData = new SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
            };

            for (uint index = 0; PInvoke.SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData); index++)
            {
                // Get Instance ID
                uint requiredSize = 0;
                var idBuffer = new Span<char>(new char[1024]);
                if (PInvoke.SetupDiGetDeviceInstanceId(hDevInfo, in devInfoData, idBuffer, &requiredSize))
                {
                    string instanceId = idBuffer.Slice(0, (int)requiredSize - 1).ToString();
                    if (instanceId == device.InstanceId)
                    {
                        return devInfoData;
                    }
                }
            }

            throw new Exception($"Failed find device {device.DeviceDesc}. Maybe it was removed.");
        }

        // Complicated logic to get the number of P-cores and E-cores
        private unsafe (int, int, bool) GetCPUInformation()
        {
            uint returnLength = 0;
            bool success = PInvoke.GetSystemCpuSetInformation(null, 0, out returnLength, null);
            if (!success && Marshal.GetLastWin32Error() != 0)
                throw new Exception($"Failed to get CPU set info size. Error: {Marshal.GetLastWin32Error()}");

            IntPtr buffer = Marshal.AllocHGlobal((int)returnLength);
            try
            {
                success = PInvoke.GetSystemCpuSetInformation((SYSTEM_CPU_SET_INFORMATION*)buffer, returnLength, out returnLength, null);
                if (!success)
                    throw new Exception($"Failed to get CPU set info. Error: {Marshal.GetLastWin32Error()}");

                int pCoreCount = 0;
                int eCoreCount = 0;
                bool hyperThreadingEnabled = false;
                IntPtr offset = 0;
                while (offset < returnLength)
                {
                    SYSTEM_CPU_SET_INFORMATION cpuSet = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(buffer + offset);
                    if (cpuSet.Type == CPU_SET_INFORMATION_TYPE.CpuSetInformation)
                    {
                        if (cpuSet.Anonymous.CpuSet.EfficiencyClass == 0)
                        {
                            eCoreCount++;
                        }
                        else
                        {
                            pCoreCount++;

                            hyperThreadingEnabled |= (cpuSet.Anonymous.CpuSet.LogicalProcessorIndex != cpuSet.Anonymous.CpuSet.CoreIndex);
                        }
                    }
                    offset += (int)cpuSet.Size;
                }
                return (pCoreCount, eCoreCount, hyperThreadingEnabled);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
