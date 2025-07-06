using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace FFLocker
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
        }

        private void DonateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://coff.ee/psppspnaik209",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // Could not open the link
            }
        }
    }
}
