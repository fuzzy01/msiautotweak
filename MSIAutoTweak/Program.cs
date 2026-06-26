using System.IO;
using System.Windows;

namespace MSIAutoTweak
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory); // Set current directory to the executable's location        

            Application app = new App();
#if NET9_0_OR_GREATER
#pragma warning disable WPF0001
            // app.ThemeMode = System.Windows.ThemeMode.Light; // Set the theme mode to Light for .NET 9.0 or greater
#endif
            app.Run(new MainWindow());
        }
    }
}