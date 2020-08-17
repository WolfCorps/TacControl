using System.ComponentModel;
using Xamarin.Forms;
using TacControl.ViewModels;

namespace TacControl.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}