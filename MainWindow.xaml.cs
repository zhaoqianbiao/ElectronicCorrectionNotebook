using Microsoft.UI.Xaml;
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
            this.InitializeComponent();
            this.Closed += MainWindow_Closed; // 添加关闭事件处理程序
            this.Title = "ElectricCorrectionNotebook";
            this.AppWindow.SetIcon("Assets/im.ico");
            cts = new CancellationTokenSource();
            errorItems = new List<ErrorItem>(); // 初始化 errorItems
            _ = LoadDataAsync();
        }

        // 加载数据-从json中读取数据
        private async Task LoadDataAsync()
        {
            errorItems = await DataService.LoadDataAsync();
            foreach (var item in errorItems)
            {
                AddNavigationViewItem(item);
            }
        }

        // 存储数据-把数据存储到json
        private async Task SaveDataAsync()
        {
            await DataService.SaveDataAsync(errorItems);
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
                ImagePaths = new List<string>(),
                Rating = -1,
            };
            // 对象中添加
            errorItems.Add(newErrorItem);
            // 导航栏中添加新项
            AddNavigationViewItem(newErrorItem);
            // 保存数据
            await SaveDataAsync();
        }

        // 关于
        private async void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // 创建一个 StackPanel 来包含 TextBlock 和 Image
            StackPanel contentPanel = new StackPanel();

            TextBlock aboutInfo = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // 创建不同颜色的文本
            aboutInfo.Inlines.Add(new Run { Text = "Created by ", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });

            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run { Text = "@QuincyZhao😀" });
            hyperlink.NavigateUri = new Uri("https://github.com/zhaoqianbiao");
            hyperlink.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

            aboutInfo.Inlines.Add(hyperlink);
            aboutInfo.Inlines.Add(new Run { Text = " in GCGS", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black) });

            // 创建一个 Image 控件
            Image aboutImage = new Image()
            {
                Source = new BitmapImage(new Uri("ms-appx:///Assets/peter.png")), // 替换为你的图片路径
                Width = 100,
                Height = 100,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // 将 TextBlock 和 Image 添加到 StackPanel
            contentPanel.Children.Add(aboutInfo);
            contentPanel.Children.Add(aboutImage);

            ContentDialog about = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot, // 确保 XamlRoot 设置正确
                Title = "About",
                Content = contentPanel, // 设置为 contentPanel 而不是 aboutInfo
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
        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (contentFrame.Content is ErrorDetailPage currentPage)
            {
                await currentPage.SaveCurrentContentAsync();
            }
            await SaveDataAsync(); // 确保数据在关闭时保存
            cts.Cancel(); // 取消所有异步操作
            Application.Current.Exit(); // 确保应用程序退出
        }

        // 右键菜单删除项
        
    }
}
