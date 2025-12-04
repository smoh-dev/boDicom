using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace boDicom.WinUI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Stream? _dicomStream;
    private DicomImage? _image;
    private int _currentFrameIndex = 0;
    public ObservableCollection<DicomThumbnailItem> Thumbnails { get; set; } 
        = new ObservableCollection<DicomThumbnailItem>();

    public MainWindow()
    {
        InitializeComponent();

        // HWND 가져오기
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

        // 정적 메서드로 AppWindow 가져오기
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        // 창 크기 설정
        appWindow.Resize(new SizeInt32(1100, 900));
        appWindow.Title = "boDicom";

        //LoadDicomFile();
        //SaveDicomToPng();
        //ShowDicomImage();
        ShowDicomFrame(_currentFrameIndex);

        // Handle mouse wheel for multi-frame DICOM
        DicomViewer.PointerWheelChanged += DicomViewer_PointerWheelChanged;
    }


    private void ShowDicomFrame(int frameIndex)
    {
        if (_image == null)
            return;

        // Render SKBitmap
        SKBitmap bitmap = _image.RenderImage(frameIndex).As<SKBitmap>();

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

    private void DicomViewer_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }

        e.Handled = true;
    }

    private async void DicomViewer_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            return;

        var items = await e.DataView.GetStorageItemsAsync();

        foreach (var item in items)
        {
            if (item is StorageFile file)
            {
                if (!await IsDicomFile(file))
                {
                    await ShowWarningDialog($"{file.Name} is not DICOM file.");
                    continue;
                }
                await LoadDicomToList(file); // Add to image list.
                await LoadDicomFileFromStorage(file); // Open the dropped file.
            }
        }
    }

    private async Task<bool> IsDicomFile(StorageFile file)
    {
        try
        {
            using IRandomAccessStream raStream = await file.OpenReadAsync();
            using var stream = raStream.AsStreamForRead();

            // Try ty to open as DICOM file
            await DicomFile.OpenAsync(stream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadDicomFileFromStorage(StorageFile file)
    {
        try
        {
            _dicomStream?.Dispose();

            var raStream = await file.OpenReadAsync();
            _dicomStream = raStream.AsStreamForRead(); 

            var dicomFile = await DicomFile.OpenAsync(_dicomStream);

            _image = new DicomImage(dicomFile.Dataset);
            _currentFrameIndex = 0;

            ShowDicomFrame(_currentFrameIndex);
        }
        catch (Exception ex)
        {
            await ShowWarningDialog($"Failed to load DICOM file: {ex.Message}");
        }
    }

    private async Task ShowWarningDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Warn",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async Task LoadDicomToList(StorageFile file)
    {
        // 1) placeholder
        var placeholder = new WriteableBitmap(150, 150);

        var item = new DicomThumbnailItem
        {
            FileName = file.Name,
            FilePath = file.Path,
            Thumbnail = placeholder
        };

        Thumbnails.Add(item);

        _ = Task.Run(() =>
        {
            var buffer = CreateGrayBuffer(150, 150); //  Run in background

            DispatcherQueue.TryEnqueue(() =>
            {
                var wb = new WriteableBitmap(150, 150);
                using var stream = wb.PixelBuffer.AsStream();
                stream.Write(buffer);
                item.Thumbnail = wb;
                item.IsLoaded = true;
            });
        });

    }

    private byte[] CreateGrayBuffer(int width, int height)
    {
        byte[] pixel = { 128, 128, 128, 255 };
        byte[] buffer = new byte[width * height * 4];

        for (int i = 0; i < buffer.Length; i += 4)
        {
            buffer[i] = pixel[0];
            buffer[i + 1] = pixel[1];
            buffer[i + 2] = pixel[2];
            buffer[i + 3] = pixel[3];
        }

        return buffer;
    }


    private async void ThumbnailItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe
            && fe.DataContext is DicomThumbnailItem item)
        {
            var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
            await LoadDicomFileFromStorage(file);
        }
    }

    #region Old Methods
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

        // SKBitmap → WriteableBitmap
        using (var stream = wb.PixelBuffer.AsStream())
        {
            // Get pixel data of SKBitmap(BGRA)
            var bytes = bitmap.Bytes; // Format of SKBitmap.Bytes is BGRA_8888
            stream.Write(bytes, 0, bytes.Length);
        }

        // Show image.
        DicomViewer.Source = wb;
    }

    #endregion Old Methods
}
