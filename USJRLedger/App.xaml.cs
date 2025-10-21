using USJRLedger.Services;
using USJRLedger.Views.Admin;
using USJRLedger.Views.Adviser;
using USJRLedger.Views.Officer;
using USJRLedger.Views.Common;

namespace USJRLedger
{
    public partial class App : Application
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;

        public App()
        {
            InitializeComponent();

            _dataService = new DataService();
            _authService = new AuthService(_dataService);

            // Temporary page (so MainPage is set)
            MainPage = new NavigationPage(new ContentPage
            {
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            });
        }

        protected override async void OnStart()
        {
            await LoadMainPageAsync();
        }

        private async Task LoadMainPageAsync()
        {
            bool restored = await _authService.RestoreSessionAsync();

            if (restored && _authService.CurrentUser != null)
            {
                Page dashboardPage = _authService.CurrentUser.Role switch
                {
                    Models.UserRole.Admin => new AdminDashboardPage(_authService),
                    Models.UserRole.Adviser => new AdviserDashboardPage(_authService, _dataService),
                    Models.UserRole.Officer => new OfficerDashboardPage(_authService, _dataService),
                    _ => new LoginPage(_authService)
                };

                MainPage = new NavigationPage(dashboardPage);
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage(_authService));
            }
        }
    }
}
