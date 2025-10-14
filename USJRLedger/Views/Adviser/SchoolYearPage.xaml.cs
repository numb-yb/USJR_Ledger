using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Adviser
{
    public partial class SchoolYearPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly SchoolYearService _schoolYearService;
        private readonly string _organizationId;
        private List<SchoolYear> _schoolYears;
        private SchoolYear _activeSchoolYear;

        public SchoolYearPage(AuthService authService, DataService dataService)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _schoolYearService = new SchoolYearService(dataService);
            _organizationId = _authService.CurrentUser.OrganizationId;

            LoadSchoolYearsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSchoolYearsAsync();
        }

        private async Task LoadSchoolYearsAsync()
        {
            try
            {
                _schoolYears = await _schoolYearService.GetSchoolYearsByOrganizationAsync(_organizationId);
                _activeSchoolYear = _schoolYears.FirstOrDefault(sy => sy.IsActive);

                if (_activeSchoolYear != null)
                {
                    ActiveSchoolYearLabel.Text = $"{_activeSchoolYear.Semester} {_activeSchoolYear.Year}";
                    ActiveSchoolYearLabel.TextColor = Colors.Green;
                    StartDateLabel.Text = _activeSchoolYear.StartDate.ToString("MMMM dd, yyyy");

                    StartSchoolYearButton.IsVisible = false;
                    EndSchoolYearButton.IsVisible = true;
                }
                else
                {
                    ActiveSchoolYearLabel.Text = "No Active School Year";
                    ActiveSchoolYearLabel.TextColor = Colors.Red;
                    StartDateLabel.Text = "-";

                    StartSchoolYearButton.IsVisible = true;
                    EndSchoolYearButton.IsVisible = false;
                }

                SchoolYearsListView.ItemsSource = _schoolYears.OrderByDescending(sy => sy.StartDate);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load school years: {ex.Message}", "OK");
            }
        }

        private async void OnStartSchoolYearClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SemesterEntry.Text) ||
                string.IsNullOrWhiteSpace(YearEntry.Text))
            {
                await DisplayAlert("Error", "Please enter both semester and year", "OK");
                return;
            }

            string semester = SemesterEntry.Text;
            string year = YearEntry.Text;

            try
            {
                await _schoolYearService.StartSchoolYearAsync(_organizationId, semester, year);
                await DisplayAlert("Success", "School year started successfully", "OK");

                // Clear form and reload
                SemesterEntry.Text = string.Empty;
                YearEntry.Text = string.Empty;
                await LoadSchoolYearsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to start school year: {ex.Message}", "OK");
            }
        }

        private async void OnEndSchoolYearClicked(object sender, EventArgs e)
        {
            if (_activeSchoolYear == null)
            {
                await DisplayAlert("Error", "No active school year to end", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirm",
                $"Are you sure you want to end the current school year ({_activeSchoolYear.Semester} {_activeSchoolYear.Year})?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _schoolYearService.EndSchoolYearAsync(_activeSchoolYear.Id);
                    await DisplayAlert("Success", "School year ended successfully", "OK");
                    await LoadSchoolYearsAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to end school year: {ex.Message}", "OK");
                }
            }
        }
    }
}