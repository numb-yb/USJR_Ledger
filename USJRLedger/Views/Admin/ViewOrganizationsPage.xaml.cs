using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common; // Ensure this matches your details page namespace

namespace USJRLedger.Views.Admin
{
    public partial class ViewOrganizationsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;

        private bool _isPageLoaded = false;

        public ViewOrganizationsPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _isPageLoaded = true;
            await LoadOrganizationsAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isPageLoaded = false;
        }

        private async Task LoadOrganizationsAsync()
        {
            try
            {
                var organizations = await _organizationService.GetAllOrganizationsAsync();

                // Only update UI if page is still visible to prevent crashes
                if (_isPageLoaded)
                {
                    OrganizationsListView.ItemsSource = organizations;
                }
            }
            catch (Exception ex)
            {
                if (_isPageLoaded)
                    await DisplayAlert("Error", $"Failed to load organizations: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Organization org)
            {
                bool isCurrentlyActive = org.IsActive;
                string action = isCurrentlyActive ? "deactivate" : "activate";

                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to {action} {org.Name}?", "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        // Flip the status
                        await _organizationService.UpdateOrganizationStatusAsync(org.Id, !isCurrentlyActive);
                        await LoadOrganizationsAsync(); // Reload list
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Update failed: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Organization org)
            {
                // Navigate to details page
                await Navigation.PushAsync(new OrganizationProfilePage(_authService, _dataService, org.Id));
            }
        }
    }
}