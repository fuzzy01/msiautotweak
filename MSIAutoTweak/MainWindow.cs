using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.IO;

namespace MSIAutoTweak
{
    public class Config
    {
        public bool RestartDevices { get; set; } = true;
        public bool OptimizeMiscDevices { get; set; } = true;
    }

    public partial class MainWindow : Window
    {
        private Config _config;
        private readonly MSIOptimizer _msiOptimizer;

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
                _msiOptimizer.Optimize();
                MessageBox.Show($"Optimization completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during optimization: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
        }

        private void SaveConfig()
        {
            _config.RestartDevices = RestartDevicesCheckBox.IsChecked ?? true;
            _config.OptimizeMiscDevices = OptimizeMiscDevicesCheckBox.IsChecked ?? true;
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
            UInt64 assignmentSet = (UInt64)value;
            if (assignmentSet == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 64; i++)
            {
                if ((assignmentSet & (1UL << i)) != 0)
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