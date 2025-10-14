using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new Command(async () => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please enter both username and password";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                bool isSuccess = await _authService.LoginAsync(Username, Password);

                if (isSuccess)
                {
                    // Navigate to next page (this will be handled in the view)
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}