using USJRLedger.Services;
using USJRLedger.Views.Common;

namespace USJRLedger
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new SplashPage();
        }
    }
}