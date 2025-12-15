using USJRLedger.Models;
using USJRLedger.Services;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace USJRLedger.Views.Adviser
{
    public partial class ManageOfficersPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly UserService _userService;
        private readonly string _organizationId;
        private List<User> _officers;

        // Track the officer being edited (null means we are in "Add Mode")
        private User _editingOfficer = null;

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

        // 1. Handle "Save" (Add or Update)
        private async void OnSaveOfficerClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(StudentIdEntry.Text) ||
                string.IsNullOrWhiteSpace(PositionEntry.Text))
            {
                await DisplayAlert("Error", "Please fill in Name, ID, and Position.", "OK");
                return;
            }

            try
            {
                if (_editingOfficer == null)
                {
                    // --- CREATE NEW OFFICER ---
                    if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
                    {
                        await DisplayAlert("Error", "Password is required for new officers.", "OK");
                        return;
                    }

                    await _userService.CreateOfficerAsync(
                        NameEntry.Text,
                        StudentIdEntry.Text,
                        PasswordEntry.Text,
                        _organizationId,
                        PositionEntry.Text);

                    await DisplayAlert("Success", "Officer added successfully!", "OK");
                }
                else
                {
                    // --- UPDATE EXISTING OFFICER ---
                    _editingOfficer.Name = NameEntry.Text;
                    _editingOfficer.Username = StudentIdEntry.Text;
                    _editingOfficer.Position = PositionEntry.Text; // Ensure User model has Position property

                    // Only update password if user typed something
                    if (!string.IsNullOrWhiteSpace(PasswordEntry.Text))
                    {
                        _editingOfficer.Password = PasswordEntry.Text;
                    }

                    await _userService.UpdateUserAsync(_editingOfficer);
                    await DisplayAlert("Success", "Officer updated successfully!", "OK");
                }

                // Notify dashboard
                MessagingCenter.Send(this, "OfficersChanged");

                // Reset UI
                ResetForm();
                await LoadOfficersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Operation failed: {ex.Message}", "OK");
            }
        }

        // 2. Handle "Edit" Click
        private void OnEditOfficerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User officer)
            {
                _editingOfficer = officer;

                // Populate Form
                NameEntry.Text = officer.Name;
                StudentIdEntry.Text = officer.Username;
                PositionEntry.Text = officer.Position; // Or RoleDescription if Position property doesn't exist
                PasswordEntry.Text = ""; // Clear password field for security
                PasswordEntry.Placeholder = "Enter new password to change";

                // Update UI to "Edit Mode"
                FormTitleLabel.Text = "Edit Officer";
                SaveButton.Text = "Update Officer";
                SaveButton.BackgroundColor = Colors.Green;
                CancelButton.IsVisible = true;

                // Scroll to top to see the form
                // (Optional: this.Content.ScrollToAsync(0, 0, true));
            }
        }

        // 3. Handle "Cancel" Click
        private void OnCancelEditClicked(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            _editingOfficer = null;
            NameEntry.Text = string.Empty;
            StudentIdEntry.Text = string.Empty;
            PositionEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            PasswordEntry.Placeholder = "Enter temporary password";

            // Reset UI to "Add Mode"
            FormTitleLabel.Text = "Add New Officer";
            SaveButton.Text = "Add Officer";
            SaveButton.BackgroundColor = Color.FromArgb("#28a745"); // Green
            CancelButton.IsVisible = false;
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User officer)
            {
                bool newStatus = !officer.IsActive;
                string action = newStatus ? "activate" : "deactivate";

                if (await DisplayAlert("Confirm", $"Are you sure you want to {action} {officer.Name}?", "Yes", "No"))
                {
                    await _userService.UpdateUserStatusAsync(officer.Id, newStatus);
                    MessagingCenter.Send(this, "OfficersChanged");
                    await LoadOfficersAsync();
                }
            }
        }

        private async void OnDeleteOfficerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User officer)
            {
                if (await DisplayAlert("Delete", $"Delete officer {officer.Name}?", "Yes", "No"))
                {
                    await _userService.DeleteUserAsync(officer.Id);
                    MessagingCenter.Send(this, "OfficersChanged");
                    await LoadOfficersAsync();
                }
            }
        }
    }
}