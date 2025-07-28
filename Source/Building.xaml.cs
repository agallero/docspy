using DocSpy.Source;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.BadgeNotifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;


namespace DocSpy
{
    public sealed partial class Building : Window
    {
        public TRoot Root { get; }
        public bool Working { get; set; } = false;
        readonly Stopwatch Watch = new();


        public Building(TRoot root)
        {
            InitializeComponent();

            Closed += Building_Closed;
            AppWindow.Closing += AppWindow_Closing;
            Root = root;
            
            this.Title = "D👁️CSpy Build";
            CaptionLabel.Text = $"Building {Root.Name}...";

            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 800));
            AppWindow.SetIcon("Assets/DocSpy.ico");
            SetWindowOwner(owner: App.MainWindow);
            CenterWindow();
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (Working) args.Cancel = true; // Prevent the window from closing
        }

        #region Really??
        // Sets the owner window of the modal window.
        private void SetWindowOwner(Window owner)
        {
            // Get the HWND (window handle) of the owner window (main window).
            IntPtr ownerHwnd = WindowNative.GetWindowHandle(owner);

            // Get the HWND of the AppWindow (modal window).
            IntPtr ownedHwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);

            // Set the owner window using SetWindowLongPtr for 64-bit systems
            // or SetWindowLong for 32-bit systems.
            if (IntPtr.Size == 8) // Check if the system is 64-bit
            {
                SetWindowLongPtr(ownedHwnd, -8, ownerHwnd); // -8 = GWLP_HWNDPARENT
            }
            else // 32-bit system
            {
                SetWindowLong(ownedHwnd, -8, ownerHwnd); // -8 = GWL_HWNDPARENT
            }
        }

        // Import the Windows API function SetWindowLongPtr for modifying window properties on 64-bit systems.
        [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Import the Windows API function SetWindowLong for modifying window properties on 32-bit systems.
        [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Centers the given AppWindow on the screen based on the available display area.
        private void CenterWindow()
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
            if (area == null) return;
            AppWindow.Move(new PointInt32((area.Value.Width - AppWindow.Size.Width) / 2, (area.Value.Height - AppWindow.Size.Height) / 2));
        }

        #endregion

        private void Building_Closed(object sender, WindowEventArgs args)
        {
            if (!Working) BadgeNotificationManager.Current.ClearBadge();
            App.MainWindow.Activate();
        }

        public string CalcElapsedTime
        {
            get
            {
                if (!Watch.IsRunning)
                {
                    return "0 min, 0 sec";
                }
                var elapsed = Watch.Elapsed;
                if (elapsed.TotalMinutes < 1)
                {
                    return $"{elapsed.Seconds} sec";
                }
                return $"{elapsed.Minutes} min, {elapsed.Seconds} sec";
            }
        }

        public async Task Run()
        {
            var CloseButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10, 10, 10, 10)
            };
            Grid.SetRow(CloseButton, 3);

            if (string.IsNullOrEmpty(Root.BuildCommand))
            {
                CaptionLabel.Text = "No build command specified.";
                LogBox.Text += $"The configuration file doesn't specify a command to build {Root.Name}";
            }
            else
            {
                var ExitCode = 0;
                BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.Activity);
                try
                {
                    ProgressIndicator.ShowPaused = false;
                    var Timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    Working = true;
                    try
                    {
                        Timer.Tick += (s, e) =>
                        {
                            ElapsedTime.Text = CalcElapsedTime;
                        };
                        Timer.Start();
                        Watch.Start();
                        ExitCode = await RunProcessAsync(Root.BuildCommand, Root.BuildCommandArguments ?? "", DispatcherQueue.GetForCurrentThread(), LogBox);
                        Watch.Stop();
                        Timer.Stop();
                    }
                    finally
                    {
                        Working = false;
                    }
                    ProgressIndicator.ShowPaused = true;
                }
                finally
                {
                    BadgeNotificationManager.Current.ClearBadge();
                }

                if (ExitCode == 0)
                {
                    this.Close();
                    AppNotification notification = new AppNotificationBuilder()
                        .AddText($"Documentation for {Root.Name} built successfully!")
                        .AddText($"Time: {Watch.Elapsed.ToString("%m' min, '%s' sec'")}")
                        .BuildNotification();

                    AppNotificationManager.Default.Show(notification);
                    return;
                }
            }
            
            CloseButton.Click += (s, e) =>
            {
                this.Close();
            };

            ContentGrid.Children.Add(CloseButton);
            if (AppWindow != null && AppWindow.IsVisible) BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.Error);
            else BadgeNotificationManager.Current.ClearBadge(); //If the window was already closed, there is no way to clear the badge later.

            CaptionLabel.Text = $"ERROR Building {Root.Name}...";
            CaptionLabel.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red);
            AppNotification errNotification = new AppNotificationBuilder()
                 .AddText($"😿 Error building documentation for {Root.Name}.")
                 .BuildNotification();

            AppNotificationManager.Default.Show(errNotification);

        }

        private static async Task<int> RunProcessAsync(string fileName, string arguments, DispatcherQueue dispatcherQueue, TextBlock LogBox)
        {
            using var process = new Process
            {
                StartInfo = {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetTempPath(),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8,
                    },
            };

            process.OutputDataReceived += (sender, args) =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        LogBox.Text += args.Data + Environment.NewLine;
                    }
                });
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        LogBox.Text += "Error: " + args.Data + Environment.NewLine;
                    }
                });
            };

            LogBox.Text = Quote(process.StartInfo.FileName) + " " + process.StartInfo.Arguments + Environment.NewLine + Environment.NewLine;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        private static string Quote(string fileName)
        {
            if (fileName.Contains(' ') && !fileName.StartsWith('"'))
            {
                return "\"" + fileName + "\"";
            }
            return fileName;
        }


        private void ScrollView_ExtentChanged(ScrollView sender, object args)
        {
            sender.ScrollTo(0, sender.ExtentHeight, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
        }
    } 

}
