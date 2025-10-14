using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Admin;
using USJRLedger.Views.Adviser;
using USJRLedger.Views.Officer;
using USJRLedger.Views.Common;

namespace USJRLedger.Views.Common
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;

        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                StatusLabel.Text = "Please enter both username and password";
                return;
            }

            bool isLoggedIn = await _authService.LoginAsync(username, password);

            if (isLoggedIn)
            {
                // Check if password change is required
                if (_authService.RequiresPasswordChange())
                {
                    await Navigation.PushAsync(new ChangePasswordPage(_authService));
                    return;
                }

                // Navigate to the appropriate dashboard based on user role
                Page dashboardPage = _authService.CurrentUser.Role switch
                {
                    UserRole.Admin => new AdminDashboardPage(_authService),
                    UserRole.Adviser => new AdviserDashboardPage(_authService),
                    UserRole.Officer => new OfficerDashboardPage(_authService),
                    _ => throw new NotImplementedException("Unknown user role")
                };

                // Replace the navigation stack with the dashboard page
                Application.Current.MainPage = new NavigationPage(dashboardPage);
            }
            else
            {
                StatusLabel.Text = "Invalid username or password";
            }
        }
    }
}