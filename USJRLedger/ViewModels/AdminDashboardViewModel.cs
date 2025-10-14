using System.Collections.ObjectModel;
using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private readonly UserService _userService;

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private int _totalOrganizations;
        public int TotalOrganizations
        {
            get => _totalOrganizations;
            set => SetProperty(ref _totalOrganizations, value);
        }

        private int _activeOrganizations;
        public int ActiveOrganizations
        {
            get => _activeOrganizations;
            set => SetProperty(ref _activeOrganizations, value);
        }

        private int _totalAdvisers;
        public int TotalAdvisers
        {
            get => _totalAdvisers;
            set => SetProperty(ref _totalAdvisers, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand CreateOrganizationCommand { get; }
        public ICommand ViewOrganizationsCommand { get; }
        public ICommand ManageAdvisersCommand { get; }

        public AdminDashboardViewModel(AuthService authService, DataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
            _userService = new UserService(dataService);

            WelcomeMessage = $"Welcome, {_authService.CurrentUser.Name}";

            LogoutCommand = new Command(() => _authService.Logout());
            CreateOrganizationCommand = new Command(OnCreateOrganization);
            ViewOrganizationsCommand = new Command(OnViewOrganizations);
            ManageAdvisersCommand = new Command(OnManageAdvisers);

            LoadStatsAsync();
        }

        public async Task LoadStatsAsync()
        {
            var organizations = await _organizationService.GetAllOrganizationsAsync();
            TotalOrganizations = organizations.Count;
            ActiveOrganizations = organizations.Count(o => o.IsActive);

            var advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
            TotalAdvisers = advisers.Count;
        }

        private void OnCreateOrganization()
        {
            // Navigation will be handled in the view
        }

        private void OnViewOrganizations()
        {
            // Navigation will be handled in the view
        }

        private void OnManageAdvisers()
        {
            // Navigation will be handled in the view
        }
    }
}