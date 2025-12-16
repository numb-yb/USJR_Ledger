using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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

        // Use ObservableCollection so the UI updates automatically when items are removed
        private ObservableCollection<ExpenseViewModel> _pendingExpenses;

        public ExpenseRequestsPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();

            _authService = authService;
            _dataService = dataService;
            _transactionService = new TransactionService(dataService);
            _userService = new UserService(dataService);
            _eventService = new EventService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPendingExpensesAsync();
        }

        private async Task LoadPendingExpensesAsync()
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

                // Filter for pending expenses
                var rawList = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Expense &&
                                t.ApprovalStatus == ApprovalStatus.Pending)
                    .ToList();

                var viewModels = new List<ExpenseViewModel>();

                foreach (var expense in rawList)
                {
                    var officer = await _userService.GetUserByIdAsync(expense.CreatedBy);
                    string eventName = "-";

                    if (!string.IsNullOrEmpty(expense.EventId))
                    {
                        var eventItem = await _eventService.GetEventByIdAsync(expense.EventId);
                        eventName = eventItem?.Name ?? "-";
                    }

                    // --- LOGIC: Handle General Expense Title ---
                    string displayTitle = expense.Detail;

                    if (expense.Category == TransactionCategory.General)
                    {
                        displayTitle = "General Expense";
                    }
                    else if (string.IsNullOrEmpty(displayTitle))
                    {
                        displayTitle = expense.Category.ToString();
                    }
                    // ------------------------------------------

                    viewModels.Add(new ExpenseViewModel
                    {
                        Id = expense.Id,
                        Detail = displayTitle,
                        Amount = expense.Amount,
                        AmountString = $"\u20B1 {expense.Amount:N2}", // Peso Sign
                        Category = expense.Category.ToString(),
                        RequestedBy = officer?.Name ?? "Unknown",
                        DateRequested = expense.CreatedDate.ToString("MMM dd, yyyy"),
                        Event = eventName,
                        HasReceipt = !string.IsNullOrEmpty(expense.ReceiptPath),
                        ReceiptPath = expense.ReceiptPath
                    });
                }

                // Sort by date (Newest first) and bind
                _pendingExpenses = new ObservableCollection<ExpenseViewModel>(
                    viewModels.OrderByDescending(e => e.DateRequested)
                );

                ExpensesCollectionView.ItemsSource = _pendingExpenses;
                UpdateVisibility();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load expenses: {ex.Message}", "OK");
            }
        }

        private void UpdateVisibility()
        {
            if (_pendingExpenses == null || _pendingExpenses.Count == 0)
            {
                NoExpensesLabel.IsVisible = true;
                ExpensesCollectionView.IsVisible = false;
            }
            else
            {
                NoExpensesLabel.IsVisible = false;
                ExpensesCollectionView.IsVisible = true;
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var expense = button?.BindingContext as ExpenseViewModel;
            if (expense == null) return;

            bool confirm = await DisplayAlert("Confirm",
                $"Approve expense: {expense.Detail} - {expense.AmountString}?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        expense.Id, ApprovalStatus.Approved, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Expense approved.", "OK");

                    // Remove from list immediately
                    _pendingExpenses.Remove(expense);
                    UpdateVisibility();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed: {ex.Message}", "OK");
                }
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var expense = button?.BindingContext as ExpenseViewModel;
            if (expense == null) return;

            bool confirm = await DisplayAlert("Confirm",
                $"Reject expense: {expense.Detail} - {expense.AmountString}?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        expense.Id, ApprovalStatus.Rejected, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Expense rejected.", "OK");

                    // Remove from list immediately
                    _pendingExpenses.Remove(expense);
                    UpdateVisibility();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed: {ex.Message}", "OK");
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
                        await Navigation.PushAsync(new ReceiptViewerPage(expense.Detail, receiptData));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Receipt file not found.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Cannot view receipt: {ex.Message}", "OK");
                }
            }
        }
    }

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