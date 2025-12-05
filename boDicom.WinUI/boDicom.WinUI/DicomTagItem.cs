using System.Collections.ObjectModel;

namespace boDicom.WinUI;

public class DicomTagItem
{
    public string Display { get; set; } = "";
    public ObservableCollection<DicomTagItem> Children { get; set; } = new();
}
