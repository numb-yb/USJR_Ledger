using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using USJRLedger.Views.Adviser;

namespace USJRLedger.Views.Admin
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly SystemResetService _resetService;

        public AdminDashboardPage(AuthService authService)
        {
            InitializeComponent();

            _authService = authService;
            _dataService = new DataService();
            _resetService = new SystemResetService(_dataService);

            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser?.Name ?? "Administrator"}";

            LoadStatsAsync();
        }

        private async void LoadStatsAsync()
        {
            try
            {
                var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                TotalOrganizationsLabel.Text = $"Total Organizations: {organizations?.Count ?? 0}";
                ActiveOrganizationsLabel.Text = $"Active Organizations: {organizations?.Count(o => o.IsActive) ?? 0}";

                var users = await _dataService.LoadFromFileAsync<User>("users.json");
                TotalAdvisersLabel.Text = $"Total Advisers: {users?.Count(u => u.Role == UserRole.Adviser) ?? 0}";

                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                TotalTransactionsLabel.Text = $"Total Transactions: {transactions?.Count ?? 0}";

                var events = await _dataService.LoadFromFileAsync<Event>("events.json");
                TotalEventsLabel.Text = $"Total Events: {events?.Count ?? 0}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async void OnCreateOrgClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateOrganizationPage(_authService, _dataService));
        }

        private async void OnViewOrgsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ViewOrganizationsPage(_authService, _dataService));
        }

        private async void OnManageAdvisersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ManageAdvisersPage(_authService, _dataService));
        }

        private async void OnCompleteSystemResetClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                " COMPLETE SYSTEM RESET ",
                "This will DELETE ALL data including users, organizations, transactions, and events.\n\nThis action cannot be undone.",
                "Reset Everything", "Cancel");

            if (!confirm)
            {
                await DisplayAlert("Cancelled", "System reset cancelled.", "OK");
                return;
            }

            bool secondConfirm = await DisplayAlert(
                "FINAL CONFIRMATION",
                "Are you absolutely sure you want to proceed?",
                "YES, DELETE EVERYTHING", "NO, CANCEL");

            if (!secondConfirm)
            {
                await DisplayAlert("Cancelled", "System reset cancelled.", "OK");
                return;
            }

            try
            {
                await DisplayAlert("Processing", "Resetting the system. Please wait...", "OK");

                // Use the correct method and pass the admin ID
                string adminId = _authService.CurrentUser?.Id ?? string.Empty;
                var results = await _resetService.PerformCompleteSystemResetAsync(adminId);

                await DisplayAlert(
                    "System Reset Complete",
                    $"Users Reset: {results.UsersReset}\n" +
                    $"Organizations Reset: {results.OrganizationsReset}\n" +
                    $"Transactions Reset: {results.TransactionsReset}\n" +
                    $"Other Files Reset: {results.OtherItemsReset}",
                    "OK");

                LoadStatsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"System reset failed: {ex.Message}", "OK");
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }
    }
}
