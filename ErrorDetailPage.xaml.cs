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

        // 初始化页面
        public ErrorDetailPage()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
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
                    SuggestedStartLocation = PickerLocationId.Desktop
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
                        DialogImage.Source = new BitmapImage(new Uri(filePath));
                        DialogImage.Tag = filePath;
                        await ImageDialog.ShowAsync();
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
                if (DialogImage.Tag is string filePath)
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

        // 点击保存数据
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

        /*private async void OpenFromClipboardClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var filesFolder = Path.Combine(appDataPath, "Files");
                Directory.CreateDirectory(filesFolder);

                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await dataPackageView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (storageFile != null)
                        {
                            await SaveFileToFixedPathAsync(storageFile, filesFolder);
                        }
                    }
                        

                    foreach (var bitmap in package)
                    {
                        var destinationPath = Path.Combine(filesFolder, file.Name);
                        await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(filesFolder), file.Name, NameCollisionOption.ReplaceExisting);
                        ErrorItem.FilePaths.Add(destinationPath);
                    }
                    DisplayFilesIcon();
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        XamlRoot = rootPanel.XamlRoot,
                        Title = "Error 错误",
                        Content = "No images in clipboard! 剪贴板中没有图像",
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

        private async Task SaveFileToFixedPathAsync(StorageFile sourceFile, string destinationPath)
        {
            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(destinationPath));
            var destinationFile = await destinationFolder.CreateFileAsync(Path.GetFileName(destinationPath), CreationCollisionOption.ReplaceExisting);

            await sourceFile.CopyAndReplaceAsync(destinationFile);

            var dialog = new ContentDialog
            {
                XamlRoot = rootPanel.XamlRoot,
                Title = "成功",
                Content = $"图像已保存到 {destinationFile.Path}",
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }*/
    }
}
