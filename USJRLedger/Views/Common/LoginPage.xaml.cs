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
            string username = UsernameEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            //  Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                StatusLabel.Text = "Please enter both username and password.";
                return;
            }

            //  Attempt login
            bool isLoggedIn = await _authService.LoginAsync(username, password);

            if (isLoggedIn)
            {
                //  If password change is required (e.g., first-time login)
                if (_authService.RequiresPasswordChange())
                {
                    await Navigation.PushAsync(new ChangePasswordPage(_authService));
                    return;
                }

                //  Navigate based on user role
                Page dashboardPage = _authService.CurrentUser.Role switch
                {
                    UserRole.Admin => new AdminDashboardPage(_authService),
                    UserRole.Adviser => new AdviserDashboardPage(_authService),
                    UserRole.Officer => new OfficerDashboardPage(_authService),
                    _ => null
                };

                if (dashboardPage != null)
                {
                    // Replace the navigation stack (prevents back navigation to login)
                    Application.Current.MainPage = new NavigationPage(dashboardPage);
                }
                else
                {
                    StatusLabel.Text = "User role not recognized.";
                }
            }
            else
            {
                //  Invalid credentials
                StatusLabel.Text = "Invalid username or password.";
            }
        }

        //  Forgot Password link
        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ResetPasswordPage(_authService));
        }
    }
}
