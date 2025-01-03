﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectronicCorrectionNotebook.Services;
using System.Threading;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Microsoft.UI;           // Needed for WindowId.
using Microsoft.UI.Windowing; // Needed for AppWindow.
using WinRT.Interop;          // Needed for XAML/HWND interop.
using Microsoft.UI.Input;
using Rect = Windows.Foundation.Rect;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElectronicCorrectionNotebook
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<ErrorItem> errorItems;
        private CancellationTokenSource cts;


        public MainWindow()
        {
            InitializeComponent();

            Closed += MainWindow_Closed;
            CoreApplication.Exiting += OnExiting; 
            
            AppWindow.SetIcon("Assets/im.ico");
            cts = new CancellationTokenSource();
            errorItems = new List<ErrorItem>(); 
            _ = LoadDataAsync(cts.Token);


            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

        }


        // 加载数据-从json中读取数据
        private async Task LoadDataAsync(CancellationToken token)
        {
            try
            {
                errorItems = await DataService.LoadDataAsync(token);
                foreach (var item in errorItems)
                {
                    AddNavigationViewItem(item);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                // Log or display the error message
            }
        }

        // 存储数据-把数据存储到json
        private async Task SaveDataAsync(CancellationToken token)
        {
            try
            {
                await DataService.SaveDataAsync(errorItems, token);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                // Log or display the error message
            }
        }

        // 导航栏中添加新项
        private void AddNavigationViewItem(ErrorItem errorItem)
        {
            var newItem = new NavigationViewItem
            {
                Content = errorItem.Title,
                Icon = new SymbolIcon(Symbol.Comment),
                Tag = errorItem
            };
            nvSample.MenuItems.Add(newItem);
        }

        // 添加新错题-总步骤
        private async void Add_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var newErrorItem = new ErrorItem
            {
                Title = "New Error",
                Date = DateTime.Now,
                Description = "Description...",
                FilePaths = new List<string>(),
                Rating = -1,
            };
            errorItems.Add(newErrorItem);
            AddNavigationViewItem(newErrorItem);
            await SaveDataAsync(cts.Token);
        }

        // 关于
        private async void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PublicEvents.PlaySystemSound();
            StackPanel contentPanel = new StackPanel();

            TextBlock aboutInfo = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };

            aboutInfo.Inlines.Add(new Run { Text = "Created by ", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });

            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run { Text = "@QuincyZhao😀" });
            hyperlink.NavigateUri = new Uri("https://github.com/zhaoqianbiao");
            hyperlink.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

            aboutInfo.Inlines.Add(hyperlink);
            aboutInfo.Inlines.Add(new Run { Text = " in GCGS", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });

            Image aboutImage = new Image()
            {
                Source = new BitmapImage(new Uri("ms-appx:///Assets/peter.png")), 
                Width = 100,
                Height = 100,
                Margin = new Thickness(0, 10, 0, 0)
            };

            contentPanel.Children.Add(aboutInfo);
            contentPanel.Children.Add(aboutImage);

            ContentDialog about = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot, 
                Title = "About",
                Content = contentPanel,
                CloseButtonText = "Ok"
            };
            await about.ShowAsync();
        }

        // 当title更改时，更新导航视图项的名字
        public void UpdateNavigationViewItem(ErrorItem errorItem)
        {
            foreach (var menuItem in nvSample.MenuItems)
            {
                if (menuItem is NavigationViewItem item && item.Tag == errorItem)
                {
                    item.Content = errorItem.Title;
                    break;
                }
            }
        }

        // 导航视图选择更改时
        public async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // 自动保存
            if (contentFrame.Content is ErrorDetailPage currentPage)
            {
                await currentPage.SaveCurrentContentAsync();
            }

            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var selectedErrorItem = args.SelectedItemContainer.Tag as ErrorItem;
                if (selectedErrorItem != null)
                {
                    contentFrame.Navigate(typeof(ErrorDetailPage), selectedErrorItem);
                }
            }
        }

        // 窗口关闭时
        public async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            args.Handled = true;

            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Confirm to exit 确认退出",
                Content = "Are you sure to save and exit? 你确定要保存数据并退出吗？",
                PrimaryButtonText = "Yes 是",
                CloseButtonText = "No 否",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot // 确保 XamlRoot 设置正确
            };
            PublicEvents.PlaySystemSound();
            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await SaveCurrentStateAsync();
                this.Closed -= MainWindow_Closed;
                Application.Current.Exit();
            }
        }

        // 应用程序退出时
        private async void OnExiting(object sender, object e)
        {
            await SaveCurrentStateAsync();
        }

        // 保存当前状态
        private async Task SaveCurrentStateAsync()
        {
            if (contentFrame.Content is ErrorDetailPage currentPage)
            {
                await currentPage.SaveCurrentContentAsync();
            }
            await SaveDataAsync(cts.Token);
            cts.Cancel(); // 
        }

    }
}
