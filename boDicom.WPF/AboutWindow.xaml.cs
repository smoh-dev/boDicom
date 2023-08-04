using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace boDicom.WPF
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            loadLogoImage();
        }
        private void loadLogoImage()
        {
            BitmapImage logoImage = new BitmapImage(new Uri("./boDicom.png", UriKind.Relative));
            var dx = logoImage.DpiX; // Explicitly calling the value so that it can be used immediately. 
            LogoImage.Source = logoImage;
        }
    }
}
