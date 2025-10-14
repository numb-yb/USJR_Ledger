using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Admin
{
    public partial class ViewOrganizationsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private List<Organization> _organizations;
        private bool _isActive; //  Track if page is visible

        public ViewOrganizationsPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isActive = true; //  Mark as active
            _ = LoadOrganizationsAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isActive = false; // Prevent further UI updates
        }

        private async Task LoadOrganizationsAsync()
        {
            try
            {
                var organizations = await _organizationService.GetAllOrganizationsAsync();

                if (!_isActive)
                    return; //  Page is gone, stop safely

                _organizations = organizations;
                OrganizationsListView.ItemsSource = _organizations;
            }
            catch (Exception ex)
            {
                if (_isActive)
                    await DisplayAlert("Error", $"Failed to load organizations: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateStatusClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var org = button?.BindingContext as Organization;

            if (org == null)
                return;

            bool isActive = !org.IsActive;
            string action = isActive ? "activate" : "deactivate";
            string newStatus = isActive ? "Active" : "Inactive";

            bool confirm = await DisplayAlert("Confirm",
                $"Are you sure you want to {action} {org.Name}?\nStatus will change to: {newStatus}", "Yes", "No");

            if (!confirm)
                return;

            try
            {
                await _organizationService.UpdateOrganizationStatusAsync(org.Id, isActive);
                string statusMessage = isActive ? "activated" : "deactivated";
                await DisplayAlert("Success", $"Organization {statusMessage} successfully", "OK");

                if (_isActive)
                    await LoadOrganizationsAsync(); // Only reload if still active
            }
            catch (Exception ex)
            {
                if (_isActive)
                    await DisplayAlert("Error", $"Failed to update organization: {ex.Message}", "OK");
            }
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var org = button?.BindingContext as Organization;

            if (org != null && _isActive)
            {
                await Navigation.PushAsync(new OrganizationDetailsPage(_authService, _dataService, org.Id));
            }
        }
    }
}
