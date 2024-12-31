using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElectronicCorrectionNotebook
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private Window mainWindow;

        public SettingsPage()
        {
            this.InitializeComponent();
            mainWindow = App.MainWindow;
            LoadSettings();
        }

        // 加载设置
        private void LoadSettings()
        {
            
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建 FolderPicker 让用户选择导出位置
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // 获取当前窗口的 HWND
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // 获取内部存储位置的错题数据文件夹
                StorageFolder internalFolder = ApplicationData.Current.LocalFolder;

                // 复制文件到用户选择的文件夹
                await CopyFolderContentsAsync(internalFolder, folder);

                // 显示导出成功的消息
                ContentDialog successDialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Success 导出成功!",
                    Content = "Success 错题数据已成功导出!",
                    CloseButtonText = "Ok"
                };
                await successDialog.ShowAsync();
            }
        }

        private async Task CopyFolderContentsAsync(StorageFolder sourceFolder, StorageFolder destinationFolder)
        {
            foreach (StorageFile file in await sourceFolder.GetFilesAsync())
            {
                await file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }

            foreach (StorageFolder folder in await sourceFolder.GetFoldersAsync())
            {
                StorageFolder newFolder = await destinationFolder.CreateFolderAsync(folder.Name, CreationCollisionOption.OpenIfExists);
                await CopyFolderContentsAsync(folder, newFolder);
            }
        }
    }
}
