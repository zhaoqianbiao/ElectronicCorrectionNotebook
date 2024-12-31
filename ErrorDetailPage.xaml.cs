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
                DisplayImages();
            }
        }

        // ѡ��ͼƬ��ť����¼�
        private async void OnSelectImageClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // ��ȡ��ǰ���ھ��
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                // ��ͼƬ���Ƶ����ش洢
                var localFolder = ApplicationData.Current.LocalFolder;
                foreach (var file in files)
                {
                    var copiedFile = await file.CopyAsync(localFolder, file.Name, NameCollisionOption.ReplaceExisting);
                    ErrorItem.ImagePaths.Add(copiedFile.Path);
                }
                DisplayImages();
            }
        }

        private void DisplayImages()
        {
            ImagePanel.Items.Clear();
            foreach (var imagePath in ErrorItem.ImagePaths)
            {
                var bitmapImage = new BitmapImage(new Uri(imagePath));
                var image = new Image
                {
                    Source = bitmapImage,
                    Width = 100,
                    Height = 100,
                    Margin = new Thickness(5),
                };
                image.Tapped += Image_Tapped;
                ImagePanel.Items.Add(image);
            }
        }

        // ���ͼƬ�Ŵ�
        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // ImageScrollViewer.ChangeView(null, null, 0);
            var image = sender as Image;
            if (image != null)
            {
                DialogImage.Source = image.Source;
                DialogImage.Tag = ((BitmapImage)image.Source).UriSource.LocalPath; // ��ͼƬ·���洢�� Tag ��
                await ImageDialog.ShowAsync();
            }
        }

        // ��ϵͳ�鿴���д�ͼƬ
        private async void OpenInSystemViewerClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var imagePath = DialogImage.Tag as string;
            if (!string.IsNullOrEmpty(imagePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(imagePath);
                if (file != null)
                {
                    await Launcher.LaunchFileAsync(file);
                }
            }
        }

        // �����������
        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            /*
            ErrorItem.Title = TitleTextBox.Text;
            ErrorItem.Date = DatePicker.Date.DateTime;
            ErrorItem.Description = DescriptionTextBox.Text;

            // �������ݵ������ļ�
            var errorItems = await DataService.LoadDataAsync();
            var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
            if (existingItem != null)
            {
                existingItem.Title = ErrorItem.Title;
                existingItem.Date = ErrorItem.Date;
                existingItem.Description = ErrorItem.Description;
                existingItem.ImagePath = ErrorItem.ImagePath;
            }
            else
            {
                errorItems.Add(ErrorItem);
            }
            await DataService.SaveDataAsync(errorItems);
            */
            await SaveCurrentContentAsync();
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

            // �������ݸ�DataService ����
            var errorItems = await DataService.LoadDataAsync();
            var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
            if (existingItem != null)
            {
                existingItem.Title = ErrorItem.Title;
                existingItem.Date = ErrorItem.Date;
                existingItem.Description = ErrorItem.Description;
                existingItem.ImagePaths = ErrorItem.ImagePaths;
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
