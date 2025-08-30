using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;

namespace Packer.Dialogs
{
    public sealed partial class AddNewItemDialog : ContentDialog
    {
        public string RealPath => FilePathBox.Text;
        public string VirtualPath => VirtualPathBox.Text;

        public AddNewItemDialog()
        {
            this.InitializeComponent();
        }

        async private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");

            IntPtr hWnd = WindowNative.GetWindowHandle(App.Current.As<App>().MainWindow);
            InitializeWithWindow.Initialize(picker, hWnd);

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null)
                return;

            FilePathBox.Text = file.Path;
        }
    }
}
