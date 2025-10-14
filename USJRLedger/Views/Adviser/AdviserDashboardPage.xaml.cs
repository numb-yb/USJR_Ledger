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

        public AdviserDashboardPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = new DataService();

            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser.Name}";

            _ = LoadDataAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
            _organization = organizations.FirstOrDefault(o => o.Id == _authService.CurrentUser.OrganizationId);

            if (_organization != null)
            {
                OrganizationLabel.Text = $"Organization: {_organization.Name} ({_organization.Department})";

                var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");
                _currentSchoolYear = schoolYears.FirstOrDefault(sy => sy.OrganizationId == _organization.Id && sy.IsActive);

                if (_currentSchoolYear != null)
                {
                    SchoolYearStatusLabel.Text = $"Active: {_currentSchoolYear.Semester} {_currentSchoolYear.Year}";
                    SchoolYearStatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    SchoolYearStatusLabel.Text = "No Active School Year";
                    SchoolYearStatusLabel.TextColor = Colors.Red;
                }

                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var orgTransactions = transactions.Where(t => t.OrganizationId == _organization.Id);

                decimal totalIncome = orgTransactions.Where(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Approved).Sum(t => t.Amount);
                decimal totalExpense = orgTransactions.Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Approved).Sum(t => t.Amount);
                decimal balance = totalIncome - totalExpense;

                BalanceLabel.Text = $"₱ {balance:N2}";

                int pendingExpensesCount = orgTransactions.Count(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Pending);
                PendingExpensesLabel.Text = pendingExpensesCount.ToString();

                // Add pending income count
                int pendingIncomeCount = orgTransactions.Count(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Pending);
                PendingIncomeLabel.Text = pendingIncomeCount.ToString();

                var pendingExpenses = orgTransactions
                    .Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Pending)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(5)
                    .Select(t => new NotificationItem
                    {
                        Detail = $"Expense Request: {t.Detail} - ₱{t.Amount:N2}",
                        DateString = t.CreatedDate.ToString("MMM dd, yyyy")
                    })
                    .ToList();

                NotificationListView.ItemsSource = pendingExpenses;
            }
            else
            {
                OrganizationLabel.Text = "No Organization Assigned";
                SchoolYearStatusLabel.Text = "N/A";
                BalanceLabel.Text = "₱ 0.00";
                PendingExpensesLabel.Text = "0";
                PendingIncomeLabel.Text = "0";
            }
        }

        private async void OnViewOrgProfileClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new OrganizationProfilePage(_authService, _dataService, _organization.Id));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnManageOfficersClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new ManageOfficersPage(_authService, _dataService));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnSchoolYearClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new SchoolYearPage(_authService, _dataService));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnExpenseRequestsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new ExpenseRequestsPage(_authService, _dataService));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnIncomeRequestsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new IncomeRequestsPage(_authService, _dataService));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnEventsClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new EventsListPage(_authService, _dataService));
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }
    }
}