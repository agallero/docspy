using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.BadgeNotifications;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DocSpy
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        internal static MainWindow MainWindow { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                if (args[1].Equals("--light", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Current.RequestedTheme = Microsoft.UI.Xaml.ApplicationTheme.Light;
                }
                else if (args[1].Equals("--dark", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Current.RequestedTheme = Microsoft.UI.Xaml.ApplicationTheme.Dark;
                }
            }

            InitializeComponent();
            UnhandledException += App_UnhandledException;
            BadgeNotificationManager.Current.ClearBadge(); //silly thing survives app restarts

        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (MainWindow != null)
            {
                ContentDialog customContentDialog = new()
                {
                    Title = "Error",
                    Content = e.Exception.Message,
                    CloseButtonText = "OK",

                    XamlRoot = MainWindow.Content.XamlRoot
                };

                await customContentDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            MainWindow.Closed += (s, e) =>
            {
                BadgeNotificationManager.Current.ClearBadge();
            };
        }
    }
}
