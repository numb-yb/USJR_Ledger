using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Adviser
{
    public partial class IncomeTrailPage : ContentPage
    {
        private readonly string _organizationId;
        private readonly DataService _dataService = new DataService();

        // We store the full list of objects in memory so we can access IDs later
        private List<Transaction> _allTransactions = new List<Transaction>();
        private List<SchoolYear> _schoolYears = new List<SchoolYear>();

        public IncomeTrailPage(string organizationId)
        {
            InitializeComponent(); // This loads the XAML. If the XAML is wrong, this is where errors start.
            _organizationId = organizationId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            // 1. Load School Years first
            var allYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

            // Filter only years for this org (or global ones if they are shared)
            // and sort by StartDate (Newest first)
            _schoolYears = allYears
                .Where(sy => sy.OrganizationId == _organizationId)
                .OrderByDescending(sy => sy.StartDate)
                .ToList();

            // 2. Load Transactions
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            _allTransactions = transactions
                .Where(t => t.OrganizationId == _organizationId &&
                            t.Type == TransactionType.Income &&
                            t.ApprovalStatus == ApprovalStatus.Approved)
                .ToList();

            // 3. Populate the Picker with readable names
            // We create a list of strings like "2023-2024 - 1st Semester"
            var pickerList = _schoolYears
                .Select(sy => $"{sy.Year} - {sy.Semester}")
                .ToList();

            SchoolYearPicker.ItemsSource = pickerList;

            // 4. Auto-select the active one or the first one
            if (_schoolYears.Any())
            {
                // Try to find the active year, otherwise pick the first one
                var activeYearIndex = _schoolYears.FindIndex(sy => sy.IsActive);
                SchoolYearPicker.SelectedIndex = (activeYearIndex >= 0) ? activeYearIndex : 0;
            }
            else
            {
                IncomeTrailList.ItemsSource = null;
            }
        }

        private void OnSchoolYearChanged(object sender, EventArgs e)
        {
            // Safety check
            if (SchoolYearPicker.SelectedIndex == -1 || !_schoolYears.Any()) return;

            // 1. Get the actual SchoolYear object based on the index selected
            // (The index in the picker matches the index in our _schoolYears list)
            var selectedSchoolYear = _schoolYears[SchoolYearPicker.SelectedIndex];

            // 2. Filter transactions that match that SchoolYear ID
            var filteredList = _allTransactions
                .Where(t => t.SchoolYearId == selectedSchoolYear.Id)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            // 3. Fallback for empty details (UI Cleanup)
            foreach (var item in filteredList)
            {
                if (string.IsNullOrEmpty(item.Detail))
                    item.Detail = item.Category.ToString();
            }

            IncomeTrailList.ItemsSource = filteredList;
        }
    }
}