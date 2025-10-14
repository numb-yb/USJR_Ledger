using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Officer
{
    public partial class CreateEventPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;
        private SchoolYear _currentSchoolYear;
        private string _organizationId;

        public CreateEventPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _eventService = new EventService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            LoadSchoolYearAsync();
        }

        private async void LoadSchoolYearAsync()
        {
            _currentSchoolYear = await _schoolYearService.GetActiveSchoolYearAsync(_organizationId);
            if (_currentSchoolYear != null)
            {
                SchoolYearLabel.Text = $"School Year: {_currentSchoolYear.Semester} {_currentSchoolYear.Year}";
            }
            else
            {
                await DisplayAlert("Error", "No active school year found", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void OnCreateEventClicked(object sender, EventArgs e)
        {
            string eventName = EventNameEntry.Text;
            DateTime eventDate = EventDatePicker.Date;

            if (string.IsNullOrWhiteSpace(eventName))
            {
                await DisplayAlert("Error", "Please enter an event name", "OK");
                return;
            }

            try
            {
                await _eventService.CreateEventAsync(
                    _organizationId,
                    _currentSchoolYear.Id,
                    eventName,
                    eventDate,
                    _authService.CurrentUser.Id);

                await DisplayAlert("Success", "Event created successfully", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create event: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}