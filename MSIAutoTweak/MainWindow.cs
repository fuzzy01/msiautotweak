using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.IO;
using System.Windows.Controls.Primitives;

namespace MSIAutoTweak
{
    public class Config
    {
        public bool RestartDevices { get; set; } = true;
        public bool OptimizeMiscDevices { get; set; } = true;
        public int OptimizationStrategy { get; set; } = 0;
        public int VideoCoreCount { get; set; } = 1;
    }

    public partial class MainWindow : Window
    {
        private Config _config;
        private readonly MSIOptimizer _msiOptimizer;

        private static readonly SolidColorBrush GreenBrush  = new(Color.FromRgb(0xC8, 0xE6, 0xC9));
        private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(0xFF, 0xF9, 0xC4));
        private static readonly SolidColorBrush BeigeBrush  = new(Color.FromRgb(0xF5, 0xF0, 0xE8));

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            LoadConfig();
            _msiOptimizer = new MSIOptimizer();
            _msiOptimizer.LoadDevices();
            DevicesGrid.ItemsSource = _msiOptimizer.Devices;
        }

        private void InitializeUI()
        {
        }

        private void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _msiOptimizer.LoadDevices();
                int[] coreOptions = { 1, 2, 3, 4 };
                _msiOptimizer.Optimize(
                    RestartDevicesCheckBox.IsChecked ?? true,
                    OptimizeMiscDevicesCheckBox.IsChecked ?? true,
                    (OptimizationStrategy)StrategyComboBox.SelectedIndex,
                    coreOptions[VideoCoresComboBox.SelectedIndex]);
                _msiOptimizer.LoadDevices();
                DevicesGrid.Items.Refresh();
                MessageBox.Show($"Optimization completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _msiOptimizer.LoadDevices();
                DevicesGrid.Items.Refresh();
                MessageBox.Show($"An error occurred during optimization: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    
        private void DevicesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is not Device device) return;
            e.Row.Background = GetRowBrush(device, (OptimizationStrategy)StrategyComboBox.SelectedIndex);
        }

        private static Brush GetRowBrush(Device device, OptimizationStrategy strategy)
        {
            if (strategy == OptimizationStrategy.Default)
            {
                bool atDefault = device.DevicePolicy == (int)Device.IRQ_DEVICE_POLICY.IrqPolicyMachineDefault || device.DevicePolicy ==  (int)Device.IRQ_DEVICE_POLICY.IrqPolicyUndefined;
                return atDefault ? GreenBrush : YellowBrush;
            }
            
            // MoveToECores and Hybrid strategies
            
            if (device.IsMSISupported && (device.Class == "SCSIAdapter" || device.Class == "HDC"))
                return BeigeBrush;
            
            if (device.IsMSISupported && device.MSISupported != 0
                && device.DevicePolicy == (int)Device.IRQ_DEVICE_POLICY.IrqPolicySpecifiedProcessors
                && device.AssignmentSetOverride != 0)
                return GreenBrush;
            
            if (!device.IsMSISupported && device.IsLineBasedSupported 
                && device.DevicePolicy == (int)Device.IRQ_DEVICE_POLICY.IrqPolicySpecifiedProcessors
                && device.AssignmentSetOverride != 0)
                return GreenBrush;
            
            return YellowBrush;
        }

        private void StrategyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DevicesGrid?.Items.Refresh();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists("config.json"))
            {
                _config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"))
                ?? new Config();
            }
            else
            {
                _config = new Config();
            }
            RestartDevicesCheckBox.IsChecked = _config.RestartDevices;
            OptimizeMiscDevicesCheckBox.IsChecked = _config.OptimizeMiscDevices;
            StrategyComboBox.SelectedIndex = _config.OptimizationStrategy;
            int[] coreOptions = { 1, 2, 3, 4 };
            VideoCoresComboBox.SelectedIndex = Math.Max(0, Array.IndexOf(coreOptions, _config.VideoCoreCount));
        }

        private void SaveConfig()
        {
            _config.RestartDevices = RestartDevicesCheckBox.IsChecked ?? true;
            _config.OptimizeMiscDevices = OptimizeMiscDevicesCheckBox.IsChecked ?? true;
            _config.OptimizationStrategy = StrategyComboBox.SelectedIndex;
            int[] coreOptions = { 1, 2, 3, 4 };
            _config.VideoCoreCount = coreOptions[VideoCoresComboBox.SelectedIndex];
            File.WriteAllText("config.json", JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public class InterruptSupportConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint interruptSupport = (uint)value;
            string res = "Unknown";

            if ((interruptSupport & 0x1) != 0)
            {
                res = "Line Based";
            }

            if ((interruptSupport & 0x2) != 0)
            {
                res += (res.Length > 0 ? ", " : "") + "MSI";
            }

            if ((interruptSupport & 0x4) != 0)
            {
                res += (res.Length > 0 ? ", " : "") + "MSI-X";
            }

            return res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DevicePolicyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int policy = (int)value;
            return policy switch
            {
                0 => "Machine Default",
                1 => "All Close Processors",
                2 => "One Close Processor",
                3 => "All Processors in Machine",
                4 => "Specified Processors",
                5 => "Spread Messages Across All Processors",
                6 => "All Processors in Machine When Steered",
                _ => "Undefined"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DevicePriorityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int priority = (int)value;
            return priority switch
            {
                1 => "Low",
                2 => "Normal",
                3 => "High",
                _ => "Undefined"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class AssignmentSetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Int64 assignmentSet = (Int64)value;
            if (assignmentSet == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 64; i++)
            {
                if ((assignmentSet & (1L << i)) != 0)
                {
                    sb.Append(i + " ");
                }
            }
            return sb.ToString().Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}