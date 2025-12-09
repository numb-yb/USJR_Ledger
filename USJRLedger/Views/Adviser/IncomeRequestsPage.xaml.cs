using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Required for ObservableCollection
using System.Linq;
using System.Threading.Tasks;

namespace USJRLedger.Views.Adviser
{
    public partial class IncomeRequestsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly TransactionService _transactionService;
        private readonly UserService _userService;
        private readonly EventService _eventService;
        private readonly string _organizationId;

        private List<Transaction> _pendingIncome;

        public IncomeRequestsPage(AuthService authService, DataService dataService)
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
            await LoadPendingIncomeAsync();
        }

        private async Task LoadPendingIncomeAsync()
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

                _pendingIncome = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Income &&
                                t.ApprovalStatus == ApprovalStatus.Pending)
                    .ToList();

                var pendingIncomeViewModels = new List<IncomeViewModel>();

                foreach (var income in _pendingIncome)
                {
                    var officer = await _userService.GetUserByIdAsync(income.CreatedBy);
                    string eventName = "-";

                    if (!string.IsNullOrEmpty(income.EventId))
                    {
                        var eventItem = await _eventService.GetEventByIdAsync(income.EventId);
                        eventName = eventItem?.Name ?? "-";
                    }

                    pendingIncomeViewModels.Add(new IncomeViewModel
                    {
                        Id = income.Id,
                        Detail = income.Detail,
                        Amount = income.Amount,
                        AmountString = $"\u20B1 {income.Amount:N2}",
                        Category = income.Category.ToString(),
                        RequestedBy = officer?.Name ?? "Unknown",
                        DateRequested = income.CreatedDate.ToString("MMM dd, yyyy"),
                        Event = eventName,
                        HasReceipt = !string.IsNullOrEmpty(income.ReceiptPath),
                        ReceiptPath = income.ReceiptPath
                    });
                }

                // FIX: Use ObservableCollection for dynamic updates
                var sortedList = new ObservableCollection<IncomeViewModel>(
                    pendingIncomeViewModels.OrderByDescending(e => e.DateRequested)
                );

                IncomeList.ItemsSource = sortedList;
                UpdateVisibility(sortedList.Count);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load income requests: {ex.Message}", "OK");
            }
        }

        // Helper method to toggle NoIncome label
        private void UpdateVisibility(int count)
        {
            if (count == 0)
            {
                NoIncome.IsVisible = true;
                IncomeList.IsVisible = false;
            }
            else
            {
                NoIncome.IsVisible = false;
                IncomeList.IsVisible = true;
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var income = button?.BindingContext as IncomeViewModel;

            if (income == null) return;

            bool confirm = await DisplayAlert("Confirm",
                $"Approve income: {income.Detail} - {income.AmountString}?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        income.Id, ApprovalStatus.Approved, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Income approved.", "OK");

                    // FIX: Remove item from list instead of reloading everything
                    if (IncomeList.ItemsSource is ObservableCollection<IncomeViewModel> list)
                    {
                        list.Remove(income);
                        UpdateVisibility(list.Count);
                    }
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
            var income = button?.BindingContext as IncomeViewModel;

            if (income == null) return;

            bool confirm = await DisplayAlert("Confirm",
                $"Reject income: {income.Detail} - {income.AmountString}?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _transactionService.UpdateTransactionApprovalAsync(
                        income.Id, ApprovalStatus.Rejected, _authService.CurrentUser.Id);

                    await DisplayAlert("Success", "Income rejected.", "OK");

                    // FIX: Remove item from list instead of reloading everything
                    if (IncomeList.ItemsSource is ObservableCollection<IncomeViewModel> list)
                    {
                        list.Remove(income);
                        UpdateVisibility(list.Count);
                    }
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
            var income = button?.BindingContext as IncomeViewModel;

            if (income != null && !string.IsNullOrEmpty(income.ReceiptPath))
            {
                try
                {
                    byte[] receiptData = await _dataService.LoadReceiptAsync(income.ReceiptPath);
                    if (receiptData != null)
                    {
                        await Navigation.PushAsync(new ReceiptViewerPage(income.Detail, receiptData));
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

    public class IncomeViewModel
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