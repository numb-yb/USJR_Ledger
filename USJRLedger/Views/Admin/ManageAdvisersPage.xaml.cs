using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Admin
{
    public partial class ManageAdvisersPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly UserService _userService;

        public ManageAdvisersPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _userService = new UserService(dataService);

            LoadOrganizationsAsync();
            LoadAdvisersAsync();
        }

        private async Task LoadOrganizationsAsync()
        {
            try
            {
                var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                OrganizationPicker.ItemsSource = organizations;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load organizations: {ex.Message}", "OK");
            }
        }

        private async Task LoadAdvisersAsync()
        {
            try
            {
                var advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
                AdvisersListView.ItemsSource = advisers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load advisers: {ex.Message}", "OK");
            }
        }

        private async void OnAddAdviserClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                OrganizationPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please fill all fields and select an organization.", "OK");
                return;
            }

            try
            {
                var organization = (Organization)OrganizationPicker.SelectedItem;
                await _userService.CreateAdviserAsync(
                    NameEntry.Text,
                    UsernameEntry.Text,
                    PasswordEntry.Text,
                    organization.Id);

                await DisplayAlert("Success", "Adviser added successfully!", "OK");

                // Clear inputs
                NameEntry.Text = UsernameEntry.Text = PasswordEntry.Text = string.Empty;
                OrganizationPicker.SelectedItem = null;
                ClearOrganizationButton.IsVisible = false;

                await LoadAdvisersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add adviser: {ex.Message}", "OK");
            }
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var adviser = button?.BindingContext as User;

            if (adviser == null) return;

            bool isActive = !adviser.IsActive;
            string action = isActive ? "activate" : "deactivate";

            bool confirm = await DisplayAlert("Confirm Action",
                $"Are you sure you want to {action} {adviser.Name}?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _userService.UpdateUserStatusAsync(adviser.Id, isActive);
                await DisplayAlert("Success", $"Adviser {action}d successfully!", "OK");
                await LoadAdvisersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update adviser: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteAdviserClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var adviser = button?.BindingContext as User;

            if (adviser == null) return;

            bool confirm = await DisplayAlert("Confirm Delete",
                $"Are you sure you want to delete adviser {adviser.Name}?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _userService.DeleteUserAsync(adviser.Id);
                await DisplayAlert("Success", "Adviser deleted successfully!", "OK");
                await LoadAdvisersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete adviser: {ex.Message}", "OK");
            }
        }

        private void OnOrganizationSelected(object sender, EventArgs e)
        {
            ClearOrganizationButton.IsVisible = OrganizationPicker.SelectedItem != null;
        }

        private void OnClearOrganizationClicked(object sender, EventArgs e)
        {
            OrganizationPicker.SelectedItem = null;
            ClearOrganizationButton.IsVisible = false;
        }
    }
}
