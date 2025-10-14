using USJRLedger.Services;
using USJRLedger.Views.Common;

namespace USJRLedger
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Initialize services
            var dataService = new DataService();
            var authService = new AuthService(dataService);

            // Set the main page to the login page
            MainPage = new NavigationPage(new LoginPage(authService));
        }
    }
}