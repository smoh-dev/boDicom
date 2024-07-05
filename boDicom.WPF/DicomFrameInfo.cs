using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace boDicom.WPF
{
    public class DicomFrameInfo
    {
        public int FrameNumber { get; set; }
        public long FrameOffset { get; set; }
        public long FrameSize { get; set; }
    }
}
