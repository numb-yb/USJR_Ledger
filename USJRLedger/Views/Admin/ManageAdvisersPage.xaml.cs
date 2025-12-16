using USJRLedger.Models;
using USJRLedger.Services;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace USJRLedger.Views.Admin
{
    public partial class ManageAdvisersPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly UserService _userService;

        // Track the adviser being edited
        private User _editingAdviser = null;
        private List<Organization> _organizations; // Cache orgs for easy lookup

        public ManageAdvisersPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _userService = new UserService(dataService);

            _ = InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            await LoadOrganizationsAsync();
            await LoadAdvisersAsync();
        }

        private async Task LoadOrganizationsAsync()
        {
            try
            {
                _organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                OrganizationPicker.ItemsSource = _organizations;
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
                AdvisersCollectionView.ItemsSource = advisers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load advisers: {ex.Message}", "OK");
            }
        }

        // 1. Handle Save (Create OR Update)
        private async void OnSaveAdviserClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                OrganizationPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please fill all fields and select an organization.", "OK");
                return;
            }

            var selectedOrg = (Organization)OrganizationPicker.SelectedItem;

            try
            {
                if (_editingAdviser == null)
                {
                    // --- CREATE NEW ADVISER ---
                    if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
                    {
                        await DisplayAlert("Error", "Password is required for new advisers.", "OK");
                        return;
                    }

                    await _userService.CreateAdviserAsync(
                        NameEntry.Text,
                        UsernameEntry.Text,
                        PasswordEntry.Text,
                        selectedOrg.Id);

                    await DisplayAlert("Success", "Adviser added successfully!", "OK");
                }
                else
                {
                    // --- UPDATE EXISTING ADVISER ---
                    _editingAdviser.Name = NameEntry.Text;
                    _editingAdviser.Username = UsernameEntry.Text;
                    _editingAdviser.OrganizationId = selectedOrg.Id;

                    // Only update password if user typed something
                    if (!string.IsNullOrWhiteSpace(PasswordEntry.Text))
                    {
                        _editingAdviser.Password = PasswordEntry.Text;
                    }

                    await _userService.UpdateUserAsync(_editingAdviser);
                    await DisplayAlert("Success", "Adviser updated successfully!", "OK");
                }

                ResetForm();
                await LoadAdvisersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Operation failed: {ex.Message}", "OK");
            }
        }

        // 2. Handle Edit Click
        private void OnEditAdviserClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is User adviser)
            {
                _editingAdviser = adviser;

                // Populate Form
                NameEntry.Text = adviser.Name;
                UsernameEntry.Text = adviser.Username;

                // Select their organization
                if (_organizations != null)
                {
                    var org = _organizations.FirstOrDefault(o => o.Id == adviser.OrganizationId);
                    OrganizationPicker.SelectedItem = org;
                }

                // Setup Password Field
                PasswordEntry.Text = "";
                PasswordEntry.Placeholder = "Enter new password to change";

                // Update UI State
                FormTitleLabel.Text = "Edit Adviser Details";
                SaveButton.Text = "Update Adviser";
                CancelButton.IsVisible = true;

                // Scroll up (Optional)
                // this.Content.ScrollToAsync(0, 0, true);
            }
        }

        // 3. Handle Cancel Click
        private void OnCancelEditClicked(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            _editingAdviser = null;
            NameEntry.Text = string.Empty;
            UsernameEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            PasswordEntry.Placeholder = "Enter temporary password";
            OrganizationPicker.SelectedItem = null;
            ClearOrganizationButton.IsVisible = false;

            // Reset UI State
            FormTitleLabel.Text = "Add New Adviser";
            SaveButton.Text = "Add Adviser";
            CancelButton.IsVisible = false;
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            var adviser = (sender as Button)?.BindingContext as User;
            if (adviser == null) return;

            bool newStatus = !adviser.IsActive;
            string action = newStatus ? "activate" : "deactivate";

            if (!await DisplayAlert("Confirm", $"Are you sure you want to {action} {adviser.Name}?", "Yes", "No"))
                return;

            try
            {
                await _userService.UpdateUserStatusAsync(adviser.Id, newStatus);
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
            var adviser = (sender as Button)?.BindingContext as User;
            if (adviser == null) return;

            if (!await DisplayAlert("Confirm Delete", $"Delete adviser {adviser.Name}?", "Yes", "No")) return;

            try
            {
                await _userService.DeleteUserAsync(adviser.Id);
                await DisplayAlert("Deleted", "Adviser removed successfully!", "OK");
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