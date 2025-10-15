using USJRLedger.Models;
using USJRLedger.Services;

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

            LoadOfficersAsync();


        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadOfficersAsync();
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
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(PositionEntry.Text))
            {
                await DisplayAlert("Error", "Please fill all fields", "OK");
                return;
            }

            string name = NameEntry.Text;
            string studentId = StudentIdEntry.Text;
            string password = PasswordEntry.Text;
            string position = PositionEntry.Text;

            try
            {
                await _userService.CreateOfficerAsync(name, studentId, password, _organizationId, position);
                await DisplayAlert("Success", "Officer added successfully", "OK");

                // Clear form
                NameEntry.Text = string.Empty;
                StudentIdEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                PositionEntry.Text = string.Empty;

                // Reload officers
                await LoadOfficersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add officer: {ex.Message}", "OK");
            }
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var officer = button?.BindingContext as User;

            if (officer != null)
            {
                bool isActive = !officer.IsActive;
                string action = isActive ? "activate" : "deactivate";

                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to {action} officer {officer.Name}?", "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _userService.UpdateUserStatusAsync(officer.Id, isActive);
                        await DisplayAlert("Success", $"Officer {action}d successfully", "OK");

                        // Reload officers
                        await LoadOfficersAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to update officer: {ex.Message}", "OK");
                    }
                }
            }
        }
    }
}