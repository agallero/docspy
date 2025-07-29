using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;



namespace DocSpy
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "D👁️CSpy";
            AppWindow.SetIcon("Assets/DocSpy.ico");
            ServerCtrl.UpdateUINavigated = BrowserCtrl.UpdateWebView;
            Server.Instance.UpdateUIServerStarted = ServerCtrl.ActivatePanel;
            Server.Instance.UpdateUIServerStopped = ServerCtrl.DeactivatePanel;
            BrowserCtrl.UpdateUINavigated = ServerCtrl.ActivateRoot;
            BrowserCtrl.UpdateUIGoToSettings = SelectSettings;
            SettingsPage.UpdateUIGoToBrowser = ()=>
            {
                ServerCtrl.RecreatePanel();
                SelectBrowser();
            };
            ServerCtrl.UpdateUIRefresh = BrowserCtrl.ReloadWebView;

            if (Config.Instance.LoadConfigs())
            {
                ServerCtrl.RecreatePanel();
                SelectBrowser();
            }
            else
            {
                SelectSettings();
            }
        }

        private void SelectSettings()
        {
            ContentPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Visible;
        }

        private void SelectBrowser()
        {
            SettingsPage.Visibility = Visibility.Collapsed;
            ContentPage.Visibility = Visibility.Visible;
        }
    }

}
    