using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.NativeCodec;
using FellowOakDicom.Imaging.Render;
using FellowOakDicom.IO.Buffer;
using Serilog;
using Serilog.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace boDicom.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo currentOpenedFileInfo;
        private DicomFile currentOpenedDicomFile;
        private DicomImage currentOpenedDicomImage;
        private DataGridRow currentSelectedDicomTag;
        private string dicomTagTableEventCaller;

        private MainWindowViewModel mainWindowViewModel;

        public MainWindow()
        {
            // Initialize logger.
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.File("logs/bo-dicom-.log", rollingInterval: RollingInterval.Day
                    , outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            // Initialize variables.
            currentOpenedDicomFile = null;
            currentOpenedDicomImage = null;
            currentSelectedDicomTag = null;
            dicomTagTableEventCaller = "";

            // Register ImageManager for fo-dicom.
            new DicomSetupBuilder()
                   .RegisterServices(s =>
                        s.AddFellowOakDicom()
                          .AddImageManager<ImageSharpImageManager>()
                          .AddTranscoderManager<FellowOakDicom.Imaging.NativeCodec.NativeTranscoderManager>())
                   .SkipValidation()
                   .Build();

            // Initialize view model.
            mainWindowViewModel = new MainWindowViewModel();
            DataContext = mainWindowViewModel;

            // Initialize component.
            InitializeComponent();

            Log.Information("boDicom started.");
        }

        /// <summary>
        /// Check the transfer syntax is J2K.
        /// </summary>
        /// <param name="xfer"></param>
        /// <returns></returns>
        private bool IsJ2k(DicomTransferSyntax xfer)
        {
            return xfer == DicomTransferSyntax.JPEG2000Lossless || xfer == DicomTransferSyntax.JPEG2000Lossy
                || xfer == DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly || xfer == DicomTransferSyntax.JPEG2000Part2MultiComponent;
        }

        /// <summary>
        /// Convert SixLabors.ImageSharp.Image to BitmapImage
        /// </summary>
        /// <param name="shartpImage"></param>
        /// <returns></returns>
        private BitmapImage ConvertSharpImageToBitmapImage(SixLabors.ImageSharp.Image shartpImage)
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream())
            {
                shartpImage.Save(ms, new PngEncoder());
                ms.Seek(0, SeekOrigin.Begin);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            return bitmap;
        }
        /// <summary>
        /// Load frames and set to MainWindowViewModel.DicomFrameInfo
        /// </summary>
        /// <param name="dicomItems"></param>
        private void SetFrameData(DicomDataset dicomItems)
        {
            if (IsJ2k(dicomItems.InternalTransferSyntax))
            {
                DicomItem pixelItem = dicomItems.Last();
                int frameNumber = 1;
                foreach (FileByteBuffer fragment in ((DicomFragmentSequence)pixelItem).Fragments)
                {
                    mainWindowViewModel.AddDicomFrameInfo(new DicomFrameInfo()
                    {
                        FrameNumber = frameNumber,
                        FrameOffset = fragment.Position,
                        FrameSize = fragment.Size,
                    });
                    frameNumber++;
                    if (dicomItems.GetSingleValue<string>(DicomTag.Modality).ToUpper() == "SM")
                        break;
                }
            }
            else
            {
                DicomPixelData pixelData = DicomPixelData.Create(dicomItems);
                DicomElement pixelElem = (DicomElement)dicomItems.Last();
                long frameOffset = currentOpenedFileInfo.Length - pixelElem.Length;
                long beforeFrameSize = 0;
                for (int i = 0; i < pixelData.NumberOfFrames; i++)
                {
                    frameOffset += beforeFrameSize;
                    //RangeByteBuffer currentFrame = (RangeByteBuffer)pixelData.GetFrame(i);
                    mainWindowViewModel.AddDicomFrameInfo(new DicomFrameInfo()
                    {
                        FrameNumber = i + 1,
                        FrameOffset = frameOffset,
                        FrameSize = pixelData.GetFrame(i).Data.LongLength,
                    });
                    beforeFrameSize = pixelData.GetFrame(i).Data.LongLength;
                    if (dicomItems.GetSingleValue<string>(DicomTag.Modality).ToUpper() == "SM")
                        break;
                }
            }
        }


        #region Event handlers: Menu
        private void Click_Menu_FileOpen(object sender, RoutedEventArgs e)
        {
            mainWindowViewModel.OpenFile();
            ChangeOpenedFile();
        }
        /// <summary>
        /// After open file, change selected item to last item.
        /// </summary>
        private void ChangeOpenedFile()
        {
            int count = OpenedFileList.Items.Count;
            OpenedFileList.SelectedItem = OpenedFileList.Items[count - 1];
        }

        private void Click_Menu_About(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
        #endregion Event handlers: Menu


        #region Event handlers: Open files
        /// <summary>
        /// Load dicom file and show dicom image, frames, and tags.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Change_ListBox_OpenFiles(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            if (listBox.SelectedItem != null)
            {
                Stopwatch stopwatch = new Stopwatch();

                // Open dicom file
                stopwatch.Start();
                currentOpenedDicomFile = DicomFile.Open(listBox.SelectedItem.ToString());
                currentOpenedFileInfo = new FileInfo(listBox.SelectedItem.ToString());
                currentOpenedDicomImage = new DicomImage(currentOpenedDicomFile.Dataset);
                stopwatch.Stop();
                Log.Debug("Open file {file_name}({duration}ms).", currentOpenedFileInfo.Name, stopwatch.ElapsedMilliseconds);

                // Load dicom tags
                stopwatch.Start();
                DicomTransferSyntax xfer = currentOpenedDicomFile.FileMetaInfo.TransferSyntax;
                mainWindowViewModel.CleanDicomTagInfo();
                foreach (var item in currentOpenedDicomFile.Dataset)
                {
                    DicomTagInfo dicomTagInfo;
                    if (item.ValueRepresentation.Code == "SQ")
                        dicomTagInfo = new DicomTagInfo((DicomSequence)item);
                    else if (item.Tag == DicomTag.PixelData)
                        dicomTagInfo = new DicomTagInfo(item.Tag, item.ValueRepresentation, "");
                    else
                        dicomTagInfo = new DicomTagInfo((DicomElement)item);
                    mainWindowViewModel.AddDicomTagInfo(dicomTagInfo);
                }
                stopwatch.Stop();
                Log.Debug("Load tags({duration}ms).", stopwatch.ElapsedMilliseconds);

                // Load frames
                stopwatch.Start();
                mainWindowViewModel.DicomImageGroupBoxHeader = currentOpenedFileInfo.Name;
                mainWindowViewModel.CleanDicomFrameInfo();
                if (currentOpenedDicomFile.Dataset.Contains(DicomTag.PixelData))
                    SetFrameData(currentOpenedDicomFile.Dataset);
                stopwatch.Stop();
                Log.Debug("Load frames({duration}ms).", stopwatch.ElapsedMilliseconds);

                // Show image.
                stopwatch.Start();
                ImageArea.Source = ConvertSharpImageToBitmapImage(currentOpenedDicomImage.RenderImage(0).AsSharpImage());
                stopwatch.Stop();
                Log.Debug("Show image({duration}ms).", stopwatch.ElapsedMilliseconds);
            }
        }
        #endregion Event handlers: Open files


        #region Event handlers: Frames table
        /// <summary>
        /// Show frame image of selected frame.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Change_DataGrid_FrameInfo(object sender, SelectionChangedEventArgs e)
        {
            DicomFrameInfo row = ((DataGrid)sender).SelectedItem as DicomFrameInfo;
            if (row != null)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                ImageArea.Source = ConvertSharpImageToBitmapImage(currentOpenedDicomImage.RenderImage(row.FrameNumber - 1).AsSharpImage());
                stopwatch.Stop();
                Log.Debug("Show frame image of {frame_number}({duration}ms).", row.FrameNumber, stopwatch.ElapsedMilliseconds);
            }
        }
        #endregion Event handlers: Frames table


        #region Event handlers: Tags table
        /// <summary>
        /// Collapse sub grid when click the parent row again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Click_DataGrid_Tags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            if (dataGrid.Name == "TagsTable" && dicomTagTableEventCaller != "TagsTable")
            {
                dicomTagTableEventCaller = "TagsTable";
                return;
            }
            else if (dataGrid.Name != "TagsTable")
            {
                return;
            }

            if (dataGrid.SelectedItems.Count > 0)
            {
                DataGridRow selectedRow = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(dataGrid.SelectedItem);

                if (currentSelectedDicomTag == null)
                {
                    currentSelectedDicomTag = selectedRow;
                }
                else
                {
                    if (selectedRow == currentSelectedDicomTag)
                    {
                        selectedRow.IsSelected = false;
                        currentSelectedDicomTag = null;
                    }
                    else
                    {
                        currentSelectedDicomTag = selectedRow;
                    }
                }
            }
        }
        private void Click_DataGrid_SequenceItems(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            dicomTagTableEventCaller = dataGrid.Name;
        }
        private void Click_DataGrid_SequenceTags(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            dicomTagTableEventCaller = dataGrid.Name;
        }
        #endregion Event handlers: Tags table
    }
}
