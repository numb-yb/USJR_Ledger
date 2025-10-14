using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class CreateOrganizationViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly OrganizationService _organizationService;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _department;
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private Color _statusColor;
        public Color StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public CreateOrganizationViewModel(AuthService authService, DataService dataService)
        {
            _authService = authService;
            _organizationService = new OrganizationService(dataService);

            CreateCommand = new Command(async () => await CreateOrganizationAsync());
            CancelCommand = new Command(OnCancel);
        }

        private async Task CreateOrganizationAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Department))
            {
                StatusMessage = "Please enter both name and department";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;

            try
            {
                await _organizationService.CreateOrganizationAsync(Name, Department);

                StatusMessage = "Organization created successfully";
                StatusColor = Colors.Green;

                // Clear the form
                Name = string.Empty;
                Department = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating organization: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnCancel()
        {
            // Navigation will be handled in the view
        }
    }
}