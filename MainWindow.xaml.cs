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
using Windows.UI.WindowManagement;
using System.Runtime.InteropServices;
using WinRT;
using PInvoke;
using ElectronicCorrectionNotebook.DataStructure;
using System.IO;

namespace ElectronicCorrectionNotebook
{

    public sealed partial class MainWindow : Window
    {
        private List<ErrorItem> errorItems = new List<ErrorItem>();
        private CancellationTokenSource cts;

        private const int MinWidth = 1250;  // 设置最小宽度
        private const int MinHeight = 1250; // 设置最小高度

        private Microsoft.UI.Windowing.AppWindow appWindow;

        public MainWindow()
        {
            InitializeComponent();
            SubClassing();
            Closed += MainWindow_Closed;
            CoreApplication.Exiting += OnExiting;

            // 初始化 AppWindow
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            appWindow.SetIcon("Assets/im.ico");
            cts = new CancellationTokenSource();
            // 加载数据
            _ = LoadDataAsync(cts.Token);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }

        // 加载数据-从json中读取数据-不用改
        private async Task LoadDataAsync(CancellationToken token)
        {
            try
            {
                // List<ErrorItems> errorItems 获取数据
                errorItems = await DataService.LoadDataAsync(token);
                foreach (var item in errorItems)
                {
                    // 添加导航视图项 入口
                    AddNavigationViewItem(item);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error 错误",
                    Content = ex,
                    CloseButtonText = "Ok",
                    XamlRoot = this.Content.XamlRoot
                };
                var result = await errorDialog.ShowAsync();
            }
        }

