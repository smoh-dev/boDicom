using boDicom.MAUI.Models;
using boDicom.MAUI.PageModels;

namespace boDicom.MAUI.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}