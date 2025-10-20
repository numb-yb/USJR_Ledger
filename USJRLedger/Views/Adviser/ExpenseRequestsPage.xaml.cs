using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using System.Collections.ObjectModel;

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

                var pendingExpenseViewModels = new List<ExpenseViewModel>();

                foreach (var expense in _pendingExpenses)
                {
                    var officer = await _userService.GetUserByIdAsync(expense.CreatedBy);

                    string eventName = "None";
                    if (!string.IsNullOrEmpty(expense.EventId))
                    {
                        var eventItem = await _eventService.GetEventByIdAsync(expense.EventId);
                        eventName = eventItem != null && !string.IsNullOrEmpty(eventItem.Name)
                            ? eventItem.Name
                            : "None";
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

                ExpensesCollectionView.ItemsSource = pendingExpenseViewModels
                    .OrderByDescending(e => e.DateRequested)
                    .ToList();

                bool hasItems = pendingExpenseViewModels.Any();
                NoExpensesLabel.IsVisible = !hasItems;
                ExpensesCollectionView.IsVisible = hasItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load expense requests: {ex.Message}", "OK");
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ExpenseViewModel expense)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to approve this expense:\n\n{expense.Detail}\n{expense.AmountString}?",
                    "Yes", "No");

                if (!confirm)
                    return;

                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        expense.Id, ApprovalStatus.Approved, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Expense approved successfully.", "OK");
                    await LoadPendingExpensesAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to approve expense: {ex.Message}", "OK");
                }
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ExpenseViewModel expense)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to reject this expense:\n\n{expense.Detail}\n{expense.AmountString}?",
                    "Yes", "No");

                if (!confirm)
                    return;

                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        expense.Id, ApprovalStatus.Rejected, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Expense rejected successfully.", "OK");
                    await LoadPendingExpensesAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to reject expense: {ex.Message}", "OK");
                }
            }
        }

        private async void OnViewReceiptClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ExpenseViewModel expense)
            {
                if (string.IsNullOrEmpty(expense.ReceiptPath))
                {
                    await DisplayAlert("Error", "No receipt found for this expense.", "OK");
                    return;
                }

                try
                {
                    byte[] receiptData = await _dataService.LoadReceiptAsync(expense.ReceiptPath);

                    if (receiptData != null)
                    {
                        await Navigation.PushAsync(new ReceiptViewerPage(expense.Detail, receiptData));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to load receipt data.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to view receipt: {ex.Message}", "OK");
                }
            }
        }
    }

    // ViewModel for UI Binding
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
