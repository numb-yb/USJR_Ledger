using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;

namespace USJRLedger.Views.Adviser
{
    public class NotificationItem
    {
        public string Detail { get; set; }
        public string DateString { get; set; }
    }

    public partial class AdviserDashboardPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;

        private Organization _organization;
        private SchoolYear _currentSchoolYear;
        private bool _isVisible;

        public AdviserDashboardPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = new DataService();
            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser?.Name ?? "User"}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isVisible = true;

            //Subscribe to officer change messages
            MessagingCenter.Subscribe<ManageOfficersPage>(this, "OfficersChanged", async (sender) =>
            {
                await Task.Delay(150); // allow UI teardown to finish
                if (_isVisible)
                    await LoadDashboardAsync();
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                if (_isVisible)
                    await LoadDashboardAsync();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isVisible = false;

            //Unsubscribe when not visible
            MessagingCenter.Unsubscribe<ManageOfficersPage>(this, "OfficersChanged");
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                if (!_isVisible) return;

                var orgs = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                _organization = orgs.FirstOrDefault(o => o.Id == _authService.CurrentUser.OrganizationId);

                if (_organization == null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OrganizationLabel.Text = "No Organization Assigned";
                        SchoolYearStatusLabel.Text = "N/A";
                        BalanceLabel.Text = "₱ 0.00";
                        PendingExpensesLabel.Text = "0";
                        PendingIncomeLabel.Text = "0";
                        NotificationListView.ItemsSource = null;
                    });
                    return;
                }

                var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
                _currentSchoolYear = schoolYears.FirstOrDefault(sy =>
                    sy.OrganizationId == _organization.Id && sy.IsActive);

                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var orgTransactions = transactions.Where(t => t.OrganizationId == _organization.Id).ToList();

                decimal totalIncome = orgTransactions
                    .Where(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Approved)
                    .Sum(t => t.Amount);

                decimal totalExpense = orgTransactions
                    .Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Approved)
                    .Sum(t => t.Amount);

                decimal balance = totalIncome - totalExpense;

                int pendingExpenses = orgTransactions
                    .Count(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Pending);

                int pendingIncome = orgTransactions
                    .Count(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Pending);

                var notifications = orgTransactions
                    .Where(t => t.ApprovalStatus == ApprovalStatus.Pending)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(5)
                    .Select(t => new NotificationItem
                    {
                        Detail = $"{t.Type}: {t.Detail} - ₱{t.Amount:N2}",
                        DateString = t.CreatedDate.ToString("MMM dd, yyyy")
                    })
                    .ToList();

                if (!_isVisible) return;

                // Safely update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isVisible) return;

                    OrganizationLabel.Text = $"Organization: {_organization.Name} ({_organization.Department})";
                    SchoolYearStatusLabel.Text = _currentSchoolYear != null
                        ? $"Active: {_currentSchoolYear.Semester} {_currentSchoolYear.Year}"
                        : "No Active School Year";
                    SchoolYearStatusLabel.TextColor = _currentSchoolYear != null ? Colors.Green : Colors.Red;

                    BalanceLabel.Text = $"₱ {balance:N2}";
                    PendingExpensesLabel.Text = pendingExpenses.ToString();
                    PendingIncomeLabel.Text = pendingIncome.ToString();
                    NotificationListView.ItemsSource = notifications;
                });
            }
            catch (Exception ex)
            {
                if (_isVisible)
                    await DisplayAlert("Error", $"Failed to load dashboard: {ex.Message}", "OK");
            }
        }

        #region Navigation

        private async void OnViewOrgProfileClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new OrganizationProfilePage(_authService, _dataService, _organization.Id));
            else
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
        }

        private async void OnManageOfficersClicked(object sender, EventArgs e)
        {
            if (_organization == null)
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
                return;
            }

            var page = new ManageOfficersPage(_authService, _dataService);
            await Navigation.PushAsync(page);
        }

        private async void OnSchoolYearClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new SchoolYearPage(_authService, _dataService));
            else
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
        }

        private async void OnExpenseRequestsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new ExpenseRequestsPage(_authService, _dataService));
            else
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
        }

        private async void OnIncomeRequestsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new IncomeRequestsPage(_authService, _dataService));
            else
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
        }

        private async void OnEventsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new EventsListPage(_authService, _dataService));
            else
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }

        #endregion
    }
}
