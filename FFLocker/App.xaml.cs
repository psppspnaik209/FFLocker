using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using FFLocker.Logic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FFLocker
{
    public partial class App : Application
    {
        private Window? _window;

        #region P/Invoke Declarations

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;

        #endregion

        public App()
        {
            Bootstrap.Initialize(0x00010007);
            this.InitializeComponent();

            // If there are no command-line args, we are in GUI mode. Hide the console.
            if (Environment.GetCommandLineArgs().Length <= 1)
            {
                var handle = GetConsoleWindow();
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_HIDE);
                }
            }
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            if (cmdLineArgs.Length > 1)
            {
                // This is a command-line activation.
                // Since the OutputType is Exe, the console is already available.
                if (cmdLineArgs.Length == 3 && (cmdLineArgs[1].Equals("lock", StringComparison.OrdinalIgnoreCase) || cmdLineArgs[1].Equals("unlock", StringComparison.OrdinalIgnoreCase)))
                {
                    var operation = cmdLineArgs[1];
                    var path = cmdLineArgs[2];
                    await CliManager.Handle(operation, path);
                }
                else
                {
                    Console.WriteLine("Invalid command-line arguments. Usage: FFLocker.exe [lock|unlock] \"<path>\"");
                }

                // Exit after handling the command line operation.
                Current.Exit();
                return;
            }

            // This is a normal GUI activation.
            _window = new MainWindow();
            _window.Activate();
        }
    }
}

