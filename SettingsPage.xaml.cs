using ElectronicCorrectionNotebook.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
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
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private const string settingsFileName = "settings.json";
        private Window mainWindow;

        public SettingsPage()
        {
            this.InitializeComponent();
            mainWindow = App.MainWindow;

            // ȷ��Ӧ��Ŀ¼����
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            LoadSettingsAsync();
        }

        // ��������
        private async void LoadSettingsAsync()
        {
            try
            {
                string settingsFilePath = Path.Combine(appDataPath, settingsFileName);
                if (File.Exists(settingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(settingsFilePath);
                    // ���� JSON ����������
                }
            }
            catch (FileNotFoundException)
            {
                // �ļ������ڣ������ǵ�һ������Ӧ�ó���
            }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageFolder internalFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

                await CopyFolderContentsAsync(internalFolder, folder);

                ContentDialog successDialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Success �����ɹ�!",
                    Content = "Success ���������ѳɹ�����!",
                    CloseButtonText = "Ok"
                };
                PublicEvents.PlaySystemSound();
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
