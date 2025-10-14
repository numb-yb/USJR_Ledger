using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Admin
{
    public partial class CreateOrganizationPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;

        public CreateOrganizationPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            string name = NameEntry.Text;
            string department = DepartmentEntry.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(department))
            {
                await DisplayAlert("Error", "Please enter both name and department", "OK");
                return;
            }

            try
            {
                await _organizationService.CreateOrganizationAsync(name, department);
                await DisplayAlert("Success", "Organization created successfully", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create organization: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}