using USJRLedger.Models;
using USJRLedger.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace USJRLedger.Views.Adviser
{
    public partial class EventExpensesListPage : ContentPage
    {
        private readonly DataService _dataService;
        private readonly string _organizationId;
        private readonly List<string> _schoolYearIds; // Store to pass to next page

        public EventExpensesListPage(DataService dataService, string organizationId, string yearString, List<string> schoolYearIds)
        {
            InitializeComponent();
            _dataService = dataService;
            _organizationId = organizationId;
            _schoolYearIds = schoolYearIds;

            SchoolYearLabel.Text = yearString;

            _ = LoadDataAsync(schoolYearIds);
        }

        private async Task LoadDataAsync(List<string> schoolYearIds)
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var events = await _dataService.LoadFromFileAsync<Event>("events.json");

                // Get all approved expenses for events in this year(s)
                var approvedExpenses = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Expense &&
                                t.ApprovalStatus == ApprovalStatus.Approved &&
                                schoolYearIds.Contains(t.SchoolYearId) &&
                                t.Category == TransactionCategory.Event &&
                                !string.IsNullOrEmpty(t.EventId))
                    .ToList();

                // Group by Event ID to show summaries
                var eventSummary = approvedExpenses
                    .GroupBy(t => t.EventId)
                    .Select(g =>
                    {
                        var ev = events.FirstOrDefault(e => e.Id == g.Key);
                        return new EventExpenseSummary
                        {
                            Id = g.Key,
                            Name = ev?.Name ?? "Unknown",
                            DateString = ev?.EventDate.ToString("MMM dd, yyyy") ?? "",
                            TotalAmount = g.Sum(t => t.Amount)
                        };
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                TotalLabel.Text = $"\u20B1 {approvedExpenses.Sum(t => t.Amount):N2}";
                EventExpensesList.ItemsSource = eventSummary;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Navigate when user taps an event
        private async void OnEventSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.CurrentSelection.FirstOrDefault() as EventExpenseSummary;
            if (selectedItem != null)
            {
                // Pass the SchoolYearIds so we only show transactions for the selected year
                await Navigation.PushAsync(new EventTransactionsPage(_dataService, selectedItem.Id, selectedItem.Name, _schoolYearIds));

                // Deselect
                ((CollectionView)sender).SelectedItem = null;
            }
        }
    }

    public class EventExpenseSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DateString { get; set; }
        public decimal TotalAmount { get; set; }
    }
}