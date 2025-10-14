using System;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Admin
{
    public partial class OrganizationDetailsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private readonly UserService _userService;
        private readonly string _organizationId;
        private Organization _organization;

        public OrganizationDetailsPage(AuthService authService, DataService dataService, string organizationId)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
            _userService = new UserService(dataService);
            _organizationId = organizationId;

            _ = LoadOrganizationDataAsync();
        }

        private async Task LoadOrganizationDataAsync()
        {
            _organization = await _organizationService.GetOrganizationByIdAsync(_organizationId);

            if (_organization != null)
            {
                OrganizationNameLabel.Text = _organization.Name;
                DepartmentLabel.Text = _organization.Department;
                StatusLabel.Text = _organization.IsActive ? "Active" : "Inactive";
                CreatedDateLabel.Text = _organization.CreatedDate.ToString("MMMM dd, yyyy");

                if (!_organization.IsActive && _organization.DeactivationDate.HasValue)
                {
                    DeactivationDateLabel.Text = _organization.DeactivationDate.Value.ToString("MMMM dd, yyyy");
                    DeactivationDateLabel.IsVisible = true;
                }
                else
                {
                    DeactivationDateLabel.IsVisible = false;
                }

                // Load advisers
                var advisers = await _userService.GetUsersByOrganizationAsync(_organizationId);
                var activeAdvisers = advisers.Where(a => a.Role == UserRole.Adviser && a.IsActive).ToList();
                AdvisersCountLabel.Text = activeAdvisers.Count.ToString();
                AdvisersListView.ItemsSource = activeAdvisers;

                // Load financial data
                decimal balance = await _organizationService.GetOrganizationBalanceAsync(_organizationId);
                BalanceLabel.Text = $"₱ {balance:N2}";

                // Toggle button text based on organization status
                string buttonText = _organization.IsActive ? "Deactivate Organization" : "Activate Organization";
                Color buttonColor = _organization.IsActive ? Colors.Red : Colors.Green;

                UpdateStatusButton.Text = buttonText;
                UpdateStatusButton.BackgroundColor = buttonColor;
            }
            else
            {
                await DisplayAlert("Error", "Organization not found", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void OnUpdateStatusClicked(object sender, EventArgs e)
        {
            bool isActive = !_organization.IsActive;
            string action = isActive ? "activate" : "deactivate";
            string newStatus = isActive ? "Active" : "Inactive";

            bool confirm = await DisplayAlert("Confirm",
                $"Are you sure you want to {action} {_organization.Name}?\nStatus will change to: {newStatus}", "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _organizationService.UpdateOrganizationStatusAsync(_organization.Id, isActive);
                    string statusMessage = isActive ? "activated" : "deactivated";
                    await DisplayAlert("Success", $"Organization {statusMessage} successfully", "OK");
                    await LoadOrganizationDataAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update organization: {ex.Message}", "OK");
                }
            }
        }
    }
}