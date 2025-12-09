using USJRLedger.Models;
using USJRLedger.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace USJRLedger.Views.Adviser
{
    public partial class EventTransactionsPage : ContentPage
    {
        private readonly DataService _dataService;
        private readonly string _eventId;

        public EventTransactionsPage(DataService dataService, string eventId, string eventName, List<string> schoolYearIds)
        {
            InitializeComponent();
            _dataService = dataService;
            _eventId = eventId;

            EventNameLabel.Text = eventName;
            _ = LoadDataAsync(schoolYearIds);
        }

        private async Task LoadDataAsync(List<string> schoolYearIds)
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var users = await _dataService.LoadFromFileAsync<User>("users.json");

                // Filter for Approved Expenses -> Belonging to this Event -> Within selected School Years
                var list = transactions
                    .Where(t => t.Type == TransactionType.Expense &&
                                t.ApprovalStatus == ApprovalStatus.Approved &&
                                t.EventId == _eventId &&
                                schoolYearIds.Contains(t.SchoolYearId))
                    .Select(t => new
                    {
                        Detail = t.Detail,
                        AmountString = $"\u20B1 {t.Amount:N2}",
                        Amount = t.Amount,
                        DateString = t.CreatedDate.ToString("MMM dd, yyyy"),
                        // Look up the User name
                        RequestedBy = users.FirstOrDefault(u => u.Id == t.CreatedBy)?.Name ?? "Unknown"
                    })
                    .OrderByDescending(x => x.DateString)
                    .ToList();

                TotalLabel.Text = $"\u20B1 {list.Sum(x => x.Amount):N2}";
                TransactionsList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}