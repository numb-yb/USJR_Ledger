using USJRLedger.Models;
using USJRLedger.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace USJRLedger.Views.Adviser
{
    public partial class IncomeTrailPage : ContentPage
    {
        private readonly DataService _dataService = new DataService();
        private readonly string _organizationId;

        // Cache lists
        private List<Transaction> _allIncome = new List<Transaction>();
        private List<SchoolYear> _allSchoolYears = new List<SchoolYear>();

        public IncomeTrailPage(string organizationId)
        {
            InitializeComponent();
            _organizationId = organizationId;
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var loadedYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

                // 1. Load ALL Income for this Org
                _allIncome = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Income &&
                                t.ApprovalStatus == ApprovalStatus.Approved)
                    .ToList();

                // 2. Load ALL School Years
                _allSchoolYears = loadedYears
                    .Where(sy => sy.OrganizationId == _organizationId)
                    .OrderByDescending(sy => sy.StartDate)
                    .ToList();

                // 3. Populate Picker with DISTINCT Years (e.g. "2026-2027")
                var distinctYears = _allSchoolYears
                    .Select(sy => sy.Year)
                    .Distinct()
                    .ToList();

                SchoolYearPicker.ItemsSource = distinctYears;

                // 4. Auto-select active year
                var activeSy = _allSchoolYears.FirstOrDefault(sy => sy.IsActive);
                if (activeSy != null)
                {
                    SchoolYearPicker.SelectedItem = activeSy.Year;
                }
                else if (distinctYears.Any())
                {
                    SchoolYearPicker.SelectedIndex = 0;
                }
                else
                {
                    IncomeTrailList.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Could not load data: " + ex.Message, "OK");
            }
        }

        private void OnSchoolYearChanged(object sender, EventArgs e)
        {
            var selectedYearString = SchoolYearPicker.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedYearString)) return;

            // 1. Find ALL matching SchoolYear IDs for this string (Merging semesters)
            var matchingSyIds = _allSchoolYears
                .Where(sy => sy.Year == selectedYearString)
                .Select(sy => sy.Id)
                .ToList();

            // 2. Filter income transactions belonging to ANY of these IDs
            var yearIncome = _allIncome
                .Where(t => matchingSyIds.Contains(t.SchoolYearId))
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            IncomeTrailList.ItemsSource = yearIncome;
        }
    }
}