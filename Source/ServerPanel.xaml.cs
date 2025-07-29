using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DocSpy
{
    public sealed partial class ServerPanel : UserControl
    {
        public Action<string>? UpdateUINavigated { get; set; }
        public Action? UpdateUIRefresh { get; set; }
        public ServerPanel()
        {
            InitializeComponent();
            ActualThemeChanged += (s, e) =>
            {
                RecreatePanel();
                UpdateUIRefresh?.Invoke();
            };
        }

        public void ActivatePanel()
        {
            PaintStopButton();
        }
        public void DeactivatePanel()
        {
            DeactivateSiteButtons();
            PaintStopButton();
        }

        public void ActivateRoot(TRoot root)
        {
            foreach (var button in SitesPanel.Children)
            {
                if (button is Button b && b.CommandParameter is TRoot r)
                {
                    PaintSiteButton(b, r.Name == root.Name);
                }
            }
        }

        public void RecreatePanel()
        {
            RemoveSiteButtons();

            foreach (var root in Config.Instance.Roots)
            {
                var button = new Button
                {
                    Content = root.Name,
                    CommandParameter = root,
                    Margin = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };

                button.Click += OnItemClicked;
                PaintSiteButton(button, false);

                SitesPanel.Children.Add(button);
                ButtonGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto
                });
                Grid.SetRow(button, ButtonGrid.RowDefinitions.Count - 1);
            }
        }


        private void RemoveSiteButtons()
        {
            SitesPanel.Children.Clear();
        }

        private void DeactivateSiteButtons()
        {
            foreach (var button in SitesPanel.Children)
            {
                PaintSiteButton(button as Button, false);
            }
        }


        private async void OnItemClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button)
            {
                return;
            }
            if (Server.Instance.Stopped) await Server.Instance.Serve();
            TRoot commandParameter = ((TRoot)((Button)sender!).CommandParameter);
            if (commandParameter != null)
            {
                UpdateUINavigated?.Invoke(commandParameter.Name);
            }
            else
            {
                UpdateUINavigated?.Invoke("not_found");
            }
        }

        public void ActivateSiteButton(object sender)
        {
            DeactivateSiteButtons();
            if (sender is Button button)
            {
                PaintSiteButton(button, true);
            }
        }

        private void PaintStopButton()
        {
            StopButton.Background = !Server.Instance.Stopped ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Transparent);
            StopButton.Foreground = !Server.Instance.Stopped ? new SolidColorBrush(Colors.White) : GetThemeDefaultFontColor();
            StopButton.BorderBrush = new SolidColorBrush(Colors.Gray);
            StopButton.BorderThickness = !Server.Instance.Stopped ? new Thickness(1) : new Thickness(0.5);
            return;

        }
        private void PaintSiteButton(Button? button, bool Active)
        {
            if (button == null)
            {
                return;
            }
            button.Background = Active ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Transparent);
            button.Foreground = Active ? new SolidColorBrush(Colors.White) : GetThemeDefaultFontColor();
            // In code, this will use the correct theme for the button
            button.BorderBrush = new SolidColorBrush(Colors.Gray);
            button.BorderThickness = Active ? new Thickness(1) : new Thickness(0.5);
        }

        private Brush GetThemeDefaultFontColor()
        {
            if (Resources.TryGetValue("TextFillColorPrimaryBrush", out var localBrush) && localBrush is Brush themeBrush)
            {
                return themeBrush;
            }
            else
            if (Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var brushObj) && brushObj is Brush brush)
            {
                return brush;
            }
            else
            {
                // Fallback if the resource is missing
                return new SolidColorBrush(Colors.LightGray);
            }
        }

        private async void OnStopClicked(object sender, RoutedEventArgs e)
        {
            await Server.Instance.StopServe();
        }


    }
}
