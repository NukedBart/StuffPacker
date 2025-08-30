using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Packer.Features;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;

namespace Packer
{
    public sealed partial class MainWindow : Window
    {
        private StorageFolder gFolder;
        private ObservableCollection<KeyValuePair<string, string>> gFiles;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.Resize(new SizeInt32(450, 600));
            gFiles = new();
        }

        async private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hWnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder == null) 
                return;

            gFolder = folder;
            OutputPathBox.Text = folder.Path;
            ListSelectionChangeCallback();
        }

        private void ListUpdateCallback() 
        {
            FilesListBox.ItemsSource = gFiles;
            ListSelectionChangeCallback();
        }

        private void ListSelectionChangeCallback() 
        {
            PackageButton.IsEnabled = gFolder != null && gFiles.Count > 0;
            RemoveButton.IsEnabled = gFiles.Count > 0;
            RemoveButton.IsEnabled = FilesListBox.SelectedItem != null;
        }

        private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ListSelectionChangeCallback();

        async private void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AddNewItemDialog
            {
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary) 
                return;
            if (string.IsNullOrWhiteSpace(dialog.RealPath))
                return;
            if (string.IsNullOrWhiteSpace(dialog.VirtualPath))
                return;
            if (gFiles.Any(f => f.Key == dialog.VirtualPath))
            {
                var warn = new ContentDialog
                {
                    Title = "Warning",
                    Content = "This EmbedFS Path has already been assigned.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await warn.ShowAsync();
                return;
            }

            var entry = new KeyValuePair<string, string>(dialog.VirtualPath, dialog.RealPath);

            gFiles.Add(entry);
            ListUpdateCallback();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListBox.SelectedItem is not KeyValuePair<string, string> item) return;

            gFiles.Remove(FilesListBox.SelectedItem.As<KeyValuePair<string, string>>());
            ListUpdateCallback();
        }

        async private void PackageButton_Click(object sender, RoutedEventArgs e)
        {
            if (gFolder == null)
            {
                var err = new ContentDialog
                {
                    Title = "Error",
                    Content = "Select a valid output path first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await err.ShowAsync();
                return;
            }
            try
            {
                StuffPacker.Pack(gFolder.Path, gFiles);
            }
            catch (Exception ex)
            {
                var err = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await err.ShowAsync();
                return;
            }
        }

        private void AddRandomEntriesButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                gFiles.Add(new KeyValuePair<string, string>($"/res/misc/{Guid.NewGuid().ToString()}", @$"X:\resources\generic\misc\{Guid.NewGuid().ToString()}"));
            }
            ListUpdateCallback();
        }
    }
}
