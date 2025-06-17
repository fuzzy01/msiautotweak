using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace MSIAutoTweak
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory); // Set current directory to the executable's location        

            Application app = new Application();
            app.Run(new MainWindow());
        }
    }
}