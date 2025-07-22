using FFLocker.Logic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace FFLocker
{
    public sealed partial class MainWindow : Window
    {
        private Logic.AppSettings _settings = new Logic.AppSettings();
        private IntPtr _hwnd;
        private bool _isLockedItemsViewAuthenticated = false;
        private bool _isWindowInitialized = false;

        public MainWindow()
        {
            this.InitializeComponent();
            
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
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
            
            LoadSettings();
            ApplyTheme();
            _isWindowInitialized = true;
        }

        

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = await PickPathAsync();
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

        private Task<string?> PickPathAsync()
        {
            // Get the state of the radio button on the UI thread.
            var isFolderPicker = FolderRadioButton.IsChecked == true;

            return Task.Run(() =>
            {
                var dialog = (IFileOpenDialog)new FileOpenDialogCoClass();
                dialog.SetOptions(FOS.FOS_PATHMUSTEXIST | FOS.FOS_FILEMUSTEXIST);

                // Use the captured state in the background thread.
                if (isFolderPicker)
                {
                    dialog.GetOptions(out var options);
                    dialog.SetOptions(options | FOS.FOS_PICKFOLDERS);
                }

                var hr = dialog.Show(_hwnd);
                if (hr == 0) // S_OK
                {
                    dialog.GetResult(out var item);
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    Marshal.ReleaseComObject(item);
                    Marshal.ReleaseComObject(dialog);
                    return path;
                }

                Marshal.ReleaseComObject(dialog);
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

            await PerformLockAsync(path);
        }

        private async Task PerformLockAsync(string path)
        {
            if (EncryptionManager.IsLocked(path))
            {
                await ShowMessage("The selected file or folder is already locked.");
                return;
            }

            var passwordResult = await GetPassword();
            if (passwordResult is null) return; // User canceled

            if (string.IsNullOrEmpty(passwordResult.Password))
            {
                await ShowMessage("A password is required.");
                return;
            }

            var progress = new Progress<int>(p => { });
            var logger = new Progress<string>(m => Log(m));

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            SetUiInteraction(false);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                byte[]? helloKey = null;
                if (passwordResult.UseHello)
                {
                    if (!await HelloManager.IsHelloSupportedAsync())
                    {
                        Log("Windows Hello is not supported on this device.");
                    }
                    else
                    {
                        Log("Requesting Windows Hello signature to protect the master key...");
                        helloKey = await HelloManager.GenerateHelloDerivedKeyAsync(_hwnd);

                        if (helloKey != null)
                        {
                            Log("Successfully derived key from Windows Hello signature.");
                        }
                        else
                        {
                            Log("Windows Hello operation was canceled or failed. Proceeding without it.");
                        }
                    }
                }

                using (var passwordBuffer = new SecureBuffer(Encoding.UTF8.GetByteCount(passwordResult.Password)))
                {
                    Encoding.UTF8.GetBytes(passwordResult.Password, 0, passwordResult.Password.Length, passwordBuffer.Buffer, 0);
                    // Pass the raw Hello-derived key to the encryption manager.
                    await Task.Run(() => EncryptionManager.Lock(path, passwordBuffer, progress, logger, token, helloKey), token);
                    sw.Stop();
                    Log($"Lock successful in {sw.Elapsed.TotalSeconds:F2}s.");
                    PathTextBox.Text = "";
                    if (LockedItemsPanel.Visibility == Visibility.Visible) PopulateLockedItems();
                }
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Log("[ERROR] Operation was canceled by the user.");
                await ShowMessage("The lock operation was canceled.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                await ShowMessage($"An error occurred: {ex.Message}");
                Log($"[ERROR] {ex.ToString()}");
            }
            finally
            {
                SetUiInteraction(true);
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

            await PerformUnlockAsync(path);
        }

        private async Task PerformUnlockAsync(string path)
        {
            if (!EncryptionManager.IsLocked(path))
            {
                await ShowMessage("The selected file or folder is not locked.");
                return;
            }

            bool isHelloUsed = EncryptionManager.IsHelloUsed(path);

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            if (isHelloUsed)
            {
                var result = await ShowHelloUnlockDialog();
                if (result == ContentDialogResult.Primary) // Unlock with Hello
                {
                    await UnlockWithHelloAsync(path, token);
                }
                else if (result == ContentDialogResult.Secondary) // Unlock with Password
                {
                    await UnlockWithPasswordAsync(path, token);
                }
            }
            else
            {
                await UnlockWithPasswordAsync(path, token);
            }
        }

        private async Task UnlockWithHelloAsync(string path, CancellationToken token)
        {
            var logger = new Progress<string>(m => Log(m));
            SetUiInteraction(false);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Log("Attempting to unlock with Windows Hello...");

                string? headerFilePath = null;
                if (Directory.Exists(path))
                {
                    headerFilePath = Directory.EnumerateFiles(path, "*.ffl").FirstOrDefault();
                }
                else if (File.Exists(path))
                {
                    headerFilePath = path;
                }

                if (string.IsNullOrEmpty(headerFilePath))
                {
                    throw new InvalidOperationException("No encrypted files found to unlock.");
                }
        
                var header = EncryptionManager.GetFileHeader(headerFilePath);
                if (header == null || !header.IsHelloUsed)
                {
                    throw new InvalidOperationException("This item was not locked with Windows Hello or the header is corrupt.");
                }

                Log("Requesting Windows Hello signature to recover the master key...");
                var helloKey = await HelloManager.GenerateHelloDerivedKeyAsync(_hwnd);
                if (helloKey == null)
                {
                    throw new Exception("Failed to unlock with Windows Hello. The operation was canceled or failed.");
                }

                // Recover the master key by XORing the protected key from the header with the new Hello-derived key.
                var masterKeyBytes = new byte[32];
                for (int i = 0; i < 32; i++)
                {
                    masterKeyBytes[i] = (byte)(header.HelloEncryptedKey[i] ^ helloKey[i]);
                }

                using var masterKeyBuffer = new SecureBuffer(32);
                Array.Copy(masterKeyBytes, masterKeyBuffer.Buffer, 32);

                await Task.Run(() => EncryptionManager.UnlockWithMasterKey(path, masterKeyBuffer, new Progress<int>(), logger, token), token);
                sw.Stop();
                Log($"Unlock successful in {sw.Elapsed.TotalSeconds:F2}s.");
                PathTextBox.Text = "";
                if (LockedItemsPanel.Visibility == Visibility.Visible) PopulateLockedItems();
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Log("[ERROR] Operation was canceled by the user.");
                await ShowMessage("The unlock operation was canceled.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                await ShowMessage($"An error occurred: {ex.Message}");
                Log($"[ERROR] {ex.ToString()}");
            }
            finally
            {
                SetUiInteraction(true);
            }
        }

        private async Task UnlockWithPasswordAsync(string path, CancellationToken token)
        {
            var passwordResult = await GetPassword(forUnlocking: true);
            if (passwordResult is null || string.IsNullOrEmpty(passwordResult.Password)) return;

            var progress = new Progress<int>(p => { });
            var logger = new Progress<string>(m => Log(m));

            SetUiInteraction(false);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(passwordResult.Password)))
                {
                    System.Text.Encoding.UTF8.GetBytes(passwordResult.Password, 0, passwordResult.Password.Length, passwordBuffer.Buffer, 0);
                    await Task.Run(() => EncryptionManager.Unlock(path, passwordBuffer, progress, logger, token), token);
                    sw.Stop();
                    Log($"Unlock successful in {sw.Elapsed.TotalSeconds:F2}s.");
                    PathTextBox.Text = "";
                    if (LockedItemsPanel.Visibility == Visibility.Visible) PopulateLockedItems();
                }
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Log("[ERROR] Operation was canceled by the user.");
                await ShowMessage("The unlock operation was canceled.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                await ShowMessage($"An error occurred: {ex.Message}");
            }
            finally
            {
                SetUiInteraction(true);
            }
        }

        private async Task<ContentDialogResult> ShowHelloUnlockDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "Unlock Option",
                Content = "This item was locked with Windows Hello. How would you like to unlock it?",
                PrimaryButtonText = "Unlock with Windows Hello",
                SecondaryButtonText = "Unlock with Password",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };
            return await dialog.ShowAsync();
        }

        private void ShowInfoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LogGrid.Visibility = Visibility.Visible;
            _settings.IsLogVisible = true;
            SaveSettings();
        }

        private void ShowInfoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LogGrid.Visibility = Visibility.Collapsed;
            _settings.IsLogVisible = false;
            SaveSettings();
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

                // If the current selection is not already "Locked Paths", change it.
                // This will trigger the SelectionChanged event, which populates the list.
                if (LockedItemsViewComboBox.SelectedIndex != 1)
                {
                    LockedItemsViewComboBox.SelectedIndex = 1;
                }
                else
                {
                    // If it's already "Locked Paths", the event won't fire, so populate manually.
                    PopulateLockedItems();
                }
            }
        }

        private bool _isHandlingContextMenuCheck = false;

        private async void ContextMenuCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isWindowInitialized || _isHandlingContextMenuCheck) return;
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
            if (!_isWindowInitialized || _isHandlingContextMenuCheck) return;
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

            LockedItemsListView.Items.Clear();
            var lockedItems = LockedItemsDatabase.GetLockedItems();
            var showOriginalNames = LockedItemsViewComboBox.SelectedIndex == 0;

            // If we are showing original names but are not authenticated, clear the list and exit.
            if (showOriginalNames && !_isLockedItemsViewAuthenticated)
            {
                LockedItemsListView.Items.Clear();
                return;
            }

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

        private async void LockedItemsViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isWindowInitialized) return;

            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // If the user wants to see original paths and is not yet authenticated
            if (comboBox.SelectedIndex == 0 && !_isLockedItemsViewAuthenticated)
            {
                Log("Windows Hello authentication required to view original paths.");
                var helloKey = await HelloManager.GenerateHelloDerivedKeyAsync(_hwnd);

                if (helloKey != null)
                {
                    _isLockedItemsViewAuthenticated = true;
                    Log("Authentication successful.");
                    PopulateLockedItems();
                }
                else
                {
                    Log("Authentication failed or was canceled.");
                    await ShowMessage("Authentication is required to view original paths.");
                    // Revert the selection back to "Locked Paths"
                    comboBox.SelectedIndex = 1;
                }
            }
            else
            {
                // If the user is already authenticated or is switching to "Locked Paths", just update the list.
                PopulateLockedItems();
            }
        }

        private void Log(string message)
        {
            if (DispatcherQueue.HasThreadAccess)
            {
                LogTextBox.Text += message + Environment.NewLine;
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LogTextBox.Text += message + Environment.NewLine;
                });
            }
        }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Cancel Operation",
                Content = "Are you sure you want to cancel? This may result in an inconsistent state or data loss.",
                PrimaryButtonText = "Yes, Cancel",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Log("Cancellation requested by user.");
                _cancellationTokenSource.Cancel();
            }
        }

        private void SetUiInteraction(bool isEnabled)
        {
            // Main action controls
            BrowseButton.IsEnabled = isEnabled;
            LockButton.IsEnabled = isEnabled;
            UnlockButton.IsEnabled = isEnabled;
            PathTextBox.IsEnabled = isEnabled;
            FolderRadioButton.IsEnabled = isEnabled;
            FileRadioButton.IsEnabled = isEnabled;

            // Cancel button is visible only when an operation is running
            CancelButton.Visibility = isEnabled ? Visibility.Collapsed : Visibility.Visible;

            // Locked items panel controls
            ShowLockedButton.IsEnabled = isEnabled;
            LockedItemsViewComboBox.IsEnabled = isEnabled;
            LockedItemsListView.IsEnabled = isEnabled;
            UseThisButton.IsEnabled = isEnabled;

            // Bottom bar controls
            ShowInfoCheckBox.IsEnabled = isEnabled;
            ContextMenuCheckBox.IsEnabled = isEnabled;
            ThemeComboBox.IsEnabled = isEnabled;
            AboutButton.IsEnabled = isEnabled;

            // The clear button should also be disabled during an operation
            ClearLogButton.IsEnabled = isEnabled;
        }

        private async Task<GetPasswordResult?> GetPassword(bool forUnlocking = false, bool isHelloAvailable = false)
        {
            var passwordBox = new PasswordBox
            {
                PasswordRevealMode = PasswordRevealMode.Peek
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(passwordBox);

            var helloCheckBox = new CheckBox
            {
                Content = "Use Windows Hello for faster unlocking",
                IsChecked = false,
                Margin = new Thickness(0, 10, 0, 0)
            };

            if (forUnlocking && isHelloAvailable)
            {
                // Special "Unlock with Hello" button could go here
            }
            else if (!forUnlocking)
            {
                stackPanel.Children.Add(helloCheckBox);
            }


            var dialog = new ContentDialog
            {
                Title = "Enter Password",
                Content = stackPanel,
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return new GetPasswordResult
                {
                    Password = passwordBox.Password,
                    UseHello = helloCheckBox.IsChecked ?? false
                };
            }
            return null;
        }

        private class GetPasswordResult
        {
            public string Password { get; set; } = string.Empty;
            public bool UseHello { get; set; }
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
            catch (Exception ex)
            {
                Log($"[ERROR] Failed to load settings: {ex.Message}");
            }

            ThemeComboBox.SelectedIndex = (int)_settings.Theme;
            ContextMenuCheckBox.IsChecked = RegistryManager.IsContextMenuEnabled();
            ShowInfoCheckBox.IsChecked = _settings.IsLogVisible;
            LogGrid.Visibility = _settings.IsLogVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveSettings()
        {
            if (!_isWindowInitialized) return;

            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                var json = System.Text.Json.JsonSerializer.Serialize(_settings);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Failed to save settings: {ex.Message}");
            }
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

        

        private async void UseThisButton_Click(object sender, RoutedEventArgs e)
        {
            if (LockedItemsListView.SelectedItem == null)
            {
                await ShowMessage("Please select an item from the list.");
                return;
            }

            var selectedItemString = LockedItemsListView.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedItemString)) return;

            // Remove the "[F] " or "[D] " prefix
            var path = selectedItemString.Substring(4);

            var allItems = LockedItemsDatabase.GetLockedItems();
            LockedItemInfo? itemToUse = allItems.FirstOrDefault(i => i.OriginalPath == path || i.LockedPath == path);

            if (itemToUse == null)
            {
                await ShowMessage("Could not find the selected item in the database.");
                return;
            }

            PathTextBox.Text = itemToUse.LockedPath;
            if (itemToUse.IsFolder)
            {
                FolderRadioButton.IsChecked = true;
            }
            else
            {
                FileRadioButton.IsChecked = true;
            }
        }
    }
}