using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace boDicom.WinUI;

public class DicomThumbnailItem : INotifyPropertyChanged
{
    private WriteableBitmap? _thumbnail;
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";

    public WriteableBitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (_thumbnail != value)
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }
    }

    private bool _isLoaded = false;
    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (_isLoaded != value)
            {
                _isLoaded = value;
                OnPropertyChanged(nameof(IsLoaded));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}