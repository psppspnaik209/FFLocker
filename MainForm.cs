using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace FFLocker
{
    public partial class MainForm : Form
    {
        private enum Operation
        {
            None,
            Lock,
            Unlock
        }

        private Operation _currentOperation = Operation.None;
        private AppSettings _settings;

        public MainForm(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.rbFile = new System.Windows.Forms.RadioButton();
            this.rbFolder = new System.Windows.Forms.RadioButton();
            this.btnLock = new System.Windows.Forms.Button();
            this.btnUnlock = new System.Windows.Forms.Button();
            this.panelPassword = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.chkShowInfo = new System.Windows.Forms.CheckBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.chkDarkMode = new System.Windows.Forms.CheckBox();
            this.panelPassword.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(12, 12);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(399, 23);
            this.txtPath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(417, 12);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // rbFile
            // 
            this.rbFile.AutoSize = true;
            this.rbFile.Checked = true;
            this.rbFile.Location = new System.Drawing.Point(12, 41);
            this.rbFile.Name = "rbFile";
            this.rbFile.Size = new System.Drawing.Size(84, 19);
            this.rbFile.TabIndex = 2;
            this.rbFile.TabStop = true;
            this.rbFile.Text = "Select File";
            this.rbFile.UseVisualStyleBackColor = true;
            // 
            // rbFolder
            // 
            this.rbFolder.AutoSize = true;
            this.rbFolder.Location = new System.Drawing.Point(102, 41);
            this.rbFolder.Name = "rbFolder";
            this.rbFolder.Size = new System.Drawing.Size(98, 19);
            this.rbFolder.TabIndex = 3;
            this.rbFolder.Text = "Select Folder";
            this.rbFolder.UseVisualStyleBackColor = true;
            // 
            // btnLock
            // 
            this.btnLock.Location = new System.Drawing.Point(12, 66);
            this.btnLock.Name = "btnLock";
            this.btnLock.Size = new System.Drawing.Size(237, 23);
            this.btnLock.TabIndex = 4;
            this.btnLock.Text = "Lock";
            this.btnLock.UseVisualStyleBackColor = true;
            this.btnLock.Click += new System.EventHandler(this.btnLock_Click);
            // 
            // btnUnlock
            // 
            this.btnUnlock.Location = new System.Drawing.Point(255, 66);
            this.btnUnlock.Name = "btnUnlock";
            this.btnUnlock.Size = new System.Drawing.Size(237, 23);
            this.btnUnlock.TabIndex = 5;
            this.btnUnlock.Text = "Unlock";
            this.btnUnlock.UseVisualStyleBackColor = true;
            this.btnUnlock.Click += new System.EventHandler(this.btnUnlock_Click);
            // 
            // panelPassword
            // 
            this.panelPassword.Controls.Add(this.btnCancel);
            this.panelPassword.Controls.Add(this.btnConfirm);
            this.panelPassword.Controls.Add(this.txtPassword);
            this.panelPassword.Controls.Add(this.lblPassword);
            this.panelPassword.Location = new System.Drawing.Point(12, 95);
            this.panelPassword.Name = "panelPassword";
            this.panelPassword.Size = new System.Drawing.Size(480, 40);
            this.panelPassword.TabIndex = 6;
            this.panelPassword.Visible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(399, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnConfirm
            // 
            this.btnConfirm.Location = new System.Drawing.Point(318, 8);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(75, 23);
            this.btnConfirm.TabIndex = 2;
            this.btnConfirm.Text = "Confirm";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(65, 8);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(247, 23);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.UseSystemPasswordChar = true;
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(3, 11);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(60, 15);
            this.lblPassword.TabIndex = 0;
            this.lblPassword.Text = "Password:";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 141);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(480, 23);
            this.progressBar.TabIndex = 7;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 167);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(39, 15);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Ready";
            // 
            // chkShowInfo
            // 
            this.chkShowInfo.AutoSize = true;
            this.chkShowInfo.Location = new System.Drawing.Point(12, 185);
            this.chkShowInfo.Name = "chkShowInfo";
            this.chkShowInfo.Size = new System.Drawing.Size(107, 19);
            this.chkShowInfo.TabIndex = 9;
            this.chkShowInfo.Text = "Show more info";
            this.chkShowInfo.UseVisualStyleBackColor = true;
            this.chkShowInfo.CheckedChanged += new System.EventHandler(this.chkShowInfo_CheckedChanged);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 210);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(480, 150);
            this.txtLog.TabIndex = 10;
            this.txtLog.Visible = false;
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(125, 181);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 11;
            this.btnClearLog.Text = "Clear";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Visible = false;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // chkDarkMode
            // 
            this.chkDarkMode.AutoSize = true;
            this.chkDarkMode.Location = new System.Drawing.Point(403, 185);
            this.chkDarkMode.Name = "chkDarkMode";
            this.chkDarkMode.Size = new System.Drawing.Size(89, 19);
            this.chkDarkMode.TabIndex = 12;
            this.chkDarkMode.Text = "Dark Mode";
            this.chkDarkMode.UseVisualStyleBackColor = true;
            this.chkDarkMode.CheckedChanged += new System.EventHandler(this.chkDarkMode_CheckedChanged);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(504, 371);
            this.Controls.Add(this.chkDarkMode);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.chkShowInfo);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.panelPassword);
            this.Controls.Add(this.btnUnlock);
            this.Controls.Add(this.btnLock);
            this.Controls.Add(this.rbFolder);
            this.Controls.Add(this.rbFile);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtPath);
            this.Name = "MainForm";
            this.Text = "FFLocker";
            this.panelPassword.ResumeLayout(false);
            this.panelPassword.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtPath = null!;
        private System.Windows.Forms.Button btnBrowse = null!;
        private System.Windows.Forms.RadioButton rbFile = null!;
        private System.Windows.Forms.RadioButton rbFolder = null!;
        private System.Windows.Forms.Button btnLock = null!;
        private System.Windows.Forms.Button btnUnlock = null!;
        private System.Windows.Forms.Panel panelPassword = null!;
        private System.Windows.Forms.Button btnCancel = null!;
        private System.Windows.Forms.Button btnConfirm = null!;
        private System.Windows.Forms.TextBox txtPassword = null!;
        private System.Windows.Forms.Label lblPassword = null!;
        private System.Windows.Forms.ProgressBar progressBar = null!;
        private System.Windows.Forms.Label lblStatus = null!;
        private System.Windows.Forms.CheckBox chkShowInfo = null!;
        private System.Windows.Forms.TextBox txtLog = null!;
        private System.Windows.Forms.Button btnClearLog = null!;
        private System.Windows.Forms.CheckBox chkDarkMode = null!;

        private void ApplyTheme()
        {
            chkDarkMode.Checked = _settings.DarkMode;
            var foreColor = _settings.DarkMode ? Color.White : SystemColors.ControlText;
            var backColor = _settings.DarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;

            this.ForeColor = foreColor;
            this.BackColor = backColor;

            foreach (Control c in this.Controls)
            {
                UpdateColor(c, foreColor, backColor);
            }
        }

        private void UpdateColor(Control c, Color foreColor, Color backColor)
        {
            c.ForeColor = foreColor;
            c.BackColor = backColor;
            foreach (Control child in c.Controls)
            {
                UpdateColor(child, foreColor, backColor);
            }
        }

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
                
                txtPath.Text = ""; // Clear selection on success
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
                e.SuppressKeyPress = true; // Prevents the 'ding' sound
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
            // This is to re-enable the main UI after an operation
            SetMainUIEnabled(enabled);
        }
    }
}
