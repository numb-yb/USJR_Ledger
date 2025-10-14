using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Admin;
using USJRLedger.Views.Adviser;
using USJRLedger.Views.Officer;

namespace USJRLedger.Views.Common
{
    public partial class ChangePasswordPage : ContentPage
    {
        private readonly AuthService _authService;

        public ChangePasswordPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            string newPassword = NewPasswordEntry.Text;
            string confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                StatusLabel.Text = "Please enter both fields";
                StatusLabel.TextColor = Colors.Red;
                return;
            }

            if (newPassword != confirmPassword)
            {
                StatusLabel.Text = "Passwords do not match";
                StatusLabel.TextColor = Colors.Red;
                return;
            }

            await _authService.ChangePasswordAsync(newPassword);
            StatusLabel.Text = "Password changed successfully";
            StatusLabel.TextColor = Colors.Green;

            // Navigate to the appropriate dashboard based on user role
            await Task.Delay(1000); // Let the user see the success message

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
    }
}