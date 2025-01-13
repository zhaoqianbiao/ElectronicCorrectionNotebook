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
using Windows.Media.Playback;
using Windows.Media.Core;
using System.Diagnostics;
using ElectronicCorrectionNotebook.DataStructure;

namespace ElectronicCorrectionNotebook
{
    public sealed partial class ErrorDetailPage : Page
    {
        //创建错题对象
        public ErrorItem ErrorItem { get; set; }
        // 数据路径
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private CancellationTokenSource cts;

        // Image类级别字段
        private Microsoft.UI.Xaml.Controls.Image dialogImage; 
        // Textblock类级别字段
        private TextBlock dialogTextBlock;
        // MediaPlayerElement类级别字段
        private MediaPlayerElement dialogMediaPlayer;

        // 计时器保存
        private DispatcherTimer _dispatcherTimer;
        private DateTime _lastSaveTime;
        private TimeSpan _saveInterval = TimeSpan.FromMinutes(1);

        // 初始化页面
        public ErrorDetailPage()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
        }

        #region 保存计时器
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

        #endregion

        // 导航到页面 绑定数据到UI控件
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ErrorItem = e.Parameter as ErrorItem;
            if (ErrorItem != null)
            {
                // 绑定数据到UI控件
                TitleTextBox.Text = ErrorItem.Title;
                TagBox.Text = ErrorItem.CorrectionTag;
                DatePicker.Date = ErrorItem.Date;

                // 获取软件数据文件夹路径
                StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook"));
                StorageFolder rtfFolder = await localFolder.CreateFolderAsync("rtfFiles", CreationCollisionOption.OpenIfExists);
                string rtfFileName = $"{ErrorItem.Id}.rtf";
                StorageFile rtfFile = await rtfFolder.CreateFileAsync(rtfFileName, CreationCollisionOption.OpenIfExists);

                // 加载富文本框的内容
                try
                {
                    // 检查文件是否存在
                    StorageFile file = await rtfFolder.GetFileAsync(rtfFileName);
                    if (file != null)
                    {
                        // 如果是已经存在的页面
                        using (var stream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            DescriptionRichEditBox.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, stream);
                        }
                    }
                    else
                    {
                        // 文件不存在，初始化一个空的 RTF 文档
                        DescriptionRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    // 处理异常，例如记录日志或显示错误消息
                    Debug.WriteLine($"Error loading RTF file: {ex.Message}");
                    DescriptionRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, string.Empty);
                }

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
                        // 检查目标文件是否存在，避免覆盖
                        if (File.Exists(destinationPath))
                        {
                            string uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.Name)}_{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
                            destinationPath = Path.Combine(filesFolder, uniqueFileName);
                        }
                        await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(filesFolder), Path.GetFileName(destinationPath), NameCollisionOption.GenerateUniqueName);
                        ErrorItem.FilePaths.Add(destinationPath);
                    }
                    DisplayFilesIcon();
                    await SaveCurrentContentAsync();
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error selecting files", ex);
            }
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
                if (dataPackageView == null)
                {
                    throw new InvalidOperationException("Clipboard content is null.");
                }

                // 获取（多个）文件
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await dataPackageView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            var storageFile = item as StorageFile;
                            if (storageFile != null)
                            {
                                string destinationPath = Path.Combine(filesFolder, storageFile.Name);

                                // 检查目标文件是否存在，避免覆盖
                                if (File.Exists(destinationPath))
                                {
                                    string uniqueFileName = $"{Path.GetFileNameWithoutExtension(storageFile.Name)}_{Guid.NewGuid()}{Path.GetExtension(storageFile.Name)}";
                                    destinationPath = Path.Combine(filesFolder, uniqueFileName);
                                }

                                /*
                                // string uniqueFileName = $"{storageFile.Name}";
                                string uniqueFileName = $"{Guid.NewGuid()}_{storageFile.Name}";
                                string destinationPath = Path.Combine(filesFolder, uniqueFileName);
                                */

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
                await SaveCurrentContentAsync();
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error opening from clipboard", ex);
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
                    case ".mkv":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/video.png"));
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                        // 清空图片缓存并重新加载图片
                        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmapImage.UriSource = new Uri(filePath);
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

                // 菜单1 复制文件
                var copyItem = new MenuFlyoutItem { Text = "Copy 复制" };
                copyItem.Click += (sender, e) => CopyFile(filePath);
                contextMenu.Items.Add(copyItem);

                // 菜单2 删除
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

        // 右键菜单 删除单个文件
        private async void DeleteImage(string filePath)
        {
            try
            {
                // 从 ErrorItem.FilePaths 中移除文件路径
                ErrorItem.FilePaths.Remove(filePath);

                // 删除本地文件
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

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
                await SaveCurrentContentAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Error deleting image", ex);
            }
        }

        // 右键菜单 复制单个文件
        private async void CopyFile(string filePath)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                if (file == null)
                {
                    throw new FileNotFoundException("File not found.", filePath);
                }

                var dataPackage = new DataPackage();
                dataPackage.SetStorageItems(new List<IStorageItem> { file });
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Error copying file", ex);
            }
        }

        // 点击文件图标 判断是dialog还是直接打开
        private async void File_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is Microsoft.UI.Xaml.Controls.Image image && image.Tag is string filePath)
                {
                    var fileType = Path.GetExtension(filePath).ToLower();
                    // 显示预览图片
                    if (fileType == ".jpg" || fileType == ".jpeg" || fileType == ".png" || fileType == ".bmp" || fileType == ".gif" || fileType == ".ico")
                    {
                        BitmapImage bitmap = new BitmapImage(new Uri(filePath, UriKind.RelativeOrAbsolute));
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.UriSource = new Uri(filePath);
                        dialogImage = new Microsoft.UI.Xaml.Controls.Image
                        {
                            Source = bitmap,
                            Tag = filePath
                        };

                        /* 有bug, 自动跳转到不知道为什么
                        var scrollView = new ScrollView
                        {
                            Content = dialogImage,
                            ZoomMode = ScrollingZoomMode.Enabled,
                            // ContentOrientation = ScrollingContentOrientation.None,
                            // VerticalAlignment = VerticalAlignment.Top,
                            // HorizontalAlignment = HorizontalAlignment.Left,
                            HorizontalScrollMode = ScrollingScrollMode.Auto,
                            HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Auto,
                            VerticalScrollMode = ScrollingScrollMode.Auto,
                            VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Visible,
                            MinZoomFactor = 1.0
                        };
                        */

                        Dialog.Content = dialogImage;
                        await Dialog.ShowAsync();
                    }
                    // 显示预览txt文件
                    else if (fileType == ".txt")
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
                    // 预览播放视频或音频
                    else if (fileType == ".mp3" || fileType == ".mp4" || fileType == ".mkv")
                    {
                        var file = await StorageFile.GetFileFromPathAsync(filePath);

                        dialogMediaPlayer = new MediaPlayerElement
                        {
                            AreTransportControlsEnabled = true,
                            Source = MediaSource.CreateFromUri(new Uri(filePath)),
                            AutoPlay = false,
                            Height = Double.NaN,
                            Tag = filePath
                        };

                        Dialog.Content = dialogMediaPlayer;
                        Dialog.Closed += mediaDialog_Closed;
                        await Dialog.ShowAsync();
                    }
                    // 以默认方式打开其他类型的文件
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
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或显示错误消息
                await ShowErrorMessageAsync("Error opening file", ex);
            }
        }

        // 关掉媒体播放对话框
        private void mediaDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (dialogMediaPlayer != null)
            {
                var playbackState = dialogMediaPlayer.MediaPlayer.PlaybackSession.PlaybackState;
                dialogMediaPlayer.MediaPlayer.Pause();
            }
        }

        // 以默认方式打开
        private async void OpenFileInSystemClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                string filePath = null;
                // 需要在每次的dialog关闭的时候，把dialogImage、dialogTextBlock、dialogMediaPlayer置空
                // 不然dialogImage每次都会覆盖下面的textblock和mediaplayer
                if (dialogImage != null && dialogImage.Tag is string imagePath)
                {
                    filePath = imagePath;
                }
                else if (dialogTextBlock != null && dialogTextBlock.Tag is string textBlockPath)
                {
                    filePath = textBlockPath;
                }
                else if (dialogMediaPlayer != null && dialogMediaPlayer.Tag is string mediaPath)
                {
                    filePath = mediaPath;
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
                await ShowErrorMessageAsync("Error opening image in system", ex);
            }
            finally
            {
                // !!!!!!!!!!!重要！！
                dialogImage = null;
                dialogTextBlock = null;
                dialogMediaPlayer = null;
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
                await ShowErrorMessageAsync("Error saving content", ex);
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
                    // 删除本地Files里的对应文件
                    foreach (var filePath in ErrorItem.FilePaths)
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    // 删除本地rtfFiles里的对应文件
                    // 下面的不能用，不知道为啥
                    // string rtfFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "rtfFiles", $"{ErrorItem.Id}.rtf");
                    string rtfFilePath = Path.Combine(appDataPath, "rtfFiles", $"{ErrorItem.Id}.rtf");

                    if (File.Exists(rtfFilePath))
                    {
                        File.Delete(rtfFilePath);
                    }
                    

                    var errorItems = await DataService.LoadDataAsync(cts.Token); // 取出总的errorItems List
                    var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id); // 在List中寻找和当前ErrorItem ID匹配的
                    if (existingItem != null)
                    {
                        errorItems.Remove(existingItem); // 删去当前的那个item
                    }
                    await DataService.SaveDataAsync(errorItems, cts.Token); // 保存errorItems List

                    var mainWindow = (MainWindow)App.MainWindow;
                    mainWindow.RemoveNavigationViewItem(ErrorItem); // 给navigationView传递要删除的当前的ErrorItem

                    /*
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
                    */
                }
                catch (Exception ex)
                {
                    // 处理异常，例如记录日志或显示错误消息
                    await ShowErrorMessageAsync("Error deleting content", ex);
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
            ErrorItem.CorrectionTag = TagBox.Text;
            ErrorItem.Date = DatePicker.Date.DateTime;
            ErrorItem.Rating = RatingChoose.Value;

            try
            {
                // 获取软件数据文件夹路径
                StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook"));
                StorageFolder rtfFolder = await localFolder.CreateFolderAsync("rtfFiles", CreationCollisionOption.OpenIfExists);
                string rtfFileName = $"{ErrorItem.Id}.rtf";
                StorageFile rtfFile = await rtfFolder.CreateFileAsync(rtfFileName, CreationCollisionOption.ReplaceExisting);

                // 保存 RichEditBox 的内容到 RTF 文件
                using (var stream = await rtfFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    DescriptionRichEditBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, stream);
                }

                var errorItems = await DataService.LoadDataAsync(cts.Token);
                var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
                if (existingItem != null)
                {
                    existingItem.Title = ErrorItem.Title;
                    existingItem.Date = ErrorItem.Date;
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
                await ShowErrorMessageAsync("Error saving data", ex);
            }
        }

        // 显示错误消息
        private async Task ShowErrorMessageAsync(string title, Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = title,
                Content = ex.Message,
                CloseButtonText = "Ok 确定"
            };
            await errorDialog.ShowAsync();
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

        // tag文本框内容改变时 更新对象 更新列表项目名称
        private void TagBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.CorrectionTag = TagBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // title文本框内容改变时 更新对象 更新列表项目名称
        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.Title = TitleTextBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // 强制刷新
        /*private void ForceDisplayFilesIcon()
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
                    case ".mkv":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/video.png"));
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                        // 清空图片缓存并重新加载图片
                        bitmapImage = new BitmapImage();
                        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmapImage.UriSource = new Uri(filePath);
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

                // 菜单1 复制文件
                var copyItem = new MenuFlyoutItem { Text = "Copy 复制" };
                copyItem.Click += (sender, e) => CopyFile(filePath);
                contextMenu.Items.Add(copyItem);

                // 菜单2 删除
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
        */

        // 刷新按钮点击事件
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayFilesIcon();
        }

        #region RichEditBox Settings

        // 粗体按钮
        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionRichEditBox.Document.Selection.CharacterFormat.Bold = Microsoft.UI.Text.FormatEffect.Toggle;
        }

        // 斜体按钮
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionRichEditBox.Document.Selection.CharacterFormat.Italic = Microsoft.UI.Text.FormatEffect.Toggle;
        }

        // 下划线按钮
        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            var selection = DescriptionRichEditBox.Document.Selection;
            if (selection.CharacterFormat.Underline == Microsoft.UI.Text.UnderlineType.None)
            {
                selection.CharacterFormat.Underline = Microsoft.UI.Text.UnderlineType.Single;
            }
            else
            {
                selection.CharacterFormat.Underline = Microsoft.UI.Text.UnderlineType.None;
            }
        }

        // 字体颜色
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedColor = (Button)sender;
            var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

            DescriptionRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;

            fontColorButton.Flyout.Hide();
            DescriptionRichEditBox.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }

        // 高亮颜色
        private void HighlightButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedColor = (Button)sender;
            var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

            DescriptionRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = color;

            fontColorButton.Flyout.Hide();
            DescriptionRichEditBox.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }


        /*private void DescriptionRichEditBox_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DescriptionRichEditBox.Document.Selection.SetRange(0, 0);
        }*/

        #endregion
    }
}
