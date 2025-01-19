using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
using Windows.ApplicationModel.Core;
using ElectronicCorrectionNotebook.DataStructure;
using System.IO;
using System.Text.Json;
using System;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using CommunityToolkit.WinUI.Controls;

namespace ElectronicCorrectionNotebook
{
    public partial class App : Microsoft.UI.Xaml.Application
    {
        public static Window MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }


        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();

            // 设置窗口的启动大小
            var hwnd = WindowNative.GetWindowHandle(MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1800, 1250));
            MainWindow.Activate();

            // 在应用启动时加载设置
            LoadSettingsAsync();
        }

        // 加载设置
        private async void LoadSettingsAsync()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
            string settingsFilePath = Path.Combine(appDataPath, "settings.json");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            if (!File.Exists(settingsFilePath))
            {
                File.Create(settingsFilePath).Dispose();
            }

            try
            {
                string json = await File.ReadAllTextAsync(settingsFilePath);
                Settings settings = null;
                if (!string.IsNullOrWhiteSpace(json))
                {
                    settings = JsonSerializer.Deserialize<Settings>(json);
                }
                if (settings == null)
                {
                    settings = new Settings(); // 提供默认设置
                }

                // 加载实质设置内容
                if (MainWindow.Content is FrameworkElement rootElement)
                {
                    // rootElement.RequestedTheme = settings.IsThemeDark ? ElementTheme.Dark : ElementTheme.Light;

                    ElementSoundPlayer.State = settings.IsSoundEnabled ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;

                    var gitHubBaby = (MainWindow.Content as FrameworkElement)?.FindName("GitHubBaby") as UIElement;
                    if (gitHubBaby != null)
                    {
                        gitHubBaby.Visibility = settings.IsGifEnabled ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error loading settings", ex);
            }
        }

        // 显示错误
        private async Task ShowErrorMessageAsync(string title, Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                Title = title,
                Content = $"{ex.Message}\n\n{ex.StackTrace}",
                CloseButtonText = "Ok 确定",
                XamlRoot = MainWindow.Content.XamlRoot, // 确保使用当前页面的 XamlRoot
                // RequestedTheme = (ElementTheme)Microsoft.UI.Xaml.Application.Current.RequestedTheme // 设置主题与应用程序一致
            };
            await errorDialog.ShowAsync();
        }
    }
}
