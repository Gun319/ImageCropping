using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ImageCropping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point startPoint; // Canvas 起始点
        private System.Drawing.Point imageStartPoint; // Image 起始点
        private BitmapImage? bitmapImage;
        private CroppedBitmap? croppedBitmap;
        private bool isDragging = false; // 是否正在拖拽
        private int pixelWidth = 0;
        private int pixelHeight = 0;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        #region Events
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton is MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(MainCanvas);
                var currentImagePoint = e.GetPosition(MainImage);

                imageStartPoint = GetPoint(currentImagePoint.X, currentImagePoint.Y);
                isDragging = true;
            }

            if (e.RightButton is MouseButtonState.Pressed)
            {
                // 清除之前的选框
                ClearSelectionRectangle();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton is MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(MainCanvas);
                var rect = new Rect(startPoint, currentPoint);


                DrawSelectionRectangle(rect);
            }
            else
            {
                if (isDragging && MainImage.ActualWidth is not 0)
                {
                    var currentImagePoint = e.GetPosition(MainImage);
                    FinalizeSelection(GetPoint(currentImagePoint.X, currentImagePoint.Y));
                    isDragging = false;
                }
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                    Multiselect = false
                };
                if (openFileDialog.ShowDialog() is true)
                {
                    ResetObjects();

                    string path = openFileDialog.FileName;
                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    MainImage.Source = bitmapImage;
                    pixelHeight = bitmapImage.PixelHeight;
                    pixelWidth = bitmapImage.PixelWidth;
                }
            }
            catch (Exception ex)
            {
                ExceptionHandling(ex);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (croppedBitmap!.Source is not null)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                   
                };
                if (saveFileDialog.ShowDialog() is true)
                {
                    string filePath = saveFileDialog.FileName;
                    SaveImage(croppedBitmap, filePath);
                }
            }
        }

        #endregion

        #region Private function
        void Init()
        {
            croppedBitmap = new CroppedBitmap();

            Binding widthBinding = new("ActualWidth")
            {
                Source = MainCanvas,
            };
            MainImage.SetBinding(WidthProperty, widthBinding);

            Binding heightBinding = new("ActualHeight")
            {
                Source = MainCanvas
            };
            MainImage.SetBinding(HeightProperty, heightBinding);
        }

        /// <summary>
        /// 绘制选择矩形
        /// </summary>
        /// <param name="rect"></param>
        private void DrawSelectionRectangle(Rect rect)
        {
            ClearSelectionRectangle();

            // 创建选框
            var selectionRect = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Visibility = Visibility.Visible
            };

            Canvas.SetLeft(selectionRect, rect.X);
            Canvas.SetTop(selectionRect, rect.Y);
            selectionRect.Width = Math.Abs(rect.Width);
            selectionRect.Height = Math.Abs(rect.Height);

            MainCanvas.Children.Add(selectionRect);
        }

        /// <summary>
        /// 清除选框
        /// </summary>
        private void ClearSelectionRectangle()
        {
            // 查找并移除选框
            var selectionRect = MainCanvas.Children.OfType<Rectangle>()
                .FirstOrDefault(r => r.Stroke == Brushes.Red && r.Fill == Brushes.Transparent);
            if (selectionRect is not null)
            {
                MainCanvas.Children.Remove(selectionRect);
            }
        }

        /// <summary>
        /// 提取选取内容
        /// </summary>
        /// <param name="point"></param>
        private void FinalizeSelection(System.Drawing.Point point)
        {
            try
            {
                Int32Rect rect = imageStartPoint.X < point.X
                    ? new Int32Rect(imageStartPoint.X, imageStartPoint.Y, point.X - imageStartPoint.X, point.Y - imageStartPoint.Y)
                    : new Int32Rect(point.X, point.Y, imageStartPoint.X - point.X, imageStartPoint.Y - point.Y);
                if (!rect.IsEmpty)
                {
                    croppedBitmap = new(bitmapImage, rect);
                    ResultImage.Source = croppedBitmap;
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 计算像素坐标
        /// </summary>
        /// <param name="pointX">控件坐标 X</param>
        /// <param name="pointY">控件坐标 Y</param>
        /// <returns>像素位置坐标</returns>
        private System.Drawing.Point GetPoint(double pointX, double pointY)
        {

            int pixelX = Convert.ToInt32(pointX * pixelWidth / MainImage.ActualWidth);
            int pixelY = Convert.ToInt32(pointY * pixelHeight / MainImage.ActualHeight);
            return new System.Drawing.Point(pixelX, pixelY);
        }

        /// <summary>
        /// 重置对象
        /// </summary>
        private void ResetObjects()
        {
            ClearSelectionRectangle();
            croppedBitmap = new CroppedBitmap();
            ResultImage.Source = new BitmapImage();
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="bitmapSource">图像数据</param>
        /// <param name="filePath">文件路径</param>
        private static void SaveImage(BitmapSource bitmapSource, string filePath)
        {
            try
            {
                string directoryPath = System.IO.Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
                using var fileStream = new FileStream(filePath, FileMode.Create);
                var encoder = GetEncoder(fileExtension);
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
            catch (Exception ex)
            {
                ExceptionHandling(ex);
            }
        }

        /// <summary>
        /// 获取编码器
        /// </summary>
        /// <param name="fileExtension">文件扩展名</param>
        /// <returns>编码器</returns>
        private static BitmapEncoder GetEncoder(string fileExtension)
        {
            return fileExtension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                ".bmp" => new BmpBitmapEncoder(),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="exception">捕获异常</param>
        private static void ExceptionHandling(Exception exception)
        {
            MessageBox.Show($"{exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion
    }
}