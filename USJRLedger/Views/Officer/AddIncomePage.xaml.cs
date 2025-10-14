using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Officer
{
    public partial class AddIncomePage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly TransactionService _transactionService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;
        private SchoolYear _currentSchoolYear;
        private string _organizationId;
        private List<Event> _events;

        public AddIncomePage(AuthService authService, DataService dataService)
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

            var eventItems = new List<string> { "General Income (No Event)" };
            eventItems.AddRange(_events.Select(e => e.Name));

            EventPicker.ItemsSource = eventItems;
            EventPicker.SelectedIndex = 0;
        }

        private void OnCategoryChanged(object sender, EventArgs e)
        {
            bool isEventIncome = CategoryPicker.SelectedIndex == 1; // "Event" option
            EventPickerStack.IsVisible = isEventIncome;
        }

        private async void OnAddIncomeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DetailEntry.Text) || !decimal.TryParse(AmountEntry.Text, out decimal amount))
            {
                await DisplayAlert("Error", "Please enter valid income details and amount", "OK");
                return;
            }

            string detail = DetailEntry.Text;
            TransactionCategory category = CategoryPicker.SelectedIndex == 0 ?
                TransactionCategory.General : TransactionCategory.Event;

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
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        receiptData = memoryStream.ToArray();
                        receiptFileName = ReceiptFileResult.FileName;
                    }
                }
            }

            try
            {
                await _transactionService.AddIncomeAsync(
                    _organizationId,
                    _currentSchoolYear.Id,
                    eventId,
                    category,
                    detail,
                    amount,
                    receiptData,
                    receiptFileName,
                    _authService.CurrentUser.Id);

                await DisplayAlert("Success", "Income added successfully", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add income: {ex.Message}", "OK");
            }
        }

        private async void OnSelectReceiptClicked(object sender, EventArgs e)
        {
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
                    SelectReceiptButton.Text = "Change Receipt";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to select file: {ex.Message}", "OK");
            }
        }

        private FileResult ReceiptFileResult { get; set; }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}