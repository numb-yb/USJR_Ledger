using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Common
{
    public partial class OrganizationProfilePage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly string _organizationId;
        private Organization _organization;

        public OrganizationProfilePage(AuthService authService, DataService dataService, string organizationId)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationId = organizationId;

            LoadOrganizationDataAsync();
        }

        private async void LoadOrganizationDataAsync()
        {
            var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
            _organization = organizations.FirstOrDefault(o => o.Id == _organizationId);

            if (_organization != null)
            {
                OrganizationNameLabel.Text = _organization.Name;
                DepartmentLabel.Text = _organization.Department;
                StatusLabel.Text = _organization.IsActive ? "Active" : "Inactive";

                if (!_organization.IsActive && _organization.DeactivationDate.HasValue)
                {
                    DeactivationDateLabel.Text = $"Deactivated on: {_organization.DeactivationDate.Value:MMMM dd, yyyy}";
                    DeactivationDateLabel.IsVisible = true;
                }
                else
                {
                    DeactivationDateLabel.IsVisible = false;
                }

                await LoadFinancialDataAsync();
            }
            else
            {
                // Handle case where organization is not found
                await DisplayAlert("Error", "Organization not found", "OK");
                await Navigation.PopAsync();
            }
        }

        private async Task LoadFinancialDataAsync()
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var orgTransactions = transactions.Where(t => t.OrganizationId == _organizationId &&
                                                        t.ApprovalStatus == ApprovalStatus.Approved);

            decimal totalIncome = orgTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal totalExpense = orgTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            decimal balance = totalIncome - totalExpense;

            TotalIncomeLabel.Text = $"₱ {totalIncome:N2}";
            TotalExpensesLabel.Text = $"₱ {totalExpense:N2}";
            BalanceLabel.Text = $"₱ {balance:N2}";

            // Load events if any
            var events = await _dataService.LoadFromFileAsync<Event>("events.json");
            var orgEvents = events.Where(e => e.OrganizationId == _organizationId)
                                 .OrderByDescending(e => e.EventDate)
                                 .ToList();

            EventsListView.ItemsSource = orgEvents;
        }
    }
}