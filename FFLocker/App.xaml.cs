using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using FFLocker.Logic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace FFLocker
{
    public partial class App : Application
    {
        private Window? _window;

        #region P/Invoke Declarations for Console

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const uint OPEN_EXISTING = 3;
        private const uint CP_UTF8 = 65001;

        #endregion

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
                if (AllocConsole())
                {
                    try
                    {
                        // Set console encoding to UTF-8 to correctly display ASCII art
                        SetConsoleOutputCP(CP_UTF8);
                        SetConsoleCP(CP_UTF8);

                        InitializeConsoleHandles();

                        if (cmdLineArgs.Length == 3 && (cmdLineArgs[1].Equals("lock", StringComparison.OrdinalIgnoreCase) || cmdLineArgs[1].Equals("unlock", StringComparison.OrdinalIgnoreCase)))
                        {
                            var operation = cmdLineArgs[1];
                            var path = cmdLineArgs[2];
                            await CliManager.Handle(operation, path);
                        }
                        else
                        {
                            Console.WriteLine("Invalid command-line arguments. Usage: FFLocker.exe [lock|unlock] \"<path>\"");
                            Console.WriteLine("\nPress any key to exit...");
                            Console.ReadKey();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        Console.WriteLine("\nPress any key to exit...");
                        Console.ReadKey();
                    }
                    finally
                    {
                        FreeConsole();
                    }
                }

                Current.Exit();
                return;
            }

            _window = new MainWindow();
            _window.Activate();
        }

        private static void InitializeConsoleHandles()
        {
            var hIn = CreateFile("CONIN$", GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            var hOut = CreateFile("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            SetStdHandle(STD_INPUT_HANDLE, hIn);
            SetStdHandle(STD_OUTPUT_HANDLE, hOut);
            SetStdHandle(STD_ERROR_HANDLE, hOut);

            var encoding = Encoding.UTF8;
            Console.SetIn(new StreamReader(new FileStream(new SafeFileHandle(hIn, false), FileAccess.Read), encoding));
            Console.SetOut(new StreamWriter(new FileStream(new SafeFileHandle(hOut, false), FileAccess.Write), encoding) { AutoFlush = true });
            Console.SetError(new StreamWriter(new FileStream(new SafeFileHandle(hOut, false), FileAccess.Write), encoding) { AutoFlush = true });
        }
    }
}
