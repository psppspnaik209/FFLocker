using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFLocker
{
    public partial class PasswordForm : Form
    {
        private readonly string _targetPath;
        private readonly string _operation; // "lock" or "unlock"

        public PasswordForm(string operation, string targetPath)
        {
            InitializeComponent();
            _operation = operation;
            _targetPath = targetPath;

            this.Text = $"FFLocker - {char.ToUpper(operation[0]) + operation.Substring(1)}";
            lblPasswordPrompt.Text = $"Enter password to {operation} this item:";
            lblAction.Text = Path.GetFullPath(_targetPath);
            
            try
            {
                this.Icon = new Icon(@"img\256x256_icon.ico");
            }
            catch
            {
                // Icon not found, continue without it
            }
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text;
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter a password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetUIEnabled(false);
            lblStatus.Text = "Processing...";

            var progress = new Progress<int>(p => progressBar.Value = p);
            var logger = new Progress<string>(m => lblStatus.Text = m);

            try
            {
                using (var passwordBuffer = new SecureBuffer(System.Text.Encoding.UTF8.GetByteCount(password)))
                {
                    System.Text.Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);

                    if (_operation == "lock")
                    {
                        await Task.Run(() => Program.Lock(_targetPath, passwordBuffer, progress, logger));
                    }
                    else // unlock
                    {
                        await Task.Run(() => Program.Unlock(_targetPath, passwordBuffer, progress, logger));
                    }
                }

                lblStatus.Text = "Operation completed successfully!";
                progressBar.Value = 100;

                string successMessage = _operation == "lock"
                    ? "Operation completed successfully.\n\nYou can view all locked items in the main FFLocker application."
                    : "Operation completed successfully.";

                MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "An error occurred.";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetUIEnabled(true);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnConfirm.PerformClick();
            }
        }

        private void SetUIEnabled(bool enabled)
        {
            txtPassword.Enabled = enabled;
            btnConfirm.Enabled = enabled;
            btnCancel.Enabled = enabled;
        }
    }
}
