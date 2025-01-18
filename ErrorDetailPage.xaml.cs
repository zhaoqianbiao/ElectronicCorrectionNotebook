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
using Windows.Storage.Streams;
using System.Collections.Generic;
using Windows.Media.Core;
using System.Diagnostics;
using ElectronicCorrectionNotebook.DataStructure;
using Microsoft.UI.Xaml.Media;

namespace ElectronicCorrectionNotebook
{
    public sealed partial class ErrorDetailPage : Page
    {
        //�����������
        public ErrorItem ErrorItem { get; set; }
        // ����·��
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");
        private CancellationTokenSource cts;

        // Image�༶���ֶ�
        private Microsoft.UI.Xaml.Controls.Image dialogImage; 
        // Textblock�༶���ֶ�
        private TextBlock dialogTextBlock;
        // MediaPlayerElement�༶���ֶ�
        private MediaPlayerElement dialogMediaPlayer;

        // ��ʼ��ҳ��
        public ErrorDetailPage()
        {
            this.InitializeComponent();
            cts = new CancellationTokenSource();
        }

        // ������ҳ�� �����ݵ�UI�ؼ�
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ErrorItem = e.Parameter as ErrorItem;
            if (ErrorItem != null)
            {
                // �����ݵ�UI�ؼ�
                TitleTextBox.Text = ErrorItem.Title;
                TagBox.Text = ErrorItem.CorrectionTag;
                DatePicker.Date = ErrorItem.Date;

                // ��ȡ��������ļ���·��
                StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook"));
                StorageFolder rtfFolder = await localFolder.CreateFolderAsync("rtfFiles", CreationCollisionOption.OpenIfExists);
                string rtfFileName = $"{ErrorItem.Id}.rtf";
                StorageFile rtfFile = await rtfFolder.CreateFileAsync(rtfFileName, CreationCollisionOption.OpenIfExists);

                // ���ظ��ı��������
                try
                {
                    // ����ļ��Ƿ����
                    StorageFile file = await rtfFolder.GetFileAsync(rtfFileName);
                    if (file != null)
                    {
                        // ������Ѿ����ڵ�ҳ��
                        using (var stream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            DescriptionRichEditBox.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, stream);
                        }
                    }
                    else
                    {
                        // �ļ������ڣ���ʼ��һ���յ� RTF �ĵ�
                        DescriptionRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    // �����쳣�������¼��־����ʾ������Ϣ
                    Debug.WriteLine($"Error loading RTF file: {ex.Message}");
                    DescriptionRichEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, string.Empty);
                }

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
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    CommitButtonText = "Upload �ϴ�",
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
                        // ���Ŀ���ļ��Ƿ���ڣ����⸲��
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error selecting files", ex);
            }
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
                if (dataPackageView == null)
                {
                    throw new InvalidOperationException("Clipboard content is null.");
                }

                // ��ȡ��������ļ�
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

