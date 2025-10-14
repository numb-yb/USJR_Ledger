using USJRLedger.Models;
using USJRLedger.Services;

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
                OrganizationPicker.ItemsSource = _organizations;
                OrganizationPicker.ItemDisplayBinding = new Binding("Name");

                _advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
                AdvisersListView.ItemsSource = _advisers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
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

            string name = NameEntry.Text;
            string username = UsernameEntry.Text;
            string password = PasswordEntry.Text;
            var selectedOrg = OrganizationPicker.SelectedItem as Organization;

            try
            {
                await _userService.CreateAdviserAsync(name, username, password, selectedOrg.Id);
                await DisplayAlert("Success", "Adviser added successfully", "OK");

                // Clear form
                NameEntry.Text = string.Empty;
                UsernameEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                OrganizationPicker.SelectedItem = null;

                // Reload advisers
                _advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
                AdvisersListView.ItemsSource = _advisers;
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

            if (adviser != null)
            {
                bool isActive = !adviser.IsActive;
                string action = isActive ? "activate" : "deactivate";

                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to {action} adviser {adviser.Name}?", "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _userService.UpdateUserStatusAsync(adviser.Id, isActive);
                        await DisplayAlert("Success", $"Adviser {action}d successfully", "OK");

                        // Reload advisers
                        _advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
                        AdvisersListView.ItemsSource = _advisers;
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to update adviser: {ex.Message}", "OK");
                    }
                }
            }
        }
    }
}