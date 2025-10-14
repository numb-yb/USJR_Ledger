using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
namespace USJRLedger.Views.Adviser
{
    public partial class ExpenseRequestsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly TransactionService _transactionService;
        private readonly UserService _userService;
        private readonly EventService _eventService;
        private readonly string _organizationId;
        private List<Transaction> _pendingExpenses;

        public ExpenseRequestsPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _transactionService = new TransactionService(dataService);
            _userService = new UserService(dataService);
            _eventService = new EventService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            LoadPendingExpensesAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadPendingExpensesAsync();
        }

        private async Task LoadPendingExpensesAsync()
        {
            try
            {
                _pendingExpenses = await _transactionService.GetPendingExpensesAsync(_organizationId);

                // Create a list of expense view models with additional info
                var pendingExpenseViewModels = new List<ExpenseViewModel>();

                foreach (var expense in _pendingExpenses)
                {
                    var officer = await _userService.GetUserByIdAsync(expense.CreatedBy);
                    string eventName = "-";

                    if (!string.IsNullOrEmpty(expense.EventId))
                    {
                        var eventItem = await _eventService.GetEventByIdAsync(expense.EventId);
                        eventName = eventItem?.Name ?? "-";
                    }

                    pendingExpenseViewModels.Add(new ExpenseViewModel
                    {
                        Id = expense.Id,
                        Detail = expense.Detail,
                        Amount = expense.Amount,
                        AmountString = $"₱ {expense.Amount:N2}",
                        Category = expense.Category.ToString(),
                        RequestedBy = officer?.Name ?? "Unknown",
                        DateRequested = expense.CreatedDate.ToString("MMM dd, yyyy"),
                        Event = eventName,
                        HasReceipt = !string.IsNullOrEmpty(expense.ReceiptPath),
                        ReceiptPath = expense.ReceiptPath
                    });
                }

                ExpensesListView.ItemsSource = pendingExpenseViewModels.OrderByDescending(e => e.DateRequested);

                if (pendingExpenseViewModels.Count == 0)
                {
                    NoExpensesLabel.IsVisible = true;
                    ExpensesListView.IsVisible = false;
                }
                else
                {
                    NoExpensesLabel.IsVisible = false;
                    ExpensesListView.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load expense requests: {ex.Message}", "OK");
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var expense = button?.BindingContext as ExpenseViewModel;

            if (expense != null)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to approve this expense: {expense.Detail} - {expense.AmountString}?",
                    "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _transactionService.UpdateTransactionApprovalAsync(
                            expense.Id, ApprovalStatus.Approved, _authService.CurrentUser.Id);

                        await DisplayAlert("Success", "Expense approved successfully", "OK");
                        await LoadPendingExpensesAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to approve expense: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var expense = button?.BindingContext as ExpenseViewModel;

            if (expense != null)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to reject this expense: {expense.Detail} - {expense.AmountString}?",
                    "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _transactionService.UpdateTransactionApprovalAsync(
                            expense.Id, ApprovalStatus.Rejected, _authService.CurrentUser.Id);

                        await DisplayAlert("Success", "Expense rejected successfully", "OK");
                        await LoadPendingExpensesAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to reject expense: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnViewReceiptClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var expense = button?.BindingContext as ExpenseViewModel;

            if (expense != null && !string.IsNullOrEmpty(expense.ReceiptPath))
            {
                try
                {
                    byte[] receiptData = await _dataService.LoadReceiptAsync(expense.ReceiptPath);

                    if (receiptData != null)
                    {
                        // Navigate to receipt viewer page
                        await Navigation.PushAsync(new ReceiptViewerPage(expense.Detail, receiptData));
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
    }

    // Helper class for the expense list view
    public class ExpenseViewModel
    {
        public string Id { get; set; }
        public string Detail { get; set; }
        public decimal Amount { get; set; }
        public string AmountString { get; set; }
        public string Category { get; set; }
        public string RequestedBy { get; set; }
        public string DateRequested { get; set; }
        public string Event { get; set; }
        public bool HasReceipt { get; set; }
        public string ReceiptPath { get; set; }
    }
}