using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace FFLocker
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            this.ActiveControl = this.btnDonate;
            txtLicenses.Text = "MIT License\r\n\r\n" +
                               "Copyright (c) 2025 TNBB Team\r\n\r\n" +
                               "Permission is hereby granted, free of charge, to any person obtaining a copy " +
                               "of this software and associated documentation files (the \"Software\"), to deal " +
                               "in the Software without restriction, including without limitation the rights " +
                               "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell " +
                               "copies of the Software, and to permit persons to whom the Software is " +
                               "furnished to do so, subject to the following conditions:\r\n\r\n" +
                               "The above copyright notice and this permission notice shall be included in all " +
                               "copies or substantial portions of the Software.\r\n\r\n" +
                               "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
                               "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                               "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE " +
                               "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
                               "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
                               "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE " +
                               "SOFTWARE.";
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://coff.ee/psppspnaik209",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
