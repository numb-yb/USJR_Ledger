using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;

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

        // Declare UI elements as class fields - fixed variable names
        private Label NoIncomeLabel;
        private ListView IncomeListView;

        public IncomeRequestsPage(AuthService authService, DataService dataService)
        {
            // Replace InitializeComponent with our custom method
            CreateUI();

            _authService = authService;
            _dataService = dataService;
            _transactionService = new TransactionService(dataService);
            _userService = new UserService(dataService);
            _eventService = new EventService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            _ = LoadPendingIncomeAsync();
        }

        // Custom method to replace the missing InitializeComponent()
        private void CreateUI()
        {
            Title = "Income Requests";

            // Create main layout
            var mainLayout = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 20
            };

            // Add header
            mainLayout.Add(new Label
            {
                Text = "Pending Income Requests",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold
            });

            // Create and add NoIncomeLabel - fixed variable name
            NoIncomeLabel = new Label
            {
                Text = "No pending income requests",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 16,
                TextColor = Colors.Gray,
                IsVisible = false
            };
            mainLayout.Add(NoIncomeLabel);

            // Create and add IncomeListView - fixed variable name
            IncomeListView = new ListView
            {
                HasUnevenRows = true
            };

            // Set up the item template
            IncomeListView.ItemTemplate = new DataTemplate(() =>
            {
                var viewCell = new ViewCell();

                var frame = new Frame
                {
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    BorderColor = Colors.Gray
                };

                var grid = new Grid();

                // Add row definitions
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Add column definitions
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Detail row
                var detailLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16
                };
                detailLabel.SetBinding(Label.TextProperty, "Detail");
                Grid.SetRow(detailLabel, 0);
                Grid.SetColumn(detailLabel, 0);
                grid.Add(detailLabel);

                // Amount row
                var amountStack = new HorizontalStackLayout { Spacing = 5 };
                amountStack.Add(new Label
                {
                    Text = "Amount:",
                    FontAttributes = FontAttributes.Bold
                });
                var amountLabel = new Label { TextColor = Colors.Green };
                amountLabel.SetBinding(Label.TextProperty, "AmountString");
                amountStack.Add(amountLabel);

                Grid.SetRow(amountStack, 1);
                Grid.SetColumn(amountStack, 0);
                grid.Add(amountStack);

                // Requested by row
                var requestedStack = new HorizontalStackLayout { Spacing = 5 };
                requestedStack.Add(new Label
                {
                    Text = "Requested By:",
                    FontAttributes = FontAttributes.Bold
                });
                var requestedLabel = new Label();
                requestedLabel.SetBinding(Label.TextProperty, "RequestedBy");
                requestedStack.Add(requestedLabel);

                Grid.SetRow(requestedStack, 2);
                Grid.SetColumn(requestedStack, 0);
                grid.Add(requestedStack);

                // Date row
                var dateStack = new HorizontalStackLayout { Spacing = 5 };
                dateStack.Add(new Label
                {
                    Text = "Date:",
                    FontAttributes = FontAttributes.Bold
                });
                var dateLabel = new Label();
                dateLabel.SetBinding(Label.TextProperty, "DateRequested");
                dateStack.Add(dateLabel);

                dateStack.Add(new Label
                {
                    Text = "Category:",
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                var categoryLabel = new Label();
                categoryLabel.SetBinding(Label.TextProperty, "Category");
                dateStack.Add(categoryLabel);

                dateStack.Add(new Label
                {
                    Text = "Event:",
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                var eventLabel = new Label();
                eventLabel.SetBinding(Label.TextProperty, "Event");
                dateStack.Add(eventLabel);

                Grid.SetRow(dateStack, 3);
                Grid.SetColumn(dateStack, 0);
                grid.Add(dateStack);

                // Buttons column
                var buttonStack = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center
                };

                // View Receipt button
                var receiptButton = new Button { Text = "View Receipt" };
                receiptButton.SetBinding(IsVisibleProperty, "HasReceipt");
                receiptButton.Clicked += (s, e) => OnViewReceiptClicked(s, e);
                buttonStack.Add(receiptButton);

                // Approve button
                var approveButton = new Button
                {
                    Text = "Approve",
                    BackgroundColor = Colors.Green
                };
                approveButton.Clicked += (s, e) => OnApproveClicked(s, e);
                buttonStack.Add(approveButton);

                // Reject button
                var rejectButton = new Button
                {
                    Text = "Reject",
                    BackgroundColor = Colors.Red
                };
                rejectButton.Clicked += (s, e) => OnRejectClicked(s, e);
                buttonStack.Add(rejectButton);

                Grid.SetRow(buttonStack, 0);
                Grid.SetColumn(buttonStack, 1);
                Grid.SetRowSpan(buttonStack, 4);
                grid.Add(buttonStack);

                frame.Content = grid;
                viewCell.View = frame;

                return viewCell;
            });

            mainLayout.Add(IncomeListView);

            // Set the content of the page
            Content = mainLayout;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadPendingIncomeAsync();
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

                // Create a list of income view models with additional info
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
                        AmountString = $"? {income.Amount:N2}",
                        Category = income.Category.ToString(),
                        RequestedBy = officer?.Name ?? "Unknown",
                        DateRequested = income.CreatedDate.ToString("MMM dd, yyyy"),
                        Event = eventName,
                        HasReceipt = !string.IsNullOrEmpty(income.ReceiptPath),
                        ReceiptPath = income.ReceiptPath
                    });
                }

                // Fixed variable names
                IncomeListView.ItemsSource = pendingIncomeViewModels.OrderByDescending(e => e.DateRequested);

                if (pendingIncomeViewModels.Count == 0)
                {
                    NoIncomeLabel.IsVisible = true;
                    IncomeListView.IsVisible = false;
                }
                else
                {
                    NoIncomeLabel.IsVisible = false;
                    IncomeListView.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load income requests: {ex.Message}", "OK");
            }
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var income = button?.BindingContext as IncomeViewModel;

            if (income != null)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to approve this income: {income.Detail} - {income.AmountString}?",
                    "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _transactionService.UpdateTransactionApprovalAsync(
                            income.Id, ApprovalStatus.Approved, _authService.CurrentUser.Id);

                        await DisplayAlert("Success", "Income approved successfully", "OK");
                        await LoadPendingIncomeAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to approve income: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var income = button?.BindingContext as IncomeViewModel;

            if (income != null)
            {
                bool confirm = await DisplayAlert("Confirm",
                    $"Are you sure you want to reject this income: {income.Detail} - {income.AmountString}?",
                    "Yes", "No");

                if (confirm)
                {
                    try
                    {
                        await _transactionService.UpdateTransactionApprovalAsync(
                            income.Id, ApprovalStatus.Rejected, _authService.CurrentUser.Id);

                        await DisplayAlert("Success", "Income rejected successfully", "OK");
                        await LoadPendingIncomeAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to reject income: {ex.Message}", "OK");
                    }
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
                        // Navigate to receipt viewer page
                        await Navigation.PushAsync(new ReceiptViewerPage(income.Detail, receiptData));
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

    // Helper class for the income list view
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