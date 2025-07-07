using FFLocker.Logic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace FFLocker
{
    public sealed partial class MainWindow : Window
    {
        private Logic.AppSettings _settings = new Logic.AppSettings();

        public MainWindow()
        {
            this.InitializeComponent();
            LoadSettings();
            ApplyTheme();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            
            string iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "256x256_icon.ico");
            appWindow.SetIcon(iconPath);
            
            appWindow.Resize(new Windows.Graphics.SizeInt32(640, 600));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.IsMaximizable = false;
            }

            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                SetTitleBar(AppTitleBar);
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = await PickPathWithCom();
                if (!string.IsNullOrEmpty(path))
                {
                    PathTextBox.Text = path;
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"An error occurred while opening the browser: {ex.Message}");
            }
        }

        private Task<string?> PickPathWithCom()
        {
            bool isFolderPicker = FolderRadioButton.IsChecked == true;

            return Task.Run(() =>
            {
                var dialog = (IFileOpenDialog)new FileOpenDialogCoClass();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

                try
                {
                    FOS options;
                    dialog.GetOptions(out options);

                    if (isFolderPicker)
                    {
                        options |= FOS.FOS_PICKFOLDERS;
                    }
                    else
                    {
                        options |= FOS.FOS_FILEMUSTEXIST;
                    }
                    dialog.SetOptions(options);

                    if (dialog.Show(hwnd) == 0) // 0 is S_OK
                    {
                        IShellItem result;
                        dialog.GetResult(out result);
                        string path;
                        result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);
                        return path;
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(dialog);
                }
                return null;
            });
        }

        private async void LockButton_Click(object sender, RoutedEventArgs e)
        {
            var path = PathTextBox.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                await ShowMessage("Please select a file or folder.");
                return;
            }

            if (DualMetadataManager.IsLocked(path))
            {
                await ShowMessage("The selected file or folder is already locked.");
                return;
            }

            var password = await GetPassword();
            if (string.IsNullOrEmpty(password)) return;

            var progress = new Progress<int>(p => { });
            var logger = new Progress<string>(m => Log(m));

            try
            {
                using (var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(password)))
                {
                    System.Text.Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);
                    await Task.Run(() => EncryptionManager.Lock(path, passwordBuffer, progress, logger));
                    await ShowMessage("Lock successful!");
                    PathTextBox.Text = "";
                    PopulateLockedItems();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"An error occurred: {ex.Message}");
            }
        }

        private async void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            var path = PathTextBox.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                await ShowMessage("Please select a file or folder.");
                return;
            }

            if (!DualMetadataManager.IsLocked(path))
            {
                await ShowMessage("The selected file or folder is not locked.");
                return;
            }

            var password = await GetPassword();
            if (string.IsNullOrEmpty(password)) return;

            var progress = new Progress<int>(p => { });
            var logger = new Progress<string>(m => Log(m));

            try
            {
                using (var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(password)))
                {
                    System.Text.Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);
                    await Task.Run(() => EncryptionManager.Unlock(path, passwordBuffer, progress, logger));
                    await ShowMessage("Unlock successful!");
                    PathTextBox.Text = "";
                    PopulateLockedItems();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"An error occurred: {ex.Message}");
            }
        }

        private void ShowInfoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LogGrid.Visibility = Visibility.Visible;
        }

        private void ShowInfoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LogGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowLockedButton_Click(object sender, RoutedEventArgs e)
        {
            if (LockedItemsPanel.Visibility == Visibility.Visible)
            {
                LockedItemsPanel.Visibility = Visibility.Collapsed;
                ShowLockedButton.Content = "Show Locked Items";
            }
            else
            {
                LockedItemsPanel.Visibility = Visibility.Visible;
                ShowLockedButton.Content = "Hide Locked Items";
                PopulateLockedItems();
            }
        }

        private bool _isHandlingContextMenuCheck = false;

        private async void ContextMenuCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isHandlingContextMenuCheck) return;
            _isHandlingContextMenuCheck = true;

            if (RegistryManager.IsAdmin())
            {
                await Task.Run(() => RegistryManager.AddContextMenu());
                await ShowRestartExplorerPrompt();
            }
            else
            {
                await ShowMessage("This feature requires administrator privileges.");
                ContextMenuCheckBox.IsChecked = false;
            }
            _isHandlingContextMenuCheck = false;
        }

        private async void ContextMenuCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isHandlingContextMenuCheck) return;
            _isHandlingContextMenuCheck = true;

            if (RegistryManager.IsAdmin())
            {
                await Task.Run(() => RegistryManager.RemoveContextMenu());
                await ShowRestartExplorerPrompt();
            }
            else
            {
                await ShowMessage("This feature requires administrator privileges.");
                ContextMenuCheckBox.IsChecked = true;
            }
            _isHandlingContextMenuCheck = false;
        }

        private async Task ShowRestartExplorerPrompt()
        {
            var dialog = new ContentDialog
            {
                Title = "Restart Explorer",
                Content = "To see the changes, the Windows Explorer process needs to be restarted.\n\nDo you want to restart it now?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Task.Run(() => RegistryManager.RestartExplorer());
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _settings.Theme = (Logic.Theme)comboBox.SelectedIndex;
                ApplyTheme();
                SaveSettings();
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog
            {
                XamlRoot = this.Content.XamlRoot
            };
            await aboutDialog.ShowAsync();
        }

        private void PopulateLockedItems()
        {
            if (LockedItemsListView == null || LockedItemsViewComboBox == null) return;

            LockedItemsDatabase.Reload();
            LockedItemsListView.Items.Clear();
            var lockedItems = LockedItemsDatabase.GetLockedItems();
            var showOriginalNames = LockedItemsViewComboBox.SelectedIndex == 0;

            foreach (var item in lockedItems)
            {
                if (item != null)
                {
                    string typePrefix = item.IsFolder ? "[D] " : "[F] ";
                    string? pathToDisplay = showOriginalNames ? item.OriginalPath : item.LockedPath;
                    if (pathToDisplay != null)
                    {
                        LockedItemsListView.Items.Add(typePrefix + pathToDisplay);
                    }
                }
            }
        }

        private void LockedItemsViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateLockedItems();
        }

        private void Log(string message)
        {
            LogTextBox.Text += message + Environment.NewLine;
        }

        private async Task<string?> GetPassword()
        {
            var passwordBox = new PasswordBox
            {
                PasswordRevealMode = PasswordRevealMode.Peek
            };

            var dialog = new ContentDialog
            {
                Title = "Enter Password",
                Content = passwordBox,
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return passwordBox.Password;
            }
            return null;
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = string.Empty;
        }

        private async Task ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "FFLocker",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    _settings = System.Text.Json.JsonSerializer.Deserialize<Logic.AppSettings>(json) ?? new Logic.AppSettings();
                }
            }
            catch { }

            ThemeComboBox.SelectedIndex = (int)_settings.Theme;
            ContextMenuCheckBox.IsChecked = RegistryManager.IsContextMenuEnabled();
        }

        private void SaveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var json = System.Text.Json.JsonSerializer.Serialize(_settings);
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }

        private void ApplyTheme()
        {
            if (Content is FrameworkElement rootElement)
            {
                switch (_settings.Theme)
                {
                    case Logic.Theme.Light:
                        rootElement.RequestedTheme = ElementTheme.Light;
                        break;
                    case Logic.Theme.Dark:
                        rootElement.RequestedTheme = ElementTheme.Dark;
                        break;
                    case Logic.Theme.System:
                        rootElement.RequestedTheme = ElementTheme.Default;
                        break;
                }
            }
        }

        private void InitializeWithWindow(object picker)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        }
    }
}