using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Officer
{
    public partial class AddExpensePage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly TransactionService _transactionService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;
        private SchoolYear _currentSchoolYear;
        private string _organizationId;
        private List<Event> _events;
        private FileResult ReceiptFileResult;

        public AddExpensePage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _transactionService = new TransactionService(dataService);
            _eventService = new EventService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            _currentSchoolYear = await _schoolYearService.GetActiveSchoolYearAsync(_organizationId);

            if (_currentSchoolYear == null)
            {
                await DisplayAlert("Error", "No active school year found", "OK");
                await Navigation.PopAsync();
                return;
            }

            SchoolYearLabel.Text = $"School Year: {_currentSchoolYear.Semester} {_currentSchoolYear.Year}";

            _events = await _eventService.GetEventsBySchoolYearAsync(_currentSchoolYear.Id);

            var eventItems = new List<string> { "General Expense (No Event)" };
            eventItems.AddRange(_events.Select(e => e.Name));

            EventPicker.ItemsSource = eventItems;
            EventPicker.SelectedIndex = 0;
        }

        private void OnCategoryChanged(object sender, EventArgs e)
        {
            EventPickerStack.IsVisible = CategoryPicker.SelectedIndex == 1; // Event Expense
        }

        private async void OnSelectReceiptClicked(object sender, EventArgs e)
        {
            if (ReceiptFileResult != null)
            {
                // Allow user to remove the current receipt
                var confirm = await DisplayAlert("Remove Receipt?", "Do you want to remove the selected receipt?", "Yes", "No");
                if (confirm)
                {
                    ReceiptFileResult = null;
                    ReceiptFileLabel.Text = "";
                    ReceiptPreview.IsVisible = false;
                    SelectReceiptButton.Text = "Select Receipt";
                }
                return;
            }

            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Select Receipt Image"
                });

                if (result != null)
                {
                    ReceiptFileResult = result;
                    ReceiptFileLabel.Text = result.FileName;
                    SelectReceiptButton.Text = "Remove Receipt";

                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    //Important: clone the stream so it's not disposed
                    ReceiptPreview.Source = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
                    ReceiptPreview.IsVisible = true;
                }

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to select file: {ex.Message}", "OK");
            }
        }

        private async void OnAddExpenseClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DetailEntry.Text) || !decimal.TryParse(AmountEntry.Text, out decimal amount))
            {
                await DisplayAlert("Error", "Please enter valid expense details and amount.", "OK");
                return;
            }

            if (ReceiptFileResult == null)
            {
                bool proceed = await DisplayAlert(
                    "No Receipt Attached",
                    "You did not attach a receipt. Do you want to continue without one?",
                    "Yes", "No");

                if (!proceed)
                    return;
            }


            string detail = DetailEntry.Text;
            TransactionCategory category = CategoryPicker.SelectedIndex == 0
                ? TransactionCategory.General
                : TransactionCategory.Event;

            string eventId = null;
            if (category == TransactionCategory.Event && EventPicker.SelectedIndex > 0)
            {
                eventId = _events[EventPicker.SelectedIndex - 1].Id;
            }

            byte[] receiptData = null;
            string receiptFileName = null;

            if (ReceiptFileResult != null)
            {
                using (var stream = await ReceiptFileResult.OpenReadAsync())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    receiptData = memoryStream.ToArray();
                    receiptFileName = ReceiptFileResult.FileName;
                }
            }


            try
            {
                await _transactionService.AddExpenseAsync(
                    _organizationId,
                    _currentSchoolYear.Id,
                    eventId,
                    category,
                    detail,
                    amount,
                    receiptData,
                    receiptFileName,
                    _authService.CurrentUser.Id);

                await DisplayAlert("Success", "Expense added successfully. Pending approval.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add expense: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
