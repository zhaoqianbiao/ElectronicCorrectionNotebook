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

        // ��������
        private void LoadSettings()
        {
            
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            // ���� FolderPicker ���û�ѡ�񵼳�λ��
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // ��ȡ��ǰ���ڵ� HWND
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // ��ȡ�ڲ��洢λ�õĴ��������ļ���
                StorageFolder internalFolder = ApplicationData.Current.LocalFolder;

                // �����ļ����û�ѡ����ļ���
                await CopyFolderContentsAsync(internalFolder, folder);

                // ��ʾ�����ɹ�����Ϣ
                ContentDialog successDialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Success �����ɹ�!",
                    Content = "Success ���������ѳɹ�����!",
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
