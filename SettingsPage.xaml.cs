using ElectronicCorrectionNotebook.DataStructure;
using ElectronicCorrectionNotebook.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ElectronicCorrectionNotebook
{
    public sealed partial class SettingsPage : Page
    {
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private const string settingsFileName = "settings.json";
        private Window mainWindow;
        private Application application;

        public SettingsPage()
        {
            this.InitializeComponent();
            mainWindow = App.MainWindow;
            application = App.Current;

            // ȷ��Ӧ��Ŀ¼����
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // ���Դ��룬��� XamlRoot �Ƿ���ȷ
            if (this.XamlRoot == null)
            {
                throw new InvalidOperationException("XamlRoot is null. Ensure the page is properly initialized.");
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
                    var settings = JsonSerializer.Deserialize<Settings>(json);

                    // �������õ�UI
                    // themeMode.SelectedItem = settings.Theme;

                }
            }
            catch (FileNotFoundException)
            {
                // �ļ������ڣ������ǵ�һ������Ӧ�ó���
            }
        }

        // ��������
        /*
        private async void SaveSettingsAsync()
        {
            try
            {
                var settings = new Settings
                {
                    Theme = themeMode.SelectedItem.ToString()
                };

                string json = JsonSerializer.Serialize(settings);
                string settingsFilePath = Path.Combine(appDataPath, settingsFileName);
                await File.WriteAllTextAsync(settingsFilePath, json);

                // �л�Ӧ�ó�������
                if (settings.Theme == "Dark")
                {
                    ((App)Application.Current).SetAppTheme(ApplicationTheme.Dark);
                }
                else
                {
                    ((App)Application.Current).SetAppTheme(ApplicationTheme.Light);
                }
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving settings", ex);
            }
        }
        */

        // ��������
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

        // �����ļ���
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

        // ��ʾ������Ϣ
        private async Task ShowErrorMessageAsync(string title, Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                Title = title,
                Content = $"{ex.Message}\n\n{ex.StackTrace}",
                CloseButtonText = "Ok ȷ��",
                XamlRoot = this.XamlRoot, // ȷ��ʹ�õ�ǰҳ��� XamlRoot
            };
            await errorDialog.ShowAsync();
        }

        // �����л�
        private void ThemeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch themeToggleSwitch = sender as ToggleSwitch;
            if (themeToggleSwitch.IsOn)
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = ElementTheme.Dark;
                }
            }
            else
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = ElementTheme.Light;
                }
            }
        }

        // ��Ч����
        private void SoundToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch soundToggleSwitch = sender as ToggleSwitch;
            if (soundToggleSwitch.IsOn)
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
            }
            else
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
            }
        }
    }
}
