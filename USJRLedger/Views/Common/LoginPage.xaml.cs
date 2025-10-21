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
        private readonly DataService _dataService;

        public LoginPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = new DataService();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                StatusLabel.Text = "Please enter both username and password.";
                return;
            }

            bool isLoggedIn = await _authService.LoginAsync(username, password);

            if (isLoggedIn)
            {
                if (_authService.RequiresPasswordChange())
                {
                    await Navigation.PushAsync(new ChangePasswordPage(_authService));
                    return;
                }


                Page dashboardPage = _authService.CurrentUser.Role switch
                {
                    UserRole.Admin => new AdminDashboardPage(_authService),
                    UserRole.Adviser => new AdviserDashboardPage(_authService, _dataService),
                    UserRole.Officer => new OfficerDashboardPage(_authService, _dataService),
                    _ => null
                };

                if (dashboardPage != null)
                {
                    Application.Current.MainPage = new NavigationPage(dashboardPage);
                }
                else
                {
                    StatusLabel.Text = "User role not recognized.";
                }
            }
            else
            {
                StatusLabel.Text = "Invalid username or password.";
            }
        }

        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ResetPasswordPage(_authService));
        }

        private bool _isPasswordVisible = false;

        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            PasswordEntry.IsPassword = !_isPasswordVisible;
            TogglePasswordButton.Source = _isPasswordVisible ? "eye_open.png" : "eye_closed.png";
        }

    }
}
