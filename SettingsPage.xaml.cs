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

            // 确保应用目录存在
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            this.Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 调试代码，检查 XamlRoot 是否正确
            if (this.XamlRoot == null)
            {
                throw new InvalidOperationException("XamlRoot is null. Ensure the page is properly initialized.");
            }

            LoadSettingsAsync();
        }

        // 加载设置
        private async void LoadSettingsAsync()
        {
            try
            {
                string settingsFilePath = Path.Combine(appDataPath, settingsFileName);
                if (File.Exists(settingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);

                    // 加载设置到UI
                    // themeMode.SelectedItem = settings.Theme;

                }
            }
            catch (FileNotFoundException)
            {
                // 文件不存在，可能是第一次运行应用程序
            }
        }

        // 保存设置
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

                // 切换应用程序主题
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
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error saving settings", ex);
            }
        }
        */

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
                    CloseButtonText = "Ok"
                };
                PublicEvents.PlaySystemSound();
                await successDialog.ShowAsync();
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
        }
    }
}
