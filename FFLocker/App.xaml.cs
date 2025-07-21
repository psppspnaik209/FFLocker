using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Linq;

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
            // The new CLI logic is handled by the main window.
            // We pass the command-line arguments to the main window's constructor.
            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            _window = new MainWindow(cmdLineArgs);
            _window.Activate();
        }
    }
}
