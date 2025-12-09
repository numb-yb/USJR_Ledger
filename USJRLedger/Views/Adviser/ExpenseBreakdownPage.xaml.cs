using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.Views.Adviser
{
    public partial class ExpenseBreakdownPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly string _organizationId;

        private List<Transaction> _allTransactions = new List<Transaction>();
        private List<SchoolYear> _allSchoolYears = new List<SchoolYear>();

        public ExpenseBreakdownPage(AuthService authService, DataService dataService, string organizationId)
        {
            InitializeComponent();
            _authService = authService;
            _dataService = dataService;
            _organizationId = organizationId;

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                var loadedYears = await _dataService.LoadFromFileAsync<SchoolYear>("schoolyears.json");

                // Load ALL Expenses for this Org (Approved & Rejected)
                _allTransactions = transactions
                    .Where(t => t.OrganizationId == _organizationId &&
                                t.Type == TransactionType.Expense)
                    .ToList();

                _allSchoolYears = loadedYears
                    .Where(sy => sy.OrganizationId == _organizationId)
                    .OrderByDescending(sy => sy.StartDate)
                    .ToList();

                // Get DISTINCT Years (e.g. "2026-2027")
                var distinctYears = _allSchoolYears
                    .Select(sy => sy.Year)
                    .Distinct()
                    .ToList();

                SchoolYearPicker.ItemsSource = distinctYears;

                // Auto-select active year
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
                    GrandTotalLabel.Text = "\u20B1 0.00";
                    GeneralTotalLabel.Text = "\u20B1 0.00";
                    EventTotalLabel.Text = "\u20B1 0.00";
                    RejectedExpensesList.ItemsSource = null;
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

            // 2. Filter transactions
            var yearTransactions = _allTransactions
                .Where(t => matchingSyIds.Contains(t.SchoolYearId))
                .ToList();

            // 3. Separate APPROVED expenses
            var approvedExpenses = yearTransactions
                .Where(t => t.ApprovalStatus == ApprovalStatus.Approved)
                .ToList();

            // 4. Update Rejected Expenses List
            var rejectedExpenses = yearTransactions
                .Where(t => t.ApprovalStatus == ApprovalStatus.Rejected)
                .Select(t => new RejectedExpenseSummary
                {
                    Detail = t.Detail,
                    Amount = t.Amount,
                    DateString = t.CreatedDate.ToString("MMM dd, yyyy"),
                    Category = t.Category.ToString()
                })
                .OrderByDescending(r => r.DateString)
                .ToList();

            RejectedExpensesList.ItemsSource = rejectedExpenses;

            // 5. Update Totals (Cards)
            decimal grandTotal = approvedExpenses.Sum(t => t.Amount);
            GrandTotalLabel.Text = $"\u20B1 {grandTotal:N2}";

            var generalExpenses = approvedExpenses
                .Where(t => t.Category == TransactionCategory.General || string.IsNullOrEmpty(t.EventId))
                .Sum(t => t.Amount);

            GeneralTotalLabel.Text = $"\u20B1 {generalExpenses:N2}";

            var eventExpenses = approvedExpenses
                .Where(t => !string.IsNullOrEmpty(t.EventId) && t.Category == TransactionCategory.Event)
                .Sum(t => t.Amount);

            EventTotalLabel.Text = $"\u20B1 {eventExpenses:N2}";
        }

        // --- NAVIGATION HANDLERS ---

        private async void OnGeneralExpenseTapped(object sender, EventArgs e)
        {
            var selectedYearString = SchoolYearPicker.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedYearString)) return;

            var matchingSyIds = _allSchoolYears
                .Where(sy => sy.Year == selectedYearString)
                .Select(sy => sy.Id)
                .ToList();

            if (matchingSyIds.Any())
            {
                await Navigation.PushAsync(new GeneralExpensesListPage(_dataService, _organizationId, selectedYearString, matchingSyIds));
            }
        }

        private async void OnEventExpenseSummaryTapped(object sender, EventArgs e)
        {
            var selectedYearString = SchoolYearPicker.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedYearString)) return;

            var matchingSyIds = _allSchoolYears
                .Where(sy => sy.Year == selectedYearString)
                .Select(sy => sy.Id)
                .ToList();

            if (matchingSyIds.Any())
            {
                await Navigation.PushAsync(new EventExpensesListPage(_dataService, _organizationId, selectedYearString, matchingSyIds));
            }
        }
    }

    public class RejectedExpenseSummary
    {
        public string Detail { get; set; }
        public decimal Amount { get; set; }
        public string DateString { get; set; }
        public string Category { get; set; }
    }
}