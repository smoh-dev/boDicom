using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace boDicom.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private DicomImage? _image;
        private int _currentFrameIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            LoadDicomFile();
            //SaveDicomToPng();
            //ShowDicomImage();
            ShowDicomFrame(_currentFrameIndex);

            // Handle mouse wheel for multi-frame DICOM
            DicomViewer.PointerWheelChanged += DicomViewer_PointerWheelChanged;
        }

        private void LoadDicomFile()
        {
            //var dicomFilePath = Path.Combine(AppContext.BaseDirectory, "sample.dcm");
            var dicomFilePath = @"D:\sample_us.dcm";
            var dicomFile = DicomFile.OpenAsync(dicomFilePath).Result;

            _image = new DicomImage(dicomFile.Dataset);
        }

        private void SaveDicomToPng()
        {
            if (_image == null)
                return;
            
            var dicomImageOutputPath = Path.Combine(AppContext.BaseDirectory, "output.png");

            using SKBitmap bitmap = _image.RenderImage(0).As<SKBitmap>();
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(dicomImageOutputPath);

            data.SaveTo(stream);
        }

        private void ShowDicomImage()
        {
            if (_image == null)
                return;

            // Render to SKBitmap (BGRA_8888)
            using SKBitmap bitmap = _image.RenderImage(0).As<SKBitmap>();

            // Create WriteableBitmap
            var wb = new WriteableBitmap(bitmap.Width, bitmap.Height);

            // SKBitmap ¡æ WriteableBitmap
            using (var stream = wb.PixelBuffer.AsStream())
            {
                // Get pixel data of SKBitmap(BGRA)
                var bytes = bitmap.Bytes; // Format of SKBitmap.Bytes is BGRA_8888
                stream.Write(bytes, 0, bytes.Length);
            }

            // Show image.
            DicomViewer.Source = wb;
        }

        private void ShowDicomFrame(int frameIndex)
        {
            if (_image == null)
                return;

            // Render SKBitmap
            using SKBitmap bitmap = _image.RenderImage(frameIndex).As<SKBitmap>();

            // Create WriteableBitmap
            var wb = new WriteableBitmap(bitmap.Width, bitmap.Height);

            using (var stream = wb.PixelBuffer.AsStream())
            {
                var bytes = bitmap.Bytes;
                stream.Write(bytes, 0, bytes.Length);
            }

            // Show image.
            DicomViewer.Source = wb;
        }

        private void DicomViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_image == null || _image.NumberOfFrames <= 1)
                return;

            var delta = e.GetCurrentPoint(DicomViewer).Properties.MouseWheelDelta;

            if (delta > 0)
            {
                // Previous frame
                _currentFrameIndex--;
                if (_currentFrameIndex < 0)
                    _currentFrameIndex = _image.NumberOfFrames - 1;
            }
            else if (delta < 0)
            {
                // Next frame
                _currentFrameIndex++;
                if (_currentFrameIndex >= _image.NumberOfFrames)
                    _currentFrameIndex = 0;
            }

            ShowDicomFrame(_currentFrameIndex);
        }
    }
}
