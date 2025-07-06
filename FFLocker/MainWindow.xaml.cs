using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using FFLocker.Logic;

namespace FFLocker
{
    public sealed partial class MainWindow : Window
    {
        private AppSettings _settings = new AppSettings();

        public MainWindow()
        {
            this.InitializeComponent();
            LoadSettings();
            ApplyTheme();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            
            appWindow.Resize(new Windows.Graphics.SizeInt32(540, 600));

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
            if (FileRadioButton.IsChecked == true)
            {
                var openPicker = new FileOpenPicker();
                openPicker.FileTypeFilter.Add("*");
                InitializeWithWindow(openPicker);
                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    PathTextBox.Text = file.Path;
                }
            }
            else
            {
                var folderPicker = new FolderPicker();
                InitializeWithWindow(folderPicker);
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    PathTextBox.Text = folder.Path;
                }
            }
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
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"An error occurred: {ex.Message}");
            }
        }

        private void ShowInfoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LogTextBox.Visibility = Visibility.Visible;
            LockedItemsListView.Visibility = Visibility.Collapsed;
        }

        private void ShowInfoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LogTextBox.Visibility = Visibility.Collapsed;
        }

        private void ShowLockedButton_Click(object sender, RoutedEventArgs e)
        {
            if (LockedItemsListView.Visibility == Visibility.Visible)
            {
                LockedItemsListView.Visibility = Visibility.Collapsed;
                ShowLockedButton.Content = "Show Locked Items";
            }
            else
            {
                LockedItemsListView.Visibility = Visibility.Visible;
                ShowLockedButton.Content = "Hide Locked Items";
                PopulateLockedItems();
            }
        }

        private void ContextMenuCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (RegistryManager.IsAdmin())
            {
                RegistryManager.AddContextMenu();
            }
            else
            {
                ShowMessage("This feature requires administrator privileges.").GetAwaiter();
                ContextMenuCheckBox.IsChecked = false;
            }
        }

        private void ContextMenuCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (RegistryManager.IsAdmin())
            {
                RegistryManager.RemoveContextMenu();
            }
            else
            {
                ShowMessage("This feature requires administrator privileges.").GetAwaiter();
                ContextMenuCheckBox.IsChecked = true;
            }
        }

        private void DarkMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                _settings.DarkMode = toggleSwitch.IsOn;
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
            LockedItemsDatabase.Reload();
            LockedItemsListView.Items.Clear();
            var lockedItems = LockedItemsDatabase.GetLockedItems();

            foreach (var item in lockedItems)
            {
                string typePrefix = item.IsFolder ? "[D] " : "[F] ";
                LockedItemsListView.Items.Add(typePrefix + item.OriginalPath);
            }
        }

        private void Log(string message)
        {
            if (ShowInfoCheckBox.IsChecked == true)
            {
                LogTextBox.Text += message + Environment.NewLine;
            }
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
                    _settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }

            DarkModeCheckBox.IsOn = _settings.DarkMode;
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
                rootElement.RequestedTheme = _settings.DarkMode ? ElementTheme.Dark : ElementTheme.Light;
            }
        }

        private void InitializeWithWindow(object picker)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        }
    }
}