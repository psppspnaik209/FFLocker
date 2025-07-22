using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FFLocker
{
    public partial class App : Application
    {
        private Window? _window;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        public App()
        {
            Bootstrap.Initialize(0x00010007);
            this.InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            if (cmdLineArgs.Length > 1)
            {
                try
                {
                    AllocConsole();
                    await CliManager.Handle(cmdLineArgs[1], cmdLineArgs.Length > 2 ? cmdLineArgs[2] : "");
                }
                finally
                {
                    FreeConsole();
                    // Exit the application after the CLI command has been handled
                    Application.Current.Exit();
                }
            }
            else
            {
                _window = new MainWindow();
                _window.Activate();
            }
        }
    }
}
