using USJRLedger.Models;
using USJRLedger.Services;
using System.Collections.ObjectModel;

namespace USJRLedger.Views.Adviser
{
    public partial class GeneralExpensesListPage : ContentPage
    {
        private readonly DataService _dataService;
        private readonly string _organizationId;
        private readonly string _schoolYearId; // Can be a specific ID or a Year string depending on your grouping logic

        public GeneralExpensesListPage(DataService dataService, string organizationId, string schoolYearString, List<string> schoolYearIds)
        {
            InitializeComponent();
            _dataService = dataService;
            _organizationId = organizationId;

            SchoolYearLabel.Text = schoolYearString;

            _ = LoadDataAsync(schoolYearIds);
        }

        private async Task LoadDataAsync(List<string> schoolYearIds)
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var users = await _dataService.LoadFromFileAsync<User>("users.json");

                var generalExpenses = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Expense &&
                                t.ApprovalStatus == ApprovalStatus.Approved &&
                                schoolYearIds.Contains(t.SchoolYearId) &&
                                (t.Category == TransactionCategory.General || string.IsNullOrEmpty(t.EventId)))
                    .Select(t => new GeneralExpenseViewModel
                    {
                        Detail = t.Detail,
                        AmountString = $"\u20B1 {t.Amount:N2}",
                        DateString = t.CreatedDate.ToString("MMM dd, yyyy"),
                        RequestedBy = users.FirstOrDefault(u => u.Id == t.CreatedBy)?.Name ?? "Unknown",
                        Amount = t.Amount
                    })
                    .OrderByDescending(x => x.DateString) // Sort by date
                    .ToList();

                TotalLabel.Text = $"\u20B1 {generalExpenses.Sum(x => x.Amount):N2}";
                ExpensesCollectionView.ItemsSource = generalExpenses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load expenses: " + ex.Message, "OK");
            }
        }
    }

    public class GeneralExpenseViewModel
    {
        public string Detail { get; set; }
        public string AmountString { get; set; }
        public decimal Amount { get; set; }
        public string DateString { get; set; }
        public string RequestedBy { get; set; }
    }
}