using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace DocSpy
{
    public sealed partial class LogWindow : Window
    {
        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public LogWindow()
        {
            InitializeComponent();
            var presenter = OverlappedPresenter.Create();
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsAlwaysOnTop = false;
            AppWindow.Closing += (s, e) =>
            {
                // Prevent the window from closing
                e.Cancel = true;
                AppWindow.Hide();
            };

            AppWindow.SetPresenter(presenter);
            this.Title = "D👁️CSpy Log";
            AppWindow.SetIcon("Assets/DocSpy.ico");
            
        }

        internal void Log(string message)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                if (LogBox.Text.Length > 10000)
                {
                    LogBox.Text = LogBox.Text.Substring(8000); // Clear the log if it exceeds a certain length
                }
                LogBox.Text += message + Environment.NewLine;
            });
        }

        private void ScrollView_ExtentChanged(ScrollView sender, object args)
        {
            sender.ScrollTo(0, sender.ExtentHeight, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
        }

    }


}
