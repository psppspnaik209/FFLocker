using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using FFLocker.Logic;

namespace FFLocker
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            Bootstrap.Initialize(0x00010007);
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            if (cmdLineArgs.Length == 3 && (cmdLineArgs[1] == "lock" || cmdLineArgs[1] == "unlock"))
            {
                var operation = cmdLineArgs[1];
                var path = cmdLineArgs[2];
                HandleCommandLine(operation, path);
                return;
            }

            _window = new MainWindow();
            _window.Activate();
        }

        private async void HandleCommandLine(string operation, string path)
        {
            var password = await GetPassword();
            if (string.IsNullOrEmpty(password))
            {
                Current.Exit();
                return;
            }

            var progress = new Progress<int>(p => { });
            var logger = new Progress<string>(m => { });

            try
            {
                using (var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(password)))
                {
                    System.Text.Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);
                    if (operation == "lock")
                    {
                        EncryptionManager.Lock(path, passwordBuffer, progress, logger);
                    }
                    else if (operation == "unlock")
                    {
                        await Task.Run(() => EncryptionManager.Unlock(path, passwordBuffer, progress, logger));
                    }
                }
            }
            catch (Exception)
            {
                // Intentionally left blank
            }
            finally
            {
                Current.Exit();
            }
        }

        private static async Task<string?> GetPassword()
        {
            var tempWindow = new Window() { Title = "FFLocker Password" };
            tempWindow.Activate();

            var passwordBox = new PasswordBox();
            var dialog = new ContentDialog
            {
                Title = "Enter Password",
                Content = passwordBox,
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                XamlRoot = tempWindow.Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();

            string? password = null;
            if (result == ContentDialogResult.Primary)
            {
                password = passwordBox.Password;
            }

            tempWindow.Close();
            return password;
        }
    }
}