        // 存储数据-把数据存储到json-调用DataServie-不用改
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
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error 错误",
                    Content = ex,
                    CloseButtonText = "Ok",
                    XamlRoot = this.Content.XamlRoot
                };
                var result = await errorDialog.ShowAsync();
            }
        }

        // 导航栏中添加新项-不用改
        private void AddNavigationViewItem(ErrorItem errorItem)
        {
            // 创建Grid布局
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标题
            var titleTextBlock = new TextBlock
            {
                Text = errorItem.Title,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleTextBlock, 0);

            // 用于显示胶囊的框框
            var tagBorder = new Border
            {
                Background = new SolidColorBrush(ColorHelper.FromArgb(225, 45, 45, 45)),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = errorItem.CorrectionTag
                }
            };
            Grid.SetColumn(tagBorder, 1);

            // 将标题和胶囊添加到Grid中
            grid.Children.Add(titleTextBlock);
            grid.Children.Add(tagBorder);

            // item，把总的stackPanel放进去
            var newItem = new NavigationViewItem
            {
                Content = grid,
                Icon = new SymbolIcon(Symbol.Comment),
                Tag = errorItem
            };

            nvSample.MenuItems.Add(newItem);
        }

        // 添加新错题-总步骤-已改
        private async void Add_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var newErrorItem = new ErrorItem
            {
                Title = "New Error",
                Date = DateTime.Now,
                CorrectionTag = "Empty Tag",
                FilePaths = new List<string>(),
                Rating = -1,
            };
            errorItems.Add(newErrorItem);
            AddNavigationViewItem(newErrorItem);
            await SaveDataAsync(cts.Token);
        }

        // 关于-不用改
        private async void About_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PublicEvents.PlaySystemSound();
            StackPanel contentPanel = new StackPanel();

            TextBlock aboutInfo = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };


            SolidColorBrush color = null;
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                color = new SolidColorBrush(Colors.White);
            }
            else
            {
                color = new SolidColorBrush(Colors.Black);
            }

            aboutInfo.Inlines.Add(new Run { Text = "Created by ", Foreground = color });

            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run { Text = "@QuincyZhao😀" });
            hyperlink.NavigateUri = new Uri("https://github.com/zhaoqianbiao");
            hyperlink.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

            aboutInfo.Inlines.Add(hyperlink);
            aboutInfo.Inlines.Add(new Run { Text = " in GCGS", Foreground = color });

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

        // 当title和tag更改时，更新导航视图项的名字-不用改
        public void UpdateNavigationViewItem(ErrorItem errorItem)
        {
            foreach (var menuItem in nvSample.MenuItems)
            {
                if (menuItem is NavigationViewItem item && item.Tag == errorItem)
                {
                    // 创建Grid布局
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // 标题
                    var titleTextBlock = new TextBlock
                    {
                        Text = errorItem.Title,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(titleTextBlock, 0);

                    // 用于显示胶囊的框框
                    var tagBorder = new Border
                    {
                        Background = new SolidColorBrush(ColorHelper.FromArgb(225, 45, 45, 45)),
                        CornerRadius = new CornerRadius(11),
                        Padding = new Thickness(8, 2, 8, 2),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Child = new TextBlock
                        {
                            Text = errorItem.CorrectionTag
                        }
                    };
                    Grid.SetColumn(tagBorder, 1);

                    // 将标题和胶囊添加到Grid中
                    grid.Children.Add(titleTextBlock);
                    grid.Children.Add(tagBorder);

                    item.Content = grid;
                    break;
                }

            }
        }

        // 导航视图选择更改时-不用改
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

        // 窗口关闭时-不用改
        public async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            /*
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
            */

            await SaveCurrentStateAsync();
            this.Closed -= MainWindow_Closed;
            Application.Current.Exit();
        }

        // 应用程序退出时-不用改
        private async void OnExiting(object sender, object e)
        {
            await SaveCurrentStateAsync();
        }

        // 保存当前状态-不用改
        private async Task SaveCurrentStateAsync()
        {
            if (contentFrame.Content is ErrorDetailPage currentPage)
            {
                await currentPage.SaveCurrentContentAsync();
            }
            await SaveDataAsync(cts.Token);
            cts.Cancel(); // 
        }

        // 删除项-不用改
        public async void RemoveNavigationViewItem(ErrorItem errorItem)
        {
            var selectedItem = nvSample.SelectedItem;
            nvSample.MenuItems.Remove(selectedItem);
            errorItems.Remove(errorItem);
            await SaveDataAsync(cts.Token);
            contentFrame.Content = null;
        }

        // 跳转的时候 自动选中对应的item
        private void SelectNavigationViewItem(ErrorItem errorItem)
        {
            foreach (var menuItem in nvSample.MenuItems)
            {
                if (menuItem is NavigationViewItem item && item.Tag == errorItem)
                {
                    nvSample.SelectedItem = item;
                    break;
                }
            }
        }

        #region AutoSuggestBoxCodeRegion

        // 在搜索框中输入的时候更改建议列表
        public void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = new List<string>();
                foreach (var item in errorItems)
                {
                    if (item.Title.Contains(sender.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(item.Title);
                    }
                }
                sender.ItemsSource = suggestions;
            }
        }

        // 点击某一项后，跳转到某一个页面
        public async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                foreach (var item in errorItems)
                {
                    if (item.Title == args.ChosenSuggestion)
                    {
                        // 为什么要把下面这行注释掉才能好呢，为什么导航两次会导致数据丢失？
                        // contentFrame.Navigate(typeof(ErrorDetailPage), item);
                        SelectNavigationViewItem(item); // 自动选中对应的item
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(args.QueryText))
            {
                foreach (var item in errorItems)
                {
                    if (item.Title == args.QueryText)
                    {
                        // contentFrame.Navigate(typeof(ErrorDetailPage), item);
                        SelectNavigationViewItem(item); // 自动选中对应的item
                        break;
                    }
                }
            }
        }

        // 确认点击某一项后，搜索栏中显示那一项的名称
        public void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }

        #endregion

        #region MinMaxCodeRegion
        private delegate IntPtr WinProc(IntPtr hWnd, User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
        private WinProc newWndProc = null;
        private IntPtr oldWndProc = IntPtr.Zero;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, User32.WindowLongIndexFlags nIndex, WinProc newProc);
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private void SubClassing()
        {
            // Get the Window's HWND
            var hwnd = this.As<IWindowNative>().WindowHandle;

            newWndProc = new WinProc(NewWindowProc);
            oldWndProc = SetWindowLongPtr(hwnd, User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private IntPtr NewWindowProc(IntPtr hWnd, User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case User32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = User32.GetDpiForWindow(hWnd);
                    // float scalingFactor = (float)dpi / 96;

                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(MinWidth);
                    minMaxInfo.ptMinTrackSize.y = (int)(MinHeight);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;
            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        #endregion

        
    }
}
