using USJRLedger.Models;
using USJRLedger.Services;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace USJRLedger.Views.Adviser
{
    public partial class EventsListPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;
        private readonly string _organizationId;

        // Cache full list of school year objects
        private List<SchoolYear> _allSchoolYears = new List<SchoolYear>();

        public EventsListPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _eventService = new EventService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // 1. Load All School Years for this Org
                var loadedYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

                _allSchoolYears = loadedYears
                    .Where(sy => sy.OrganizationId == _organizationId)
                    .OrderByDescending(sy => sy.StartDate)
                    .ToList();

                // 2. Populate Picker with DISTINCT Years only (e.g. "2026-2027")
                // We group by the 'Year' string property to avoid duplicate entries for semesters
                var distinctYears = _allSchoolYears
                    .Select(sy => sy.Year)
                    .Distinct()
                    .ToList();

                SchoolYearPicker.ItemsSource = distinctYears;

                // 3. Auto-select active year
                // Find the year string that corresponds to the currently active semester
                var activeSy = _allSchoolYears.FirstOrDefault(sy => sy.IsActive);
                if (activeSy != null)
                {
                    SchoolYearPicker.SelectedItem = activeSy.Year;
                }
                else if (distinctYears.Any())
                {
                    SchoolYearPicker.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async void OnSchoolYearChanged(object sender, EventArgs e)
        {
            var selectedYearString = SchoolYearPicker.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedYearString)) return;

            try
            {
                // 1. Find ALL SchoolYear IDs that match this string
                // (e.g., fetch IDs for both "1st Sem 2026-2027" and "2nd Sem 2026-2027")
                var matchingSchoolYearIds = _allSchoolYears
                    .Where(sy => sy.Year == selectedYearString)
                    .Select(sy => sy.Id)
                    .ToList();

                var eventViewModels = new List<EventViewModel>();

                // 2. Load events for ALL matching semesters
                foreach (var syId in matchingSchoolYearIds)
                {
                    var events = await _eventService.GetEventsBySchoolYearAsync(syId);

                    foreach (var eventItem in events)
                    {
                        decimal balance = await _eventService.GetEventBalanceAsync(eventItem.Id);

                        eventViewModels.Add(new EventViewModel
                        {
                            Id = eventItem.Id,
                            Name = eventItem.Name,
                            EventDate = eventItem.EventDate.ToString("MMM dd, yyyy"),
                            CreatedDate = eventItem.CreatedDate.ToString("MMM dd, yyyy"),
                            Balance = $"₱{balance:N2}",
                            BalanceAmount = balance
                        });
                    }
                }

                // 3. Display combined list sorted by date
                EventsCollectionView.ItemsSource = eventViewModels
                    .OrderByDescending(ev => ev.EventDate) // Assuming EventDate string sorts correctly, better to use DateTime in VM if sorting is strict
                    .ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load events: {ex.Message}", "OK");
            }
        }

        private async void OnViewEventDetailsClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var eventViewModel = button?.BindingContext as EventViewModel;

            if (eventViewModel != null)
            {
                await Navigation.PushAsync(new EventDetailsPage(_authService, _dataService, eventViewModel.Id));
            }
        }
    }

    public class EventViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string EventDate { get; set; }
        public string CreatedDate { get; set; }
        public string Balance { get; set; }
        public decimal BalanceAmount { get; set; }
    }
}