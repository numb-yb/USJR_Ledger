using USJRLedger.Models;
using USJRLedger.Services;
using Microsoft.Maui.Dispatching;

namespace USJRLedger.Views.Admin
{
    public partial class ManageAdvisersPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly UserService _userService;
        private readonly OrganizationService _organizationService;
        private List<User> _advisers;
        private List<Organization> _organizations;

        public ManageAdvisersPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();

            _authService = authService;
            _dataService = dataService;
            _userService = new UserService(dataService);
            _organizationService = new OrganizationService(dataService);

            OrganizationPicker.SelectedIndexChanged += (s, e) =>
            {
                ClearOrganizationButton.IsVisible = OrganizationPicker.SelectedItem != null;
            };

            LoadDataAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            try
            {
                _organizations = await _organizationService.GetAllOrganizationsAsync();
                _organizations = _organizations.Where(o => o.IsActive).ToList();

                _advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);

                Dispatcher.Dispatch(() =>
                {
                    OrganizationPicker.ItemsSource = _organizations;
                    OrganizationPicker.ItemDisplayBinding = new Binding("Name");
                    AdvisersCollectionView.ItemsSource = _advisers;
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async Task RefreshAdvisersAsync()
        {
            _advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
            Dispatcher.Dispatch(() =>
            {
                AdvisersCollectionView.ItemsSource = null;
                AdvisersCollectionView.ItemsSource = _advisers;
            });
        }

        private async void OnAddAdviserClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                OrganizationPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please fill all fields", "OK");
                return;
            }

            var org = OrganizationPicker.SelectedItem as Organization;

            try
            {
                await _userService.CreateAdviserAsync(NameEntry.Text, UsernameEntry.Text, PasswordEntry.Text, org.Id);
                await DisplayAlert("Success", "Adviser added successfully", "OK");

                NameEntry.Text = UsernameEntry.Text = PasswordEntry.Text = string.Empty;
                OrganizationPicker.SelectedItem = null;
                ClearOrganizationButton.IsVisible = false;

                await RefreshAdvisersAsync();
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

            bool confirm = await DisplayAlert("Confirm",
                $"{(adviser.IsActive ? "Deactivate" : "Activate")} {adviser.Name}?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _userService.UpdateUserStatusAsync(adviser.Id, !adviser.IsActive);
                await RefreshAdvisersAsync();
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
                $"Are you sure you want to permanently delete adviser {adviser.Name}?",
                "Delete", "Cancel");

            if (!confirm) return;

            try
            {
                await _userService.DeleteUserAsync(adviser.Id);
                await DisplayAlert("Deleted", $"{adviser.Name} has been removed.", "OK");
                await RefreshAdvisersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete adviser: {ex.Message}", "OK");
            }
        }

        private void OnClearOrganizationClicked(object sender, EventArgs e)
        {
            OrganizationPicker.SelectedItem = null;
            ClearOrganizationButton.IsVisible = false;
        }
    }
}
