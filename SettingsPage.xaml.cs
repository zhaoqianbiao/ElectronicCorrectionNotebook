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

            // 确保应用目录存在
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

        // 加载设置到UI
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

                // 设置UI控件状态
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
                // 文件不存在，可能是第一次运行应用程序
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Error loading settings", ex);
            }
        }

        // 保存所有设置项到json文件
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
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error saving settings", ex);
            }
        }
        

        // 导出数据
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
                    Title = "Success 导出成功!",
                    Content = "Success 错题数据已成功导出!",
                    CloseButtonText = "Ok",
                    // RequestedTheme = (ElementTheme)Application.Current.RequestedTheme // 设置主题与应用程序一致
                };
                await successDialog.ShowAsync();
                PublicEvents.PlaySystemSound();
            }
        }

        // 复制文件夹
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

        // 显示错误消息
        private async Task ShowErrorMessageAsync(string title, Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                Title = title,
                Content = $"{ex.Message}\n\n{ex.StackTrace}",
                CloseButtonText = "Ok 确定",
                XamlRoot = this.XamlRoot, // 确保使用当前页面的 XamlRoot
                // RequestedTheme = (ElementTheme)Application.Current.RequestedTheme // 设置主题与应用程序一致
            };
            await errorDialog.ShowAsync();
        }

        // 主题切换
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

        // 音效开关
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

        // Gif开关
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
