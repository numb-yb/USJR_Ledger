using USJRLedger.Models;
using USJRLedger.Services;
using Microsoft.Maui.Controls;

namespace USJRLedger.Views.Adviser
{
    public partial class ManageOfficersPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly UserService _userService;
        private readonly string _organizationId;
        private List<User> _officers;

        public ManageOfficersPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _userService = new UserService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOfficersAsync();
        }

        private async Task LoadOfficersAsync()
        {
            try
            {
                var allUsers = await _userService.GetUsersByOrganizationAsync(_organizationId);
                _officers = allUsers.Where(u => u.Role == UserRole.Officer).ToList();
                OfficersListView.ItemsSource = _officers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load officers: {ex.Message}", "OK");
            }
        }

        private async void OnAddOfficerClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(StudentIdEntry.Text) ||
                string.IsNullOrWhiteSpace(PositionEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            try
            {
                await _userService.CreateOfficerAsync(
                    NameEntry.Text,
                    StudentIdEntry.Text,
                    PasswordEntry.Text,
                    _organizationId,
                    PositionEntry.Text);

                await DisplayAlert("Success", "Officer added successfully!", "OK");

                // Notify dashboard that officers changed
                MessagingCenter.Send(this, "OfficersChanged");

                // Clear fields
                NameEntry.Text = string.Empty;
                StudentIdEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                PositionEntry.Text = string.Empty;

                await LoadOfficersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add officer: {ex.Message}", "OK");
            }
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User officer)
            {
                bool newStatus = !officer.IsActive;
                string action = newStatus ? "activate" : "deactivate";

                bool confirm = await DisplayAlert("Confirm Action",
                    $"Are you sure you want to {action} {officer.Name}?",
                    "Yes", "No");

                if (!confirm)
                    return;

                try
                {
                    await _userService.UpdateUserStatusAsync(officer.Id, newStatus);
                    await DisplayAlert("Success", $"Officer {action}d successfully!", "OK");

                    // Notify dashboard in case totals or roles change
                    MessagingCenter.Send(this, "OfficersChanged");

                    await LoadOfficersAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update officer: {ex.Message}", "OK");
                }
            }
        }

        private async void OnDeleteOfficerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User officer)
            {
                bool confirm = await DisplayAlert("Confirm Deletion",
                    $"Are you sure you want to delete officer {officer.Name}?",
                    "Yes", "No");

                if (!confirm)
                    return;

                try
                {
                    await _userService.DeleteUserAsync(officer.Id);
                    await DisplayAlert("Success", "Officer deleted successfully!", "OK");

                    //Notify dashboard so it refreshes safely
                    MessagingCenter.Send(this, "OfficersChanged");

                    await LoadOfficersAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete officer: {ex.Message}", "OK");
                }
            }
        }
    }
}
