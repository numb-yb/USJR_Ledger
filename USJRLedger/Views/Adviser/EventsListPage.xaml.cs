using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Adviser
{
    public partial class EventsListPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;
        private readonly string _organizationId;
        private SchoolYear _activeSchoolYear;
        private List<Event> _events;

        public EventsListPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _eventService = new EventService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            LoadEventsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEventsAsync();
        }

        private async void LoadEventsAsync()
        {
            try
            {
                _activeSchoolYear = await _schoolYearService.GetActiveSchoolYearAsync(_organizationId);

                if (_activeSchoolYear != null)
                {
                    SchoolYearLabel.Text = $"School Year: {_activeSchoolYear.Semester} {_activeSchoolYear.Year}";

                    // Load events for the active school year
                    _events = await _eventService.GetEventsBySchoolYearAsync(_activeSchoolYear.Id);

                    // Create event view models with event balance
                    var eventViewModels = new List<EventViewModel>();

                    foreach (var eventItem in _events)
                    {
                        decimal balance = await _eventService.GetEventBalanceAsync(eventItem.Id);

                        eventViewModels.Add(new EventViewModel
                        {
                            Id = eventItem.Id,
                            Name = eventItem.Name,
                            EventDate = eventItem.EventDate.ToString("MMM dd, yyyy"),
                            CreatedDate = eventItem.CreatedDate.ToString("MMM dd, yyyy"),
                            Balance = $"₱ {balance:N2}",
                            BalanceAmount = balance
                        });
                    }

                    EventsListView.ItemsSource = eventViewModels.OrderByDescending(e => e.EventDate);

                    if (eventViewModels.Count == 0)
                    {
                        NoEventsLabel.IsVisible = true;
                        EventsListView.IsVisible = false;
                    }
                    else
                    {
                        NoEventsLabel.IsVisible = false;
                        EventsListView.IsVisible = true;
                    }
                }
                else
                {
                    SchoolYearLabel.Text = "No Active School Year";
                    NoEventsLabel.IsVisible = true;
                    EventsListView.IsVisible = false;
                }
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

    // Helper class for the events list view
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