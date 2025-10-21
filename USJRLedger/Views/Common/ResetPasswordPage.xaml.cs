using USJRLedger.Services;

namespace USJRLedger.Views.Common
{
    public partial class ResetPasswordPage : ContentPage
    {
        private readonly AuthService _authService;

        public ResetPasswordPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim();
            string newPassword = NewPasswordEntry.Text;
            string confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                StatusLabel.Text = "Please fill in all fields.";
                StatusLabel.TextColor = Colors.Red;
                return;
            }

            if (newPassword != confirmPassword)
            {
                StatusLabel.Text = "Passwords do not match.";
                StatusLabel.TextColor = Colors.Red;
                return;
            }

            bool success = await _authService.ResetPasswordAsync(username, newPassword);

            if (success)
            {
                StatusLabel.TextColor = Colors.Green;
                StatusLabel.Text = "Password successfully reset.";
                await Task.Delay(1500);
                await Navigation.PopAsync(); //  Go back to Login page
            }
            else
            {
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "User not found or cannot reset password.";
            }
        }


        private async void OnBackToLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Go back to LoginPage
        }

    }
}
