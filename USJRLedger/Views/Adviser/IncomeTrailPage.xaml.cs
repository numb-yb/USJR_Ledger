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
        private readonly UserService _userService;
        private readonly string _organizationId;

        // Data Caches
        private List<Transaction> _allIncome = new List<Transaction>();
        private List<SchoolYear> _allSchoolYears = new List<SchoolYear>();
        private Dictionary<string, string> _userNames = new Dictionary<string, string>(); // Map ID -> Name

        public IncomeTrailPage(string organizationId)
        {
            InitializeComponent();
            _organizationId = organizationId;
            _userService = new UserService(_dataService); // Init User Service

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var loadedYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

                // 1. Load Users to get names (Optimization: Load once)
                var users = await _userService.GetUsersByOrganizationAsync(_organizationId);
                _userNames = users.ToDictionary(u => u.Id, u => u.Name);

                // 2. Load ALL Income for this Org
                _allIncome = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Income &&
                                t.ApprovalStatus == ApprovalStatus.Approved)
                    .ToList();

                // 3. Load ALL School Years
                _allSchoolYears = loadedYears
                    .Where(sy => sy.OrganizationId == _organizationId)
                    .OrderByDescending(sy => sy.StartDate)
                    .ToList();

                // 4. Populate Picker
                var distinctYears = _allSchoolYears
                    .Select(sy => sy.Year)
                    .Distinct()
                    .ToList();

                SchoolYearPicker.ItemsSource = distinctYears;

                // 5. Auto-select active year
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

            // 1. Find matching IDs
            var matchingSyIds = _allSchoolYears
                .Where(sy => sy.Year == selectedYearString)
                .Select(sy => sy.Id)
                .ToList();

            // 2. Filter Transactions
            var yearIncome = _allIncome
                .Where(t => matchingSyIds.Contains(t.SchoolYearId))
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            // 3. Map to Display Item (VM)
            var displayList = new List<IncomeTrailViewModel>();

            foreach (var item in yearIncome)
            {
                // Lookup name from dictionary, default to "Unknown"
                string senderName = _userNames.ContainsKey(item.CreatedBy)
                    ? _userNames[item.CreatedBy]
                    : "Unknown";

                displayList.Add(new IncomeTrailViewModel
                {
                    Detail = item.Detail,
                    AmountString = $"\u20B1 {item.Amount:N2}", // Formatted Amount
                    DateString = item.CreatedDate.ToString("MMM dd, yyyy"), // Formatted Date
                    SenderName = senderName
                });
            }

            IncomeTrailList.ItemsSource = displayList;
        }
    }

    // Helper Class for the List UI
    public class IncomeTrailViewModel
    {
        public string Detail { get; set; }
        public string AmountString { get; set; }
        public string DateString { get; set; }
        public string SenderName { get; set; }
    }
}