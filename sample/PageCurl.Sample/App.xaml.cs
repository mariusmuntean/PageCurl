using Xamarin.Forms;

namespace PageCurl.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // MainPage = new ExperimentalPage();
            MainPage = new NavigationPage(new HomePage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}