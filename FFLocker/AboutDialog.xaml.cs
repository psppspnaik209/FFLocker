using Microsoft.UI.Xaml;
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
            this.Loaded += AboutDialog_Loaded;
        }

        private void AboutDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot?.Content is FrameworkElement rootElement)
            {
                this.RequestedTheme = rootElement.RequestedTheme;
            }
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
