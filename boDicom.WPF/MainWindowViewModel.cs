using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace boDicom.WPF
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel() 
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            DicomImageGroupBoxHeader = "Dicom Image";
        }


        #region Section: Open File List 
        private ObservableCollection<string> _openFiles = new ObservableCollection<string>();
        public ObservableCollection<string> OpenFiles
        {
            get { return _openFiles; }
            set
            {
                _openFiles = value;
                OnPropertyChanged("OpenFiles");
            }
        }

        public void AddOpenFiles(string fileName)
        {
            if(OpenFiles.IndexOf(fileName) < 0)
                OpenFiles.Add(fileName);
        }
        #endregion Section: Open File List 


        #region Section: Dicom Image
        private string _dicomImageGroupBoxHeader;
        public string DicomImageGroupBoxHeader
        {
            get { return _dicomImageGroupBoxHeader; }
            set 
            {  
                _dicomImageGroupBoxHeader = value;
                OnPropertyChanged("DicomImageGroupBoxHeader");
            }
        }

        private ObservableCollection<DicomFrameInfo> _dicomFrameInfo = new ObservableCollection<DicomFrameInfo>();
        public ObservableCollection<DicomFrameInfo> DicomFrameInfo
        {
            get { return _dicomFrameInfo; }
            set
            {
                _dicomFrameInfo = value;
                OnPropertyChanged("DicomFrameInfo");
            }
        }
        public void CleanDicomFrameInfo()
        {
            DicomFrameInfo.Clear();
        }
        public void AddDicomFrameInfo(DicomFrameInfo frameInfo)
        {
            DicomFrameInfo.Add(frameInfo);
        }
        #endregion Section: Dicom Image


        #region Section: Dicom Tags
        private ObservableCollection<DicomTagInfo> _dicomTags = new ObservableCollection<DicomTagInfo>();
        public ObservableCollection<DicomTagInfo> DicomTags
        {
            get { return _dicomTags; }
            set
            {
                _dicomTags = value;
                OnPropertyChanged("DicomTags");
            }
        }
        public void CleanDicomTagInfo()
        {
            DicomTags.Clear();
        }
        public void AddDicomTagInfo(DicomTagInfo tagInfo)
        {
            DicomTags.Add(tagInfo);
        }
        #endregion Section: Dicom Tags


        #region Keyboard Shortcut Commands
        public string BaseFileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public ICommand OpenFileCommand { get; set; }
        public void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = BaseFileDirectory;
            openFileDialog.Filter = "Dicom files (*.dcm)|*.dcm";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                BaseFileDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                string fileName = openFileDialog.FileName;
                AddOpenFiles(fileName);
            }
        }

        #endregion Keyboard Shortcut Commands


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
