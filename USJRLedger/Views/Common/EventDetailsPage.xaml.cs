using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;

namespace USJRLedger.Views.Adviser
{
    public partial class EventDetailsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly EventService _eventService;
        private readonly TransactionService _transactionService;
        private readonly UserService _userService;
        private readonly string _eventId;
        private Event _event;

        public EventDetailsPage(AuthService authService, DataService dataService, string eventId)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _eventService = new EventService(dataService);
            _transactionService = new TransactionService(dataService);
            _userService = new UserService(dataService);
            _eventId = eventId;

            LoadEventDetailsAsync();
        }

        private async void LoadEventDetailsAsync()
        {
            try
            {
                _event = await _eventService.GetEventByIdAsync(_eventId);

                if (_event != null)
                {
                    EventNameLabel.Text = _event.Name;
                    EventDateLabel.Text = _event.EventDate.ToString("MMMM dd, yyyy");

                    var creator = await _userService.GetUserByIdAsync(_event.CreatedBy);
                    CreatedByLabel.Text = creator?.Name ?? "Unknown";
                    CreatedDateLabel.Text = _event.CreatedDate.ToString("MMMM dd, yyyy");

                    decimal balance = await _eventService.GetEventBalanceAsync(_eventId);
                    BalanceLabel.Text = $"₱ {balance:N2}";

                    // Load transactions for this event
                    var transactions = await _transactionService.GetTransactionsByEventAsync(_eventId);

                    // Create separate lists for income and expenses
                    var incomeTransactions = transactions
                        .Where(t => t.Type == TransactionType.Income && t.ApprovalStatus == ApprovalStatus.Approved)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToList();

                    var expenseTransactions = transactions
                        .Where(t => t.Type == TransactionType.Expense && t.ApprovalStatus == ApprovalStatus.Approved)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToList();

                    decimal totalIncome = incomeTransactions.Sum(t => t.Amount);
                    decimal totalExpense = expenseTransactions.Sum(t => t.Amount);

                    TotalIncomeLabel.Text = $"₱ {totalIncome:N2}";
                    TotalExpensesLabel.Text = $"₱ {totalExpense:N2}";

                    // Create transaction view models
                    var incomeViewModels = await CreateTransactionViewModelsAsync(incomeTransactions);
                    var expenseViewModels = await CreateTransactionViewModelsAsync(expenseTransactions);

                    IncomeListView.ItemsSource = incomeViewModels;
                    ExpenseListView.ItemsSource = expenseViewModels;

                    // Set visibility based on whether there are transactions
                    NoIncomeLabel.IsVisible = incomeViewModels.Count == 0;
                    IncomeListView.IsVisible = incomeViewModels.Count > 0;

                    NoExpensesLabel.IsVisible = expenseViewModels.Count == 0;
                    ExpenseListView.IsVisible = expenseViewModels.Count > 0;
                }
                else
                {
                    await DisplayAlert("Error", "Event not found", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load event details: {ex.Message}", "OK");
            }
        }

        private async Task<List<TransactionViewModel>> CreateTransactionViewModelsAsync(List<Transaction> transactions)
        {
            var viewModels = new List<TransactionViewModel>();

            foreach (var transaction in transactions)
            {
                var creator = await _userService.GetUserByIdAsync(transaction.CreatedBy);

                viewModels.Add(new TransactionViewModel
                {
                    Detail = transaction.Detail,
                    Amount = $"₱ {transaction.Amount:N2}",
                    CreatedBy = creator?.Name ?? "Unknown",
                    DateString = transaction.CreatedDate.ToString("MMM dd, yyyy"),
                    HasReceipt = !string.IsNullOrEmpty(transaction.ReceiptPath),
                    ReceiptPath = transaction.ReceiptPath,
                    TransactionId = transaction.Id
                });
            }

            return viewModels;
        }

        private async void OnViewReceiptClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var transaction = button?.BindingContext as TransactionViewModel;

            if (transaction != null && !string.IsNullOrEmpty(transaction.ReceiptPath))
            {
                try
                {
                    byte[] receiptData = await _dataService.LoadReceiptAsync(transaction.ReceiptPath);

                    if (receiptData != null)
                    {
                        // Navigate to receipt viewer page
                        await Navigation.PushAsync(new ReceiptViewerPage(transaction.Detail, receiptData));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to load receipt", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to view receipt: {ex.Message}", "OK");
                }
            }
        }

        private async void OnGenerateStatementClicked(object sender, EventArgs e)
        {
            try
            {
                var reportService = new ReportService(_dataService);
                string report = await reportService.GenerateEventStatementAsync(_eventId);

                string fileName = $"{_event.Name.Replace(" ", "_")}_Statement_{DateTime.Now:yyyyMMdd}.txt";
                await reportService.SaveReportToFileAsync(report, fileName);

                await DisplayAlert("Success", $"Event statement generated and saved as {fileName}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to generate statement: {ex.Message}", "OK");
            }
        }
    }

    // Helper class for the transaction list views
    public class TransactionViewModel
    {
        public string TransactionId { get; set; }
        public string Detail { get; set; }
        public string Amount { get; set; }
        public string CreatedBy { get; set; }
        public string DateString { get; set; }
        public bool HasReceipt { get; set; }
        public string ReceiptPath { get; set; }
    }
}