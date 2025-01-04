using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using ElectronicCorrectionNotebook.Services;
using System.Threading.Tasks;
using Windows.System;
using System.IO;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using static System.Net.Mime.MediaTypeNames;
using Windows.Storage.Streams;
using System.Collections.Generic;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElectronicCorrectionNotebook
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ErrorDetailPage : Page
    {
        //创建错题对象
        public ErrorItem ErrorItem { get; set; }
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private CancellationTokenSource cts;

        // Image类级别字段
        private Microsoft.UI.Xaml.Controls.Image dialogImage; 
        // Textblock类级别字段
        private TextBlock dialogTextBlock;

        private DispatcherTimer _dispatcherTimer;
        private DateTime _lastSaveTime;
        private TimeSpan _saveInterval = TimeSpan.FromMinutes(1);

        // 初始化页面
        public ErrorDetailPage()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
            StartAutoSaveTimer();
        }

        private void StartAutoSaveTimer()
        {
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            _dispatcherTimer.Start();
            _lastSaveTime = DateTime.Now;
            UpdateAutoSaveText();
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            var timeSinceLastSave = DateTime.Now - _lastSaveTime;
            if (timeSinceLastSave >= _saveInterval)
            {
                // 执行保存操作
                await SaveCurrentContentAsync();
                _lastSaveTime = DateTime.Now;
            }
            UpdateAutoSaveText();
        }

        private void UpdateAutoSaveText()
        {
            var timeSinceLastSave = DateTime.Now - _lastSaveTime;
            var timeUntilNextSave = _saveInterval - timeSinceLastSave;
            var displaytime = ((int)timeUntilNextSave.TotalSeconds / 10) * 10;
            saveTimeTextBlock.Text = $"Auto save in about {displaytime} s";
        }

        // 导航到页面 绑定数据到UI控件
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ErrorItem = e.Parameter as ErrorItem;
            if (ErrorItem != null)
            {
                // 绑定数据到UI控件
                TitleTextBox.Text = ErrorItem.Title;
                DatePicker.Date = ErrorItem.Date;
                DescriptionTextBox.Text = ErrorItem.Description;
                RatingChoose.Value = ErrorItem.Rating;
                DisplayFilesIcon();
            }
        }

        // 选择文件按钮点击事件
        private async void OnSelectFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    CommitButtonText = "Upload 上传",
                };
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var files = await picker.PickMultipleFilesAsync();
                if (files != null && files.Count > 0)
                {
                    var filesFolder = Path.Combine(appDataPath, "Files");
                    Directory.CreateDirectory(filesFolder);

                    foreach (var file in files)
                    {
                        var destinationPath = Path.Combine(filesFolder, file.Name);
                        await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(filesFolder), file.Name, NameCollisionOption.ReplaceExisting);
                        ErrorItem.FilePaths.Add(destinationPath);
                    }
                    DisplayFilesIcon();
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error selecting files", ex.Message);
            }
        }

        // 显示文件图标
        private void DisplayFilesIcon()
        {
            FilePanel.Items.Clear();
            foreach (var filePath in ErrorItem.FilePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var fileType = Path.GetExtension(filePath).ToLower();
                var bitmapImage = new BitmapImage();

                switch (fileType)
                {
                    case ".docx":
                    case ".doc":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/word.png"));
                        break;
                    case ".pptx":
                    case ".ppt":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/powerpoint.png"));
                        break;
                    case ".xlsx":
                    case ".xls":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/excel.png"));
                        break;
                    case ".pdf":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/pdf.png"));
                        break;
                    case ".txt":
                    case ".md":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/text.png"));
                        break;
                    case ".zip":
                    case ".rar":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/zip.png"));
                        break;
                    case ".xmind":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/xmind.png"));
                        break;
                    case ".mp3":
                    case ".mp4":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/video.png"));
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                        bitmapImage = new BitmapImage(new Uri(filePath));
                        break;
                    default:
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/unknown.png"));
                        break;
                }

                var image = new Microsoft.UI.Xaml.Controls.Image
                {
                    Source = bitmapImage,
                    Width = 100,
                    Height = 100,
                    Tag = filePath, // 将文件路径存储在Tag中，即使是文件图标，也会传递着文件的路径
                };
                image.Tapped += File_Tapped;

                // 创建右键菜单
                var contextMenu = new MenuFlyout();
                var deleteItem = new MenuFlyoutItem { Text = "Delete 删除" };
                deleteItem.Click += (sender, e) => DeleteImage(filePath);
                contextMenu.Items.Add(deleteItem);

                // 设置右键菜单
                image.ContextFlyout = contextMenu;

                var fileNameBlock = new TextBlock
                {
                    Text = fileName,
                    Width = 120,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center 
                };
                var contentPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                contentPanel.Children.Add(image);
                contentPanel.Children.Add(fileNameBlock);
                contentPanel.Margin = new Thickness(5);
                FilePanel.Items.Add(contentPanel);
            }
        }

        // 右键删除单个文件
        private async void DeleteImage(string filePath)
        {
            // 从 ErrorItem.FilePaths 中移除文件路径
            ErrorItem.FilePaths.Remove(filePath);
            var errorItems = await DataService.LoadDataAsync(cts.Token);
            var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
            if (existingItem != null)
            {
                existingItem.FilePaths = ErrorItem.FilePaths;
            }
            else
            {
                errorItems.Add(ErrorItem);
            }
            await DataService.SaveDataAsync(errorItems, cts.Token);
            // 重新显示文件图标
            DisplayFilesIcon();
        }

        // 点击图片放大
        private async void File_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is Microsoft.UI.Xaml.Controls.Image image && image.Tag is string filePath)
                {
                    var fileType = Path.GetExtension(filePath).ToLower();
                    if (fileType == ".jpg" || fileType == ".jpeg" || fileType == ".png" || fileType == ".bmp" || fileType == ".gif" || fileType == ".ico")
                    {
                        BitmapImage bitmap = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
                        dialogImage = new Microsoft.UI.Xaml.Controls.Image()
                        {
                            Source = bitmap,
                            Tag = filePath,
                            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                            ManipulationMode = ManipulationModes.All,
                        };
                        Dialog.Content = dialogImage;
                        /*DialogImage.Source = new BitmapImage(new Uri(filePath));
                        DialogImage.Tag = filePath;*/

                        await Dialog.ShowAsync();
                    }
                    else
                    {
                        if (fileType == ".txt")
                        {
                            var file = await StorageFile.GetFileFromPathAsync(filePath);
                            var texts = await FileIO.ReadTextAsync(file);
                            dialogTextBlock = new TextBlock()
                            {
                                Text = texts,
                                Tag = filePath,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(10),
                                IsTextSelectionEnabled = true,
                                
                            };

                            var textBlockScrollViewer = new ScrollViewer
                            {
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                Content = dialogTextBlock,
                            };

                            Dialog.Content = textBlockScrollViewer;
                            await Dialog.ShowAsync();
                        }
                        else
                        {
                            var file = await StorageFile.GetFileFromPathAsync(filePath);
                            if (file != null)
                            {
                                await Launcher.LaunchFileAsync(file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error opening file", ex.Message);
            }
        }

        // 以默认方式打开
        private async void OpenIamgeInSystemClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                string filePath = null;

                if (dialogImage != null && dialogImage.Tag is string imagePath)
                {
                    filePath = imagePath;
                }
                else if (dialogTextBlock != null && dialogTextBlock.Tag is string textBlockPath)
                {
                    filePath = textBlockPath;
                }

                if (filePath != null) 
                {
                    var file = await StorageFile.GetFileFromPathAsync(filePath);
                    if (file != null)
                    {
                        await Launcher.LaunchFileAsync(file);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error opening image in system", ex.Message);
            }
        }

        // 点击保存页面
        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveCurrentContentAsync();

                ContentDialog saveSuccess = new ContentDialog()
                {
                    XamlRoot = rootPanel.XamlRoot,
                    Title = "Saved 已保存",
                    Content = "Save successfully 保存成功！",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close,
                };
                PublicEvents.PlaySystemSound();
                await saveSuccess.ShowAsync();
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error saving content", ex.Message);
            }
        }

        // 点击删除页面
        private async void DeleteClick(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Confirm to delete 确认删除",
                Content = "Are you sure to delete this page forever? 你确定要删除这一页吗？",
                PrimaryButtonText = "Yes 是",
                CloseButtonText = "No 否",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };
            PublicEvents.PlaySystemSound();
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var errorItems = await DataService.LoadDataAsync(cts.Token);        // 取出总的errorItems List
                    var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);      // 在List中寻找和当前ErrorItem ID匹配的
                    if (existingItem != null)
                    {
                        errorItems.Remove(existingItem);        // 删去当前的那个item
                    }
                    await DataService.SaveDataAsync(errorItems, cts.Token);     // 保存errorItems List

                    var mainWindow = (MainWindow)App.MainWindow;
                    mainWindow.RemoveNavigationViewItem(ErrorItem);     // 给navigationView传递要删除的当前的ErrorItem

                    ContentDialog deleteSuccess = new ContentDialog()
                    {
                        XamlRoot = rootPanel.XamlRoot,
                        Title = "Deleted 已删除",
                        Content = "Delete successfully 删除成功！",
                        CloseButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Close,
                    };
                    PublicEvents.PlaySystemSound();
                    await deleteSuccess.ShowAsync();
                }
                catch (Exception ex)
                {
                    // 处理异常，例如记录日志或显示错误消息
                    await ShowErrorMessageAsync("Error deleting content", ex.Message);
                }
            }
        }

        // 日期设置为今天
        private void SetDateToTodayClick(object sender, RoutedEventArgs e)
        {
            ErrorItem.Date = DateTime.Now;
            DatePicker.Date = ErrorItem.Date;
        }

        // 保存当前内容
        public async Task SaveCurrentContentAsync()
        {
            // 保存到对象
            ErrorItem.Title = TitleTextBox.Text;
            ErrorItem.Date = DatePicker.Date.DateTime;
            ErrorItem.Description = DescriptionTextBox.Text;
            ErrorItem.Rating = RatingChoose.Value;

            try
            {
                var errorItems = await DataService.LoadDataAsync(cts.Token);
                var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
                if (existingItem != null)
                {
                    existingItem.Title = ErrorItem.Title;
                    existingItem.Date = ErrorItem.Date;
                    existingItem.Description = ErrorItem.Description;
                    existingItem.FilePaths = ErrorItem.FilePaths;
                    existingItem.Rating = ErrorItem.Rating;
                }
                else
                {
                    errorItems.Add(ErrorItem);
                }
                await DataService.SaveDataAsync(errorItems, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (Exception ex)
            {
                // 处理其他异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error saving data", ex.Message);
            }
        }

        // 标题文本框内容改变时 更新对象 更新列表项目名称
        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.Title = TitleTextBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // 显示错误消息
        private async Task ShowErrorMessageAsync(string title, string message)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = rootPanel.XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
            };
            await errorDialog.ShowAsync();
        }

        // 从剪切板上传玩意儿
        private async void OpenFromClipboardClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 确保本地文件夹存在
                var filesFolder = Path.Combine(appDataPath, "Files");
                Directory.CreateDirectory(filesFolder);

                var dataPackageView = Clipboard.GetContent();
                // 获取（多个）文件
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await dataPackageView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        foreach(var item in items)
                        {
                            var storageFile = item as StorageFile;
                            if (storageFile != null)
                            {
                                string uniqueFileName = $"{storageFile.Name}";
                                string destinationPath = Path.Combine(filesFolder, uniqueFileName);
                                await SaveFileToFixedPathAsync(storageFile, destinationPath);
                                ErrorItem.FilePaths.Add(destinationPath);
                            }

                        }
                        DisplayFilesIcon();
                    }
                }

                // 获取截图
                else if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    var bitmap = await dataPackageView.GetBitmapAsync();
                    if (bitmap != null)
                    {
                        using (IRandomAccessStream stream = await bitmap.OpenReadAsync())
                        {
                            string uniqueFileName = $"Image_{Guid.NewGuid()}.png";
                            string destinationPath = Path.Combine(filesFolder, uniqueFileName);
                            await SaveStreamToFileAsync(stream, destinationPath);
                            ErrorItem.FilePaths.Add(destinationPath);
                        }
                        DisplayFilesIcon();
                    }
                }

                // 获取复制的文本
                else if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    var text = await dataPackageView.GetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        string uniqueFileName = $"Text_{Guid.NewGuid()}.txt";
                        string destinationPath = Path.Combine(filesFolder, uniqueFileName);
                        await CreateTextFileWithFileNamesAsync(destinationPath, text);
                        ErrorItem.FilePaths.Add(destinationPath);
                        DisplayFilesIcon();
                    }
                }

                else
                {
                    var dialog = new ContentDialog
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Error 错误",
                        Content = "No images found 剪贴板中没有图像。",
                        CloseButtonText = "Ok 确定"
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error opening from clipboard", ex.Message);
            }
        }

        // 把剪切板的文件复制到Files文件夹（可操作任意文件）
        private async Task SaveFileToFixedPathAsync(StorageFile sourceFile, string destinationPath)
        {
            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(destinationPath));
            var destinationFile = await destinationFolder.CreateFileAsync(Path.GetFileName(destinationPath), CreationCollisionOption.ReplaceExisting);

            await sourceFile.CopyAndReplaceAsync(destinationFile);

            /*
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Success 成功",
                Content = $"Saved to 文件已保存到 {destinationFile.Path}",
                CloseButtonText = "Ok 确定"
            };
            await dialog.ShowAsync();
            */
        }

        // 把剪切板的Bitmap复制到Files文件夹（仅限截图）
        private async Task SaveStreamToFileAsync(IRandomAccessStream stream, string filePath)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
            var file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);

            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
            }

            /*
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Success 成功",
                Content = $"Saved to 图像已保存到 {file.Path}",
                CloseButtonText = "Ok 确定"
            };
            await dialog.ShowAsync();
            */
        }

        // 创建txt文本文件
        private async Task CreateTextFileWithFileNamesAsync(string filePath, string text_Content)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
            var file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, text_Content);
        }
    }
}