                                // ���Ŀ���ļ��Ƿ���ڣ����⸲��
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
                        CloseButtonText = "Ok ȷ��",
                        FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]
                    };
                    await dialog.ShowAsync();
                }
                await SaveCurrentContentAsync();
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening from clipboard", ex);
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
                    case ".mkv":
                        bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/video.png"));
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                        // ���ͼƬ���沢���¼���ͼƬ
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
                    Tag = filePath, // ���ļ�·���洢��Tag�У���ʹ���ļ�ͼ�꣬Ҳ�ᴫ�����ļ���·��
                };
                image.Tapped += File_Tapped;

                // �����Ҽ��˵�
                var contextMenu = new MenuFlyout();

                // �˵�1 �����ļ�
                var copyItem = new MenuFlyoutItem { Text = "Copy ����", FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]};
                copyItem.Click += (sender, e) => CopyFile(filePath);
                contextMenu.Items.Add(copyItem);

                // �˵�2 ɾ��
                var deleteItem = new MenuFlyoutItem { Text = "Delete ɾ��", FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]};
                deleteItem.Click += (sender, e) => DeleteImage(filePath);
                contextMenu.Items.Add(deleteItem);

                // �����Ҽ��˵�
                image.ContextFlyout = contextMenu;

                var fileNameBlock = new TextBlock
                {
                    Text = fileName,
                    Width = 120,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    Margin = new Thickness(0,5,0,0),
                    FontFamily = (FontFamily)Application.Current.Resources["FontRegular"],
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

        // �Ҽ��˵� ɾ�������ļ�
        private async void DeleteImage(string filePath)
        {
            try
            {
                // �� ErrorItem.FilePaths ���Ƴ��ļ�·��
                ErrorItem.FilePaths.Remove(filePath);

                // ɾ�������ļ�
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

                // ������ʾ�ļ�ͼ��
                DisplayFilesIcon();
                await SaveCurrentContentAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Error deleting image", ex);
            }
        }

        // �Ҽ��˵� ���Ƶ����ļ�
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

        // ����ļ�ͼ�� �ж���dialog����ֱ�Ӵ�
        private async void File_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is Microsoft.UI.Xaml.Controls.Image image && image.Tag is string filePath)
                {
                    var fileType = Path.GetExtension(filePath).ToLower();
                    // ��ʾԤ��ͼƬ
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

                        Dialog.Content = dialogImage;
                        await Dialog.ShowAsync();
                    }
                    // ��ʾԤ��txt�ļ�
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
                            FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]
                        };

                        var textBlockScrollViewer = new ScrollViewer
                        {
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Content = dialogTextBlock,
                        };

                        Dialog.Content = textBlockScrollViewer;
                        await Dialog.ShowAsync();
                    }
                    // Ԥ��������Ƶ����Ƶ
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
                    // ��Ĭ�Ϸ�ʽ���������͵��ļ�
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening file", ex);
            }
        }

        // �ص�ý�岥�ŶԻ���
        private void mediaDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (dialogMediaPlayer != null)
            {
                var playbackState = dialogMediaPlayer.MediaPlayer.PlaybackSession.PlaybackState;
                dialogMediaPlayer.MediaPlayer.Pause();
            }
        }

        // ��Ĭ�Ϸ�ʽ��
        private async void OpenFileInSystemClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                string filePath = null;
                // ��Ҫ��ÿ�ε�dialog�رյ�ʱ�򣬰�dialogImage��dialogTextBlock��dialogMediaPlayer�ÿ�
                // ��ȻdialogImageÿ�ζ��Ḳ�������textblock��mediaplayer
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
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error opening image in system", ex);
            }
            finally
            {
                dialogImage = null;
                dialogTextBlock = null;
                dialogMediaPlayer = null;
            }
        }

        // �������ҳ��
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
                    FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]
                };
                PublicEvents.PlaySystemSound();
                await saveSuccess.ShowAsync();
            }
            catch (Exception ex)
            {
                // �����쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving content", ex);
            }
        }

        // ���ɾ��ҳ��
        private async void DeleteClick(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Confirm to delete ȷ��ɾ��",
                Content = "Are you sure to delete this page forever? ��ȷ��Ҫɾ����һҳ��",
                PrimaryButtonText = "Yes ��",
                CloseButtonText = "No ��",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
                FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]
            };
            PublicEvents.PlaySystemSound();
            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // ɾ������Files��Ķ�Ӧ�ļ�
                    foreach (var filePath in ErrorItem.FilePaths)
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    // ɾ������rtfFiles��Ķ�Ӧ�ļ�
                    // ����Ĳ����ã���֪��Ϊɶ
                    // string rtfFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "rtfFiles", $"{ErrorItem.Id}.rtf");
                    string rtfFilePath = Path.Combine(appDataPath, "rtfFiles", $"{ErrorItem.Id}.rtf");

                    if (File.Exists(rtfFilePath))
                    {
                        File.Delete(rtfFilePath);
                    }
                    

                    var errorItems = await DataService.LoadDataAsync(cts.Token); // ȡ���ܵ�errorItems List
                    var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id); // ��List��Ѱ�Һ͵�ǰErrorItem IDƥ���
                    if (existingItem != null)
                    {
                        errorItems.Remove(existingItem); // ɾȥ��ǰ���Ǹ�item
                    }
                    await DataService.SaveDataAsync(errorItems, cts.Token); // ����errorItems List

                    var mainWindow = (MainWindow)App.MainWindow;
                    mainWindow.RemoveNavigationViewItem(ErrorItem); // ��navigationView����Ҫɾ���ĵ�ǰ��ErrorItem

                    
                }
                catch (Exception ex)
                {
                    // �����쳣�������¼��־����ʾ������Ϣ
                    await ShowErrorMessageAsync("Error deleting content", ex);
                }
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
            ErrorItem.CorrectionTag = TagBox.Text;
            ErrorItem.Date = DatePicker.Date;
            ErrorItem.Rating = RatingChoose.Value;

            try
            {
                // ��ȡ��������ļ���·��
                StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook"));
                StorageFolder rtfFolder = await localFolder.CreateFolderAsync("rtfFiles", CreationCollisionOption.OpenIfExists);
                string rtfFileName = $"{ErrorItem.Id}.rtf";
                StorageFile rtfFile = await rtfFolder.CreateFileAsync(rtfFileName, CreationCollisionOption.ReplaceExisting);

                // ���� RichEditBox �����ݵ� RTF �ļ�
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
                // ���������쳣�������¼��־����ʾ������Ϣ
                await ShowErrorMessageAsync("Error saving data", ex);
            }
        }

        // ��ʾ������Ϣ
        private async Task ShowErrorMessageAsync(string title, Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = title,
                Content = ex.Message,
                CloseButtonText = "Ok ȷ��",
                FontFamily = (FontFamily)Application.Current.Resources["FontRegular"]
            };
            await errorDialog.ShowAsync();
        }

        // �Ѽ��а���ļ����Ƶ�Files�ļ��У��ɲ��������ļ���
        private async Task SaveFileToFixedPathAsync(StorageFile sourceFile, string destinationPath)
        {
            
            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(destinationPath));
            var destinationFile = await destinationFolder.CreateFileAsync(Path.GetFileName(destinationPath), CreationCollisionOption.ReplaceExisting);
            await sourceFile.CopyAndReplaceAsync(destinationFile);
            
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
        }

        // ����txt�ı��ļ�
        private async Task CreateTextFileWithFileNamesAsync(string filePath, string text_Content)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(filePath));
            var file = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, text_Content);
        }

        // tag�ı������ݸı�ʱ ���¶��� �����б���Ŀ����
        private void TagBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.CorrectionTag = TagBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // title�ı������ݸı�ʱ ���¶��� �����б���Ŀ����
        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorItem != null)
            {
                ErrorItem.Title = TitleTextBox.Text;
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.UpdateNavigationViewItem(ErrorItem);
            }
        }

        // ˢ�°�ť����¼�
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayFilesIcon();
        }

        #region RichEditBox Settings

        // ���尴ť
        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionRichEditBox.Document.Selection.CharacterFormat.Bold = Microsoft.UI.Text.FormatEffect.Toggle;
        }

        // б�尴ť
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionRichEditBox.Document.Selection.CharacterFormat.Italic = Microsoft.UI.Text.FormatEffect.Toggle;
        }

        // �»��߰�ť
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

        // ������ɫ
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedColor = (Button)sender;
            var rectangle = (Microsoft.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Microsoft.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

            DescriptionRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;

            fontColorButton.Flyout.Hide();
            DescriptionRichEditBox.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }

        // ������ɫ
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
