using ElectronicCorrectionNotebook.DataStructure;
using ElectronicCorrectionNotebook.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private static readonly string SettingsFilePath = Path.Combine(appDataPath, "settings.json");
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
            if (!File.Exists(SettingsFilePath))
            {
                File.Create(SettingsFilePath);
            }

            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettingsAsync();
        }

        // �������õ�UI
        private async void LoadSettingsAsync()
        {
            try
            {
                string json = await File.ReadAllTextAsync(SettingsFilePath);
                Settings settings = null;
                if (!string.IsNullOrWhiteSpace(json))
                {
                    settings = JsonSerializer.Deserialize<Settings>(json);
                }
                if (settings == null)
                {
                    settings = new Settings();
                }

                // ����UI�ؼ�״̬
                // var themeToggleSwitch = rootPanel.FindName("themeToggleSwitch") as ToggleSwitch;
                var soundToggleSwitch = rootPanel.FindName("soundToggleSwitch") as ToggleSwitch;
                var gifToggleSwitch = rootPanel.FindName("gifToggleSwitch") as ToggleSwitch;

                /*if (themeToggleSwitch != null)
                {
                    themeToggleSwitch.IsOn = settings.IsThemeDark;
                }*/
                if (soundToggleSwitch != null)
                {
                    soundToggleSwitch.IsOn = settings.IsSoundEnabled;
                }
                if (gifToggleSwitch != null)
                {
                    gifToggleSwitch.IsOn = settings.IsGifEnabled;
                }
            }
            catch (FileNotFoundException)
            {
                // �ļ������ڣ������ǵ�һ������Ӧ�ó���
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Error loading settings", ex);
            }
        }

        // �������������json�ļ�
        private async void SaveSettingsAsync()
        {
            try
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    // var themeToggleSwitch = rootElement.FindName("themeToggleSwitch") as UIElement;
                    var soundToggleSwitch = rootElement.FindName("soundToggleSwitch") as UIElement;
                    var gifToggleSwitch = rootElement.FindName("gitToggleSwitch") as UIElement;
                }

                var settings = new Settings
                {
                    // IsThemeDark = themeToggleSwitch.IsOn ? true : false,
                    IsSoundEnabled = soundToggleSwitch.IsOn? true : false,
                    IsGifEnabled = gifToggleSwitch.IsOn ? true : false
                };

                string json = JsonSerializer.Serialize(settings);
                await File.WriteAllTextAsync(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving settings", ex);
            }
        }
        

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
                    CloseButtonText = "Ok",
                    // RequestedTheme = (ElementTheme)Application.Current.RequestedTheme // ����������Ӧ�ó���һ��
                };
                await successDialog.ShowAsync();
                PublicEvents.PlaySystemSound();
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
                // RequestedTheme = (ElementTheme)Application.Current.RequestedTheme // ����������Ӧ�ó���һ��
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
            SaveSettingsAsync();
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

            SaveSettingsAsync();
        }

        // Gif����
        private void GifToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch gifToggleSwitch = sender as ToggleSwitch;
            if (gifToggleSwitch.IsOn)
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    var gitHubBaby = rootElement.FindName("GitHubBaby") as UIElement;
                    if (gitHubBaby != null)
                    {
                        gitHubBaby.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    var gitHubBaby = rootElement.FindName("GitHubBaby") as UIElement;
                    if (gitHubBaby != null)
                    {
                        gitHubBaby.Visibility = Visibility.Collapsed;
                    }
                }
            }
            SaveSettingsAsync();
        }
    }
}
