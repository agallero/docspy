using DocSpy.Source;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DocSpy
{
    public sealed partial class SettingsControl : UserControl
    {
        public Action? UpdateUIGoToBrowser { get; set; }    
        public SettingsControl()
        {
            InitializeComponent();
            DataContext = Settings.Instance.ViewModel;
        }

        private async void PickConfigButton_Click(object sender, RoutedEventArgs e)
        {
            //disable the button to avoid double-clicking
            var senderButton = (Button)sender;
            senderButton.IsEnabled = false;
            try
            {
                // Create a file picker
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                // Set options for your file picker
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.FileTypeFilter.Add(".ini");

                if (string.IsNullOrWhiteSpace(ConfigFileEdit.Text) == false)
                {
                    // openPicker. =  Path.GetDirectoryName(ConfigFileEdit.Text);
                }
                else
                {
                    openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                }

                // Open the picker for the user to pick a file
                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    ConfigFileEdit.Text = file.Path;
                }
            }
            finally
            {
                //re-enable the button
                senderButton.IsEnabled = true;
            }

        }

        private async void CreateNewConfigButton_Click(object sender, RoutedEventArgs e)
        {
            //disable the button to avoid double-clicking
            var senderButton = (Button)sender;
            senderButton.IsEnabled = false;
            try
            {

                FileSavePicker savePicker = new();
                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                // Set options for your file picker
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("ini file", [".ini"]);
                savePicker.SuggestedFileName = "docspy.config.ini";

                // Open the picker for the user to pick a file
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    try
                    {
                        Settings.Instance.ViewModel.ConfigFilePath = file.Path;
                        var templateFile = await StorageFile.GetFileFromApplicationUriAsync(
                        new Uri("ms-appx:///Assets/docspy.config.ini"));
                        // Open streams and copy content
                        using (var sourceStream = await templateFile.OpenReadAsync())
                        using (var targetStream = await file.OpenStreamForWriteAsync())
                        {
                            await sourceStream.AsStreamForRead().CopyToAsync(targetStream);
                        }


                        Process.Start(new ProcessStartInfo(file.Path) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        // Show an error message if the file couldn't be saved
                        ContentDialog customContentDialog = new()
                        {
                            Title = "Error",
                            Content = "File " + file.Name + " couldn't be saved: " + ex.Message,
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await customContentDialog.ShowAsync();
                    }
                }
            }
            finally
            {
                //re-enable the button
                senderButton.IsEnabled = true;
            }

        }

        private async void EditConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Settings.Instance.ViewModel.ConfigFilePath))
            {
                ContentDialog customContentDialog = new()
                {
                    Title = "Error",
                    Content = $"Config file {Settings.Instance.ViewModel.ConfigFilePath} does not exist.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await customContentDialog.ShowAsync();
                return;
            }
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Settings.Instance.ViewModel.ConfigFilePath,
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch (Exception ex)
            {
                ContentDialog customContentDialog = new()
                {
                    Title = "Error",
                    Content = $"Failed to open config file {Settings.Instance.ViewModel.ConfigFilePath}: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await customContentDialog.ShowAsync();
            }
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Config.Instance.LoadConfigs())
            {
                ContentDialog errorDialog = new()
                {
                    Title = "Error",
                    Content = "Failed to load configurations. Please check the configuration file.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }
            UpdateUIGoToBrowser?.Invoke();
        }

        private void ConfigFileEdit_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            FillList(sender, args.Reason);
        }

        private static void FillList(AutoSuggestBox sender, AutoSuggestionBoxTextChangeReason Reason)
        {
            try
            {
                // Since selecting an item will also change the text,
                // only listen to changes caused by user entering text.
                if (Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    if (string.IsNullOrEmpty(sender.Text))
                    {
                        sender.ItemsSource = null;
                        return;
                    }
                    var fullPath = Path.GetFullPath(sender.Text.Trim());
                    string? senderPath = Path.GetDirectoryName(fullPath);

                    var suitableItems = new List<string>();
                    if (Directory.Exists(fullPath))
                    {
                        suitableItems.AddRange(from x in Directory.GetFiles(fullPath, "*.ini", SearchOption.TopDirectoryOnly) select x);
                        suitableItems.AddRange(from x in Directory.GetDirectories(fullPath, "*", SearchOption.TopDirectoryOnly) select x + "\\");
                    }

                    if (senderPath != null && Directory.Exists(senderPath))
                    {
                        var senderFileName = Path.GetFileName(fullPath);
                        if (!String.IsNullOrEmpty(senderFileName))
                        {
                            suitableItems.AddRange(from x in Directory.GetFiles(senderPath, "*.ini", SearchOption.TopDirectoryOnly) where Path.GetFileName(x).StartsWith(senderFileName, StringComparison.OrdinalIgnoreCase) select x);
                            suitableItems.AddRange(from x in Directory.GetDirectories(senderPath, "*", SearchOption.TopDirectoryOnly) where Path.GetFileName(x).StartsWith(senderFileName, StringComparison.OrdinalIgnoreCase) select x + "\\");
                        }
                    }

                    sender.ItemsSource = suitableItems.AsEnumerable();
                }
            }
            catch (Exception)
            {
                sender.ItemsSource = null;
            }
        }

        private void ConfigFileEdit_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {            
            sender.Text = args.SelectedItem.ToString() ?? string.Empty;
        }

        private void ConfigFileEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            AutoSuggestBox Box = (AutoSuggestBox)sender;
            FillList(Box, AutoSuggestionBoxTextChangeReason.UserInput);
            Box.IsSuggestionListOpen = true;
        }

        private async void ConfigFileEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConfigFileEdit.Text))
                {
                    return;
                }
                ConfigFileEdit.Text = Path.GetFullPath(ConfigFileEdit.Text.Trim());
            }
            catch (Exception ex)
            {
                ContentDialog customContentDialog = new()
                {
                    Title = "Error",
                    Content = "Invalid file path. " + ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await customContentDialog.ShowAsync();
            }
        }
    }

}