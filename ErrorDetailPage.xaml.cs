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

        // ��ʼ��ҳ��
        public ErrorDetailPage()
        {
            this.InitializeComponent();
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
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add("*");  // ����ѡ�������ļ�

            // ��ȡ��ǰ���ھ��
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                // ���ļ����Ƶ����ش洢Files�ļ���
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

                var image = new Image
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
            if (sender is Image image && image.Tag is string filePath)
            {
                var fileType = Path.GetExtension(filePath).ToLower();
                if (fileType == ".jpg" || fileType == ".jpeg" || fileType == ".png" || fileType == ".bmp" || fileType == ".gif" || fileType == ".ico")
                {
                    // �����ͼƬ����ʾ��ͼ
                    DialogImage.Source = new BitmapImage(new Uri(filePath));
                    DialogImage.Tag = filePath; // ���ļ�·���洢��Tag��
                    await ImageDialog.ShowAsync();
                }
                else
                {
                    // ����������ļ����ͣ���Ĭ�Ϸ�ʽ���ļ�
                    var file = await StorageFile.GetFileFromPathAsync(filePath);
                    if (file != null)
                    {
                        await Launcher.LaunchFileAsync(file);
                    }
                }
            }
        }

        // ��Ĭ�Ϸ�ʽ��
        private async void OpenIamgeInSystemClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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

        // �����������
        private async void OnSaveClick(object sender, RoutedEventArgs e)
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

            // �������ݸ�DataService ����
            var errorItems = await DataService.LoadDataAsync();
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
            await DataService.SaveDataAsync(errorItems);
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

        
    }
}
