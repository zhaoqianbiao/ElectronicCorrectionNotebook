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
        //创建错题对象
        public ErrorItem ErrorItem { get; set; }
        private readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronicCorrectionNotebook");

        // 初始化页面
        public ErrorDetailPage()
        {
            this.InitializeComponent();
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
                DisplayImages();
            }
        }

        // 选择图片按钮点击事件
        private async void OnSelectImageClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // 获取当前窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                // 将图片复制到本地存储
                var imagesFolder = Path.Combine(appDataPath, "Images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                foreach (var file in files)
                {
                    var destinationPath = Path.Combine(imagesFolder, file.Name);
                    await file.CopyAsync(await StorageFolder.GetFolderFromPathAsync(imagesFolder), file.Name, NameCollisionOption.ReplaceExisting);
                    ErrorItem.ImagePaths.Add(destinationPath);
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

        // 点击图片放大
        private async void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                DialogImage.Source = image.Source;
                DialogImage.Tag = ((BitmapImage)image.Source).UriSource.LocalPath; // 将图片路径存储在 Tag 中
                await ImageDialog.ShowAsync();
            }
        }

        // 在系统查看器中打开图片
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

        // 点击保存数据
        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            await SaveCurrentContentAsync();
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

            // 传递数据给DataService 保存
            var errorItems = await DataService.LoadDataAsync();
            var existingItem = errorItems.FirstOrDefault(item => item.Id == ErrorItem.Id);
            if (existingItem != null)
            {
                existingItem.Title = ErrorItem.Title;
                existingItem.Date = ErrorItem.Date;
                existingItem.Description = ErrorItem.Description;
                existingItem.ImagePaths = ErrorItem.ImagePaths;
                existingItem.Rating = ErrorItem.Rating;
            }
            else
            {
                errorItems.Add(ErrorItem);
            }
            await DataService.SaveDataAsync(errorItems);
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
    }
}
