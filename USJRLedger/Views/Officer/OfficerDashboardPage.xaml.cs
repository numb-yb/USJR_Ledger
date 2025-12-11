using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using USJRLedger.Views.Adviser; // Necessary to access Adviser pages
using System.Collections.ObjectModel;

namespace USJRLedger.Views.Officer
{
    public partial class OfficerDashboardPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;

        // We need to store the organization object to pass its ID later
        private Organization _organization;

        public OfficerDashboardPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;

            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser?.Name}";
            PositionLabel.Text = $"Position: {_authService.CurrentUser?.Role}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                var orgs = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                _organization = orgs.FirstOrDefault(o => o.Id == _authService.CurrentUser.OrganizationId);

                if (_organization != null)
                {
                    OrganizationLabel.Text = $"Organization: {_organization.Name}";

                    // --- Load Balance & School Year Data ---
                    var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                    var schoolYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

                    var activeSy = schoolYears.FirstOrDefault(sy => sy.OrganizationId == _organization.Id && sy.IsActive);
                    SchoolYearLabel.Text = activeSy != null ? $"{activeSy.Semester} {activeSy.Year}" : "No Active SY";

                    var orgTransactions = transactions.Where(t => t.OrganizationId == _organization.Id).ToList();

                    // Calculate Totals
                    decimal income = orgTransactions
                        .Where(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Approved)
                        .Sum(t => t.Amount);

                    decimal expense = orgTransactions
                        .Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Approved)
                        .Sum(t => t.Amount);

                    BalanceLabel.Text = $"₱ {income - expense:N2}";

                    // Calculate Pending Counts
                    int pendingExp = orgTransactions.Count(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Pending);
                    int pendingInc = orgTransactions.Count(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Pending);

                    PendingExpensesLabel.Text = pendingExp.ToString();
                    PendingIncomeLabel.Text = pendingInc.ToString();

                    // Load Recent List
                    var recentList = orgTransactions
                        .OrderByDescending(t => t.CreatedDate)
                        .Take(5)
                        .Select(t => new TransactionViewModel
                        {
                            Detail = t.Detail,
                            DateString = t.CreatedDate.ToString("MMM dd, yyyy"),
                            AmountString = $"₱ {t.Amount:N2}",
                            StatusString = t.ApprovalStatus.ToString(),
                            HasReceipt = !string.IsNullOrEmpty(t.ReceiptPath),
                            ReceiptPath = t.ReceiptPath
                        })
                        .ToList();

                    TransactionsListView.ItemsSource = recentList;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Dashboard Error: {ex.Message}", "OK");
            }
        }

        // --- NAVIGATION HANDLERS ---

        private async void OnViewOrgProfileClicked(object sender, EventArgs e)
        {
            if (_organization != null)
                await Navigation.PushAsync(new OrganizationProfilePage(_authService, _dataService, _organization.Id));
        }

        private async void OnCreateEventClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateEventPage(_authService, _dataService));
        }

        private async void OnAddExpenseClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddExpensePage(_authService, _dataService));
        }

        private async void OnAddIncomeClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddIncomePage(_authService, _dataService));
        }

        // NEW BUTTON HANDLER: Expense Breakdown
        private async void OnExpenseBreakdownClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                // Passing all required services to the Adviser's breakdown page
                await Navigation.PushAsync(new ExpenseBreakdownPage(_authService, _dataService, _organization.Id));
            }
            else
            {
                await DisplayAlert("Error", "Organization not found.", "OK");
            }
        }

        // NEW BUTTON HANDLER: Income Trail
        private async void OnIncomeTrailClicked(object sender, EventArgs e)
        {
            if (_organization != null)
            {
                await Navigation.PushAsync(new IncomeTrailPage(_organization.Id));
            }
            else
            {
                await DisplayAlert("Error", "Organization not found.", "OK");
            }
        }

        private async void OnViewReceiptClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var item = button?.BindingContext as TransactionViewModel;

            if (item != null && !string.IsNullOrEmpty(item.ReceiptPath))
            {
                try
                {
                    byte[] data = await _dataService.LoadReceiptAsync(item.ReceiptPath);
                    if (data != null)
                        await Navigation.PushAsync(new ReceiptViewerPage(item.Detail, data));
                    else
                        await DisplayAlert("Error", "Receipt file not found.", "OK");
                }
                catch
                {
                    await DisplayAlert("Error", "Could not load receipt.", "OK");
                }
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }
    }

    // Helper ViewModel for the list
    public class TransactionViewModel
    {
        public string Detail { get; set; }
        public string DateString { get; set; }
        public string AmountString { get; set; }
        public string StatusString { get; set; }
        public bool HasReceipt { get; set; }
        public string ReceiptPath { get; set; }
    }
}