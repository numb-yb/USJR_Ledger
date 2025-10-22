using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;

namespace USJRLedger.Views.Officer
{
    public class TransactionItem
    {
        public string Detail { get; set; }
        public string DateString { get; set; }
        public string AmountString { get; set; }
        public string StatusString { get; set; }
        public Color StatusColor { get; set; }
    }

    public partial class OfficerDashboardPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private Organization _organization;
        private SchoolYear _currentSchoolYear;

        public OfficerDashboardPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;

            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser.Name}";
            PositionLabel.Text = $"Position: {_authService.CurrentUser.Position}";

            LoadDataAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
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
                    SchoolYearLabel.Text = $"{_currentSchoolYear.Semester} {_currentSchoolYear.Year}";
                }
                else
                {
                    SchoolYearLabel.Text = "No Active School Year";
                }

                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var orgTransactions = transactions.Where(t => t.OrganizationId == _organization.Id);

                decimal totalIncome = orgTransactions
                    .Where(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Approved)
                    .Sum(t => t.Amount);

                decimal totalExpense = orgTransactions
                    .Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Approved)
                    .Sum(t => t.Amount);

                decimal balance = totalIncome - totalExpense;

                // ✅ Display and color the balance
                BalanceLabel.Text = $"₱ {balance:N2}";
                BalanceLabel.TextColor = balance < 0 ? Colors.Red : Colors.Black;

                int pendingExpensesCount = orgTransactions.Count(t =>
                    t.Type == TransactionType.Expense &&
                    t.ApprovalStatus == ApprovalStatus.Pending &&
                    t.CreatedBy == _authService.CurrentUser.Id);

                PendingExpensesLabel.Text = pendingExpensesCount.ToString();

                var recentTransactions = orgTransactions
                    .Where(t => t.CreatedBy == _authService.CurrentUser.Id)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(10)
                    .Select(t => new TransactionItem
                    {
                        Detail = $"{(t.Type == TransactionType.Income ? "Income" : "Expense")}: {t.Detail}",
                        DateString = t.CreatedDate.ToString("MMM dd, yyyy"),
                        AmountString = $"₱ {t.Amount:N2}",
                        StatusString = t.ApprovalStatus.ToString(),
                        StatusColor = t.ApprovalStatus switch
                        {
                            ApprovalStatus.Approved => Colors.Green,
                            ApprovalStatus.Rejected => Colors.Red,
                            _ => Colors.Orange
                        }
                    })
                    .ToList();

                TransactionsListView.ItemsSource = recentTransactions;
            }
            else
            {
                OrganizationLabel.Text = "No Organization Assigned";
                SchoolYearLabel.Text = "N/A";
                BalanceLabel.Text = "₱ 0.00";
                BalanceLabel.TextColor = Colors.Black;
                PendingExpensesLabel.Text = "0";
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

        private async void OnCreateEventClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                if (_currentSchoolYear != null)
                {
                    await Navigation.PushAsync(new CreateEventPage(_authService, _dataService));
                }
                else
                {
                    await DisplayAlert("Error", "No active school year. Please contact your adviser.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnAddExpenseClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                if (_currentSchoolYear != null)
                {
                    await Navigation.PushAsync(new AddExpensePage(_authService, _dataService));
                }
                else
                {
                    await DisplayAlert("Error", "No active school year. Please contact your adviser.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "No organization assigned to your account", "OK");
            }
        }

        private async void OnAddIncomeClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                if (_currentSchoolYear != null)
                {
                    await Navigation.PushAsync(new AddIncomePage(_authService, _dataService));
                }
                else
                {
                    await DisplayAlert("Error", "No active school year. Please contact your adviser.", "OK");
                }
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
