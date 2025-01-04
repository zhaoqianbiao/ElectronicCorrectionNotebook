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
        //�����������
        public ErrorItem ErrorItem { get; set; }
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private CancellationTokenSource cts;

        // Image�༶���ֶ�
        private Microsoft.UI.Xaml.Controls.Image dialogImage; 
        // Textblock�༶���ֶ�
        private TextBlock dialogTextBlock;

        // ��ʼ��ҳ��
        public ErrorDetailPage()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
        }

        // ������ҳ�� �����ݵ�UI�ؼ�
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ErrorItem = e.Parameter as ErrorItem;
            if (ErrorItem != null)
            {
                // �����ݵ�UI�ؼ�
                TitleTextBox.Text = ErrorItem.Title;
                DatePicker.Date = ErrorItem.Date;
                DescriptionTextBox.Text = ErrorItem.Description;
                RatingChoose.Value = ErrorItem.Rating;
                DisplayFilesIcon();
            }
        }

        // ѡ���ļ���ť����¼�
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error selecting files", ex.Message);
            }
        }

        // ��ʾ�ļ�ͼ��
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
                    Tag = filePath, // ���ļ�·���洢��Tag�У���ʹ���ļ�ͼ�꣬Ҳ�ᴫ�����ļ���·��
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

        // ���ͼƬ�Ŵ�
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening file", ex.Message);
            }
        }

        // ��Ĭ�Ϸ�ʽ��
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening image in system", ex.Message);
            }
        }

        // �����������
        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveCurrentContentAsync();

                ContentDialog saveSuccess = new ContentDialog()
                {
                    XamlRoot = rootPanel.XamlRoot,
                    Title = "Saved �ѱ���",
                    Content = "Save successfully ����ɹ���",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close,
                };
                PublicEvents.PlaySystemSound();
                await saveSuccess.ShowAsync();
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving content", ex.Message);
            }
        }

        // ��������Ϊ����
        private void SetDateToTodayClick(object sender, RoutedEventArgs e)
        {
            ErrorItem.Date = DateTime.Now;
            DatePicker.Date = ErrorItem.Date;
        }

        // ���浱ǰ����
        public async Task SaveCurrentContentAsync()
        {
            // ���浽����
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
                // ���������쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving data", ex.Message);
            }
        }

        // �����ı������ݸı�ʱ ���¶��� �����б���Ŀ����
        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.Title = TitleTextBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // ��ʾ������Ϣ
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

        // �Ӽ��а��ϴ������
        private async void OpenFromClipboardClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // ȷ�������ļ��д���
                var filesFolder = Path.Combine(appDataPath, "Files");
                Directory.CreateDirectory(filesFolder);

                var dataPackageView = Clipboard.GetContent();
                // ��ȡ��������ļ�
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

                // ��ȡ��ͼ
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

                // ��ȡ���Ƶ��ı�
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
                        Title = "Error ����",
                        Content = "No images found ��������û��ͼ��",
                        CloseButtonText = "Ok ȷ��"
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening from clipboard", ex.Message);
            }
        }

        // �Ѽ��а���ļ����Ƶ�Files�ļ��У��ɲ��������ļ���
        private async Task SaveFileToFixedPathAsync(StorageFile sourceFile, string destinationPath)
        {
            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(destinationPath));
            var destinationFile = await destinationFolder.CreateFileAsync(Path.GetFileName(destinationPath), CreationCollisionOption.ReplaceExisting);

            await sourceFile.CopyAndReplaceAsync(destinationFile);

            /*
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Success �ɹ�",
                Content = $"Saved to �ļ��ѱ��浽 {destinationFile.Path}",
                CloseButtonText = "Ok ȷ��"
            };
            await dialog.ShowAsync();
            */
        }

        // �Ѽ��а��Bitmap���Ƶ�Files�ļ��У����޽�ͼ��
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
                Title = "Success �ɹ�",
                Content = $"Saved to ͼ���ѱ��浽 {file.Path}",
                CloseButtonText = "Ok ȷ��"
            };
            await dialog.ShowAsync();
            */
        }

        // ����txt�ı��ļ�
        private async Task CreateTextFileWithFileNamesAsync(string filePath, string text_Content)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
            var file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, text_Content);
        }
    }
}
