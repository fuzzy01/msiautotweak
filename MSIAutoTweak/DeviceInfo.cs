using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace MSIAutoTweak
{
    public  class Device
    {
        public enum IRQ_DEVICE_POLICY : int
        {
            IrqPolicyMachineDefault = 0,
            IrqPolicyAllCloseProcessors = 1,
            IrqPolicyOneCloseProcessor = 2,
            IrqPolicyAllProcessorsInMachine = 3,
            IrqPolicySpecifiedProcessors = 4,
            IrqPolicySpreadMessagesAcrossAllProcessors = 5,
            IrqPolicyAllProcessorsInMachineWhenSteered = 6
        }

        public enum IRQ_PRIORITY : int
        {
            IrqPriorityLow = 1,
            IrqPriorityNormal = 2,
            IrqPriorityHigh = 3
        }

        public enum IRQ_SUPPORT : uint
        {
            IrqSupportLineBased = 1,
            IrqSupportMSI = 2,
            IrqSupportMSIX = 4
        }

        // From SetupAPI
        public string DeviceDesc { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string LocationInfo { get; set; } = string.Empty;
        public string DeviceObjectName { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string InstanceId { get; internal set; } = string.Empty;
        public uint InterruptSupport { get; set; }
        public uint InterruptMessageMaximum { get; set; }

        // From registry
        public int MSISupported { get; set; } = 0;
        public int MessageNumberLimit { get; set; } = -1;
        public int DevicePolicy { get; set; } = -1; 
        public int DevicePriority { get; set; } = -1;
        public Int64 AssignmentSetOverride { get; set; } = 0;
        public Int64 TargetSet { get; set; } = 0;

        public bool IsMSISupported => (InterruptSupport & (uint)IRQ_SUPPORT.IrqSupportMSI) != 0 || (InterruptSupport & (uint)IRQ_SUPPORT.IrqSupportMSIX) != 0;
        public bool IsLineBasedSupported => (InterruptSupport & (uint)IRQ_SUPPORT.IrqSupportLineBased) != 0;
      
    }
}
