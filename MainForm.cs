using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace FFLocker
{
    public partial class MainForm : Form
    {
        #region UI Theming
        private static class Theme
        {
            public static class Dark
            {
                public static Color Back = Color.FromArgb(37, 37, 38);
                public static Color Fore = Color.FromArgb(241, 241, 241);
                public static Color ControlBack = Color.FromArgb(63, 63, 70);
                public static Color ControlHover = Color.FromArgb(80, 80, 85);
                public static Color Accent = Color.FromArgb(0, 122, 204);
            }

            public static class Light
            {
                public static Color Back = SystemColors.Control;
                public static Color Fore = SystemColors.ControlText;
                public static Color ControlBack = SystemColors.Window;
                public static Color ControlHover = Color.FromArgb(229, 241, 251);
                public static Color Accent = Color.FromArgb(0, 122, 204);
            }
        }
        #endregion

        private enum Operation { None, Lock, Unlock }
        private Operation _currentOperation = Operation.None;
        private AppSettings _settings;
        private ToolTip _toolTip = new ToolTip();

        public MainForm(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            ApplyTheme();
            LoadWindowPosition();
            InitializeToolTips();

            // Unsubscribe from the event before setting the initial state
            chkContextMenu.CheckedChanged -= chkContextMenu_CheckedChanged;
            chkContextMenu.Checked = RegistryManager.IsContextMenuEnabled();
            // Subscribe back to the event
            chkContextMenu.CheckedChanged += chkContextMenu_CheckedChanged;
        }

        private void InitializeToolTips()
        {
            _toolTip.SetToolTip(chkShowInfo, "Show detailed logs of the application's operations.");
            _toolTip.SetToolTip(chkDarkMode, "Toggle between light and dark themes.");

            if (!RegistryManager.IsAdmin())
            {
                _toolTip.SetToolTip(chkContextMenu, "Run as administrator to enable/disable the context menu.");
            }
            else
            {
                _toolTip.SetToolTip(chkContextMenu, "Enable/disable the FFLocker context menu in Windows Explorer.");
            }
        }

        private void ApplyTheme()
        {
            chkDarkMode.Checked = _settings.DarkMode;

            Color backColor, foreColor, controlBackColor, controlForeColor;

            if (_settings.DarkMode)
            {
                backColor = Theme.Dark.Back;
                foreColor = Theme.Dark.Fore;
                controlBackColor = Theme.Dark.ControlBack;
                controlForeColor = Theme.Dark.Fore;
            }
            else
            {
                backColor = Theme.Light.Back;
                foreColor = Theme.Light.Fore;
                controlBackColor = Theme.Light.ControlBack;
                controlForeColor = Theme.Light.Fore;
            }

            this.BackColor = backColor;
            this.ForeColor = foreColor;

            foreach (Control c in this.Controls)
            {
                UpdateColor(c, foreColor, backColor, controlBackColor, controlForeColor);
            }
        }

        private void UpdateColor(Control c, Color foreColor, Color backColor, Color controlBackColor, Color controlForeColor)
        {
            c.ForeColor = foreColor;
            c.BackColor = backColor;

            if (c is TextBox || c is Button || c is CheckBox || c is RadioButton)
            {
                c.BackColor = controlBackColor;
                c.ForeColor = controlForeColor;
            }

            foreach (Control child in c.Controls)
            {
                UpdateColor(child, foreColor, backColor, controlBackColor, controlForeColor);
            }
        }

        #region Window Position Handling
        private void LoadWindowPosition()
        {
            if (_settings.WindowSize != Size.Empty)
            {
                bool isOnScreen = false;
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.IntersectsWith(new Rectangle(_settings.WindowLocation, _settings.WindowSize)))
                    {
                        isOnScreen = true;
                        break;
                    }
                }

                if (isOnScreen)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = _settings.WindowLocation;
                    this.Size = _settings.WindowSize;
                    this.WindowState = _settings.WindowMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
                }
            }
        }

        private void SaveWindowPosition()
        {
            _settings.WindowMaximized = this.WindowState == FormWindowState.Maximized;
            if (this.WindowState == FormWindowState.Normal)
            {
                _settings.WindowLocation = this.Location;
                _settings.WindowSize = this.Size;
            }
            else
            {
                _settings.WindowLocation = this.RestoreBounds.Location;
                _settings.WindowSize = this.RestoreBounds.Size;
            }
            Program.SaveSettings(_settings);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveWindowPosition();
        }
        #endregion

        #region UI Event Handlers
        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            if (rbFile.Checked)
            {
                using (var ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = ofd.FileName;
                    }
                }
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void btnLock_Click(object? sender, EventArgs e)
        {
            string path = txtPath.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Please select a file or folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (DualMetadataManager.IsLocked(path))
            {
                MessageBox.Show("The selected file or folder is already locked.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _currentOperation = Operation.Lock;
            ShowPasswordPanel();
        }

        private void btnUnlock_Click(object? sender, EventArgs e)
        {
            string path = txtPath.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Please select a file or folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!DualMetadataManager.IsLocked(path))
            {
                MessageBox.Show("The selected file or folder is not locked.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _currentOperation = Operation.Unlock;
            ShowPasswordPanel();
        }

        private void ShowPasswordPanel()
        {
            panelPassword.Visible = true;
            SetMainUIEnabled(false);
            txtPassword.Focus();
        }

        private void HidePasswordPanel()
        {
            panelPassword.Visible = false;
            txtPassword.Text = "";
            SetMainUIEnabled(true);
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            HidePasswordPanel();
        }

        private async void btnConfirm_Click(object? sender, EventArgs e)
        {
            string path = txtPath.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            HidePasswordPanel();
            SetOperationUIEnabled(false);

            var progress = new Progress<int>(p => progressBar.Value = p);
            var logger = new Progress<string>(m => {
                if (chkShowInfo.Checked) {
                    txtLog.AppendText(m + Environment.NewLine);
                }
            });

            try
            {
                var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(password));
                System.Text.Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);

                if (_currentOperation == Operation.Lock)
                {
                    await Task.Run(() => Program.Lock(path, passwordBuffer, progress, (IProgress<string>)logger));
                    lblStatus.Text = "Lock operation completed successfully!";
                }
                else if (_currentOperation == Operation.Unlock)
                {
                    await Task.Run(() => Program.Unlock(path, passwordBuffer, progress, (IProgress<string>)logger));
                    lblStatus.Text = "Unlock operation completed successfully!";
                }
                
                txtPath.Text = "";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "An error occurred.";
                ((IProgress<string>)logger).Report(ex.ToString());
            }
            finally
            {
                _currentOperation = Operation.None;
                SetOperationUIEnabled(true);
                progressBar.Value = 0;
            }
        }

        private void txtPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnConfirm.PerformClick();
            }
        }

        private void chkShowInfo_CheckedChanged(object? sender, EventArgs e)
        {
            txtLog.Visible = chkShowInfo.Checked;
            btnClearLog.Visible = chkShowInfo.Checked;
        }

        private void btnClearLog_Click(object? sender, EventArgs e)
        {
            txtLog.Text = "";
        }

        private void chkDarkMode_CheckedChanged(object? sender, EventArgs e)
        {
            _settings.DarkMode = chkDarkMode.Checked;
            Program.SaveSettings(_settings);
            ApplyTheme();
        }

        private void chkContextMenu_CheckedChanged(object? sender, EventArgs e)
        {
            if (!RegistryManager.IsAdmin())
            {
                MessageBox.Show("This feature requires administrator privileges. Please restart FFLocker as an administrator.", "Administrator Privileges Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Unsubscribe, change, and re-subscribe to prevent the event from firing again
                chkContextMenu.CheckedChanged -= chkContextMenu_CheckedChanged;
                chkContextMenu.Checked = false;
                chkContextMenu.CheckedChanged += chkContextMenu_CheckedChanged;
                return;
            }

            if (chkContextMenu.Checked)
            {
                RegistryManager.AddContextMenu();
            }
            else
            {
                RegistryManager.RemoveContextMenu();
            }
            _settings.ContextMenuEnabled = RegistryManager.IsContextMenuEnabled();
            Program.SaveSettings(_settings);

            // If the operation failed, revert the checkbox state
            if (chkContextMenu.Checked != _settings.ContextMenuEnabled)
            {
                chkContextMenu.CheckedChanged -= chkContextMenu_CheckedChanged;
                chkContextMenu.Checked = _settings.ContextMenuEnabled;
                chkContextMenu.CheckedChanged += chkContextMenu_CheckedChanged;
            }
        }

        private void SetMainUIEnabled(bool enabled)
        {
            txtPath.Enabled = enabled;
            btnBrowse.Enabled = enabled;
            rbFile.Enabled = enabled;
            rbFolder.Enabled = enabled;
            btnLock.Enabled = enabled;
            btnUnlock.Enabled = enabled;
        }
        
        private void SetOperationUIEnabled(bool enabled)
        {
            SetMainUIEnabled(enabled);
        }
        #endregion
    }
}