using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DocSpy
{
    public sealed partial class BrowserControl : UserControl
    {
        public Action<TRoot>? UpdateUINavigated { get; set; }
        public Action? UpdateUIGoToSettings {get;set; }
        public LogWindow? Logger { get; internal set; }

        private bool UpdatingUrl { get; set; }
        const int MaxHistory = 64;

        public BrowserControl()
        {
            InitializeComponent();
            Loaded += BrowserControl_Loaded;

        }

        private async void BrowserControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.MainWindow.AppWindow.Changed += (s, args) =>
            {
                if (args.DidSizeChange)
                {
                    double scaleAdjustment = App.MainWindow.Content.XamlRoot.RasterizationScale;
                    if (scaleAdjustment != 0)
                    {
                        Settings.Instance.ViewModel.WindowSize = new Windows.Foundation.Size(App.MainWindow.AppWindow.Size.Width / scaleAdjustment, App.MainWindow.AppWindow.Size.Height / scaleAdjustment);
                    }
                }

                if (args.DidPositionChange)
                {
                    double scaleAdjustment = App.MainWindow.Content.XamlRoot.RasterizationScale;
                    if (scaleAdjustment != 0)
                    {
                        Settings.Instance.ViewModel.WindowPosition = new Windows.Foundation.Point(App.MainWindow.AppWindow.Position.X / scaleAdjustment, App.MainWindow.AppWindow.Position.Y / scaleAdjustment);
                    }
                }
            };
            RestoreWindowSizeAndPosition();
            await WebView.EnsureCoreWebView2Async();
            EnableDisableJavascript();

            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            }
            else
            {
                WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
            }            
        }

        public void ReloadWebView()
        {
            WebView.Reload();
        }

        private void OnFwdClicked(object sender, RoutedEventArgs e)
        {
            WebView.GoForward();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WebView.GoBack();
        }

        private async void OnReloadClicked(object sender, RoutedEventArgs e)
        {
            await ReloadAndHardRefresh();
        }

        private async Task ReloadAndHardRefresh()
        {
            await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync();
            await Server.Instance.Serve(); //forced. 
            WebView.Reload();
        }

        public void UpdateWebView(string path)
        {
            WebView.Source = new Uri($"{Server.RootUrl}/{path}/index.html");
        }

        private void WebView_Navigated(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Settings.Instance.ViewModel.BrowserUrl = WebView.Source.ToString();
            if(e.IsSuccess) UpdateBrowserHistory();
            UriHelper.MatchUrl(WebView.Source.ToString(), (root) =>
            {
                UpdateUINavigated?.Invoke(root);
            });
        }

        private void UpdateBrowserHistory()
        {
            try
            {
                var History = Settings.Instance.ViewModel.BrowserHistory;
                if (History == null)
                {
                    History = [];
                }
                var currentObj = History["current"];
                var current = currentObj == null ? 0 : (int)(currentObj);
                if (History[current.ToString(CultureInfo.InvariantCulture)].ToString() == WebView.Source.ToString())
                {
                    return; // No need to update if the current URL is already in history.
                }
                var next = (current + 1) % MaxHistory;
                History[next.ToString(CultureInfo.InvariantCulture)] = WebView.Source.ToString();
                History["current"] = next;
                Settings.Instance.ViewModel.BrowserHistory = History;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating browser history: {ex.Message}");
            }
        }

        private async void WebView_Navigating(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (Server.Instance.Stopped && e.Uri.ToString() != "about:blank") await Server.Instance.Serve();
            UpdatingUrl = true;
            try
            {
                UrlEntry.Text = e.Uri;
            }
            finally
            {
                UpdatingUrl = false;
            }
        }

        private async void RebuildButton_Clicked(object sender, RoutedEventArgs e)
        {
            Building? Progress = null;
            try
            {
                var Root = null as TRoot;
                UriHelper.MatchUrl(WebView.Source.ToString(), (root) => { Root = root; });
                if (Root == null)
                {
                    ContentDialog dialog = new()
                    {
                        Title = "Rebuild",
                        Content = $"Can't find the site at {WebView.Source}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                    return;
                }

                await Server.Instance.StopServe();
                Progress = new Building(Root);
                var presenter = OverlappedPresenter.CreateForDialog();
                presenter.IsResizable = true;
                presenter.IsModal = true;

                Progress.AppWindow.SetPresenter(presenter);
                Progress.AppWindow.Show();
                await Progress.Run();
                await ReloadAndHardRefresh();
            }
            catch (Exception ex)
            {
                Progress?.Close();

                ContentDialog errorDialog = new()
                {
                    Title = "Error",
                    Content = $"An error occurred while rebuilding: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void EditButton_Clicked(object sender, RoutedEventArgs e)
        {
            var Root = null as TRoot;
            UriHelper.MatchUrl(WebView.Source.ToString(), (root) => { Root = root; });
            if (Root == null)
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Edit",
                    Content = $"Can't find the site at {WebView.Source}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            if (String.IsNullOrEmpty(Root.Editor))
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Edit",
                    Content = "No editor specified in the configuration file.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot

                };
                await errorDialog.ShowAsync();
                return;
            }

            var arguments = Root.EditorArguments ?? "";
            if (!String.IsNullOrEmpty(arguments))
            {
                arguments = arguments.Replace("{{_rel-path_}}", UriHelper.GetRelativeSourcePathFromUrl(WebView.Source.ToString(), Root));
            }
            Process.Start(Root.Editor, arguments);
        }

        private async void OpenInExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            var Root = null as TRoot;
            UriHelper.MatchUrl(WebView.Source.ToString(), (root) => { Root = root; });
            if (Root == null)
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Open site in explorer",
                    Content = $"Can't find the site at {WebView.Source}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            Process.Start(new ProcessStartInfo()
            {
                FileName = Root.WebFolder,
                UseShellExecute = true,
            });

        }

        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = WebView.Source.ToString(),
                UseShellExecute = true,
            });

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateUIGoToSettings?.Invoke();
        }

        private void DarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("--light");
            }
            else
            {
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("--dark");
            }
            //ThemeHelper.SetTheme(App.MainWindow);
        }

        public static string DarkModeIcon => Application.Current.RequestedTheme == ApplicationTheme.Dark ? "\uE708" : "\uE706";


        private static void RestoreWindowSizeAndPosition()
        {
            double scaleAdjustment = App.MainWindow.Content.XamlRoot.RasterizationScale;
            if (Settings.Instance.ViewModel.WindowSize.Width > 0 && Settings.Instance.ViewModel.WindowSize.Height > 0)
            {
                App.MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(
                    (Int32)(Settings.Instance.ViewModel.WindowSize.Width * scaleAdjustment),
                    (Int32)(Settings.Instance.ViewModel.WindowSize.Height * scaleAdjustment)));
            }
            if (Settings.Instance.ViewModel.WindowPosition.X > 0 && Settings.Instance.ViewModel.WindowPosition.Y > 0)
            {
                App.MainWindow.AppWindow.Move(new Windows.Graphics.PointInt32(
                    (Int32)(Settings.Instance.ViewModel.WindowPosition.X * scaleAdjustment),
                    (Int32)(Settings.Instance.ViewModel.WindowPosition.Y * scaleAdjustment)));
            }

        }

        private static void SetWindowClientSize(double width, double height)
        {
            double scaleAdjustment = App.MainWindow.Content.XamlRoot.RasterizationScale;
            int buttonPanelWidth = (Int32)(Settings.Instance.ViewModel.ButtonPanelWidth.Value * scaleAdjustment);
            App.MainWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((Int32)(width * scaleAdjustment) + buttonPanelWidth, (Int32)(height * scaleAdjustment)));
        }

        private void PhoneButton_Click(object sender, RoutedEventArgs e)
        {
            // iPhone 16: https://www.ios-resolution.com
            SetWindowClientSize(393, 852);
        }

        private void TabletButton_Click(object sender, RoutedEventArgs e)
        {
            // iPad Pro 13: https://www.ios-resolution.com
            SetWindowClientSize(1032, 1376);
        }

        private void DesktopButton_Click(object sender, RoutedEventArgs e)
        {
            ((OverlappedPresenter)App.MainWindow.AppWindow.Presenter).Maximize();
        }

        private void JsButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.ViewModel.IsJsEnabled = !Settings.Instance.ViewModel.IsJsEnabled;
            EnableDisableJavascript();
        }

        private void EnableDisableJavascript()
        {
            WebView.CoreWebView2.Settings.IsScriptEnabled = Settings.Instance.ViewModel.IsJsEnabled;
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            Logger?.AppWindow.Hide();
            Logger?.AppWindow.Show();
        }

        private static void FillList(AutoSuggestBox sender, IEnumerable<string> items)
        {

            sender.ItemsSource = items;
            return;
        }

        private void UrlEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            AutoSuggestBox Box = (AutoSuggestBox)sender;
            FillList(Box, GetUrlHistory());
            Box.IsSuggestionListOpen = true;
        }

        private IEnumerable<string> GetUrlHistory()
        {
            var History = Settings.Instance.ViewModel.BrowserHistory;
            if (History == null || History.Count == 0)
            {
                yield break;
            }
            var currentObj = History["current"];
            var current = currentObj == null ? 0 : (int)(currentObj);
            var entries = new int[History.Count - 1];
            int k = 0;
            for (int i = current - 1; i >= 0; i--)
            {
                entries[k] = i;
                k++;
            }
            for (int i = History.Count - 1; i > current; i--)
            {
                entries[k] = i;
                k++;
            }
            foreach (var i in entries)
            {
                if (History.TryGetValue(i.ToString(CultureInfo.InvariantCulture), out var url))
                {
                    string? urlStr = url.ToString();
                    if (urlStr != null)
                    {
                        yield return urlStr;
                    }
                }
            }
        }

        private void UrlEntry_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void UrlEntry_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (UpdatingUrl)
            {
                return; // Don't reload while updating the URL.
            }

            try
            {
                WebView.Source = new Uri(UrlEntry.Text);
            }
            catch (UriFormatException)
            {
            }
        }
    }


    public partial class JSBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                if (boolValue)
                {
                    return new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // Not usually needed for this scenario
        }
    }
}
