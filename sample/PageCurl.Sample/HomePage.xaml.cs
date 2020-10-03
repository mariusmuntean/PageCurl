using System;
using PageCurl.Sample.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PageCurl.Sample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void Demo1_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }
    }
}