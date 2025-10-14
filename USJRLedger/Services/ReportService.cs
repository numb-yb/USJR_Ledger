using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class ReportService
    {
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private readonly UserService _userService;
        private readonly EventService _eventService;
        private readonly SchoolYearService _schoolYearService;

        public ReportService(DataService dataService)
        {
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
            _userService = new UserService(dataService);
            _eventService = new EventService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
        }

        public async Task<string> GenerateGeneralStatementAsync(string organizationId)
        {
            var organization = await _organizationService.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
                return "Organization not found";

            var sb = new StringBuilder();

            sb.AppendLine($"USJR ORGANIZATION LEDGER - GENERAL STATEMENT");
            sb.AppendLine($"Organization: {organization.Name}");
            sb.AppendLine($"Department: {organization.Department}");
            sb.AppendLine($"Date Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
            sb.AppendLine(new string('-', 80));

            var balance = await _organizationService.GetOrganizationBalanceAsync(organizationId);
            sb.AppendLine($"Current Balance: ₱ {balance:N2}");
            sb.AppendLine();

            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var orgTransactions = transactions
                .Where(t => t.OrganizationId == organizationId && t.ApprovalStatus == ApprovalStatus.Approved)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            decimal incomeTotal = 0;
            decimal expenseTotal = 0;

            sb.AppendLine("INCOME:");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Date",-12} {"Category",-10} {"Detail",-30} {"Amount",15}");
            sb.AppendLine(new string('-', 80));

            foreach (var income in orgTransactions.Where(t => t.Type == TransactionType.Income))
            {
                string category = income.Category == TransactionCategory.General ? "General" : "Event";
                sb.AppendLine($"{income.CreatedDate:MM/dd/yyyy}  {category,-10} {income.Detail,-30} {income.Amount,15:N2}");
                incomeTotal += income.Amount;
            }

            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Total Income:",-54} {incomeTotal,15:N2}");
            sb.AppendLine();

            sb.AppendLine("EXPENSES:");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Date",-12} {"Category",-10} {"Detail",-30} {"Amount",15}");
            sb.AppendLine(new string('-', 80));

            foreach (var expense in orgTransactions.Where(t => t.Type == TransactionType.Expense))
            {
                string category = expense.Category == TransactionCategory.General ? "General" : "Event";
                sb.AppendLine($"{expense.CreatedDate:MM/dd/yyyy}  {category,-10} {expense.Detail,-30} {expense.Amount,15:N2}");
                expenseTotal += expense.Amount;
            }

            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Total Expenses:",-54} {expenseTotal,15:N2}");
            sb.AppendLine();
            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"{"Net Balance:",-54} {(incomeTotal - expenseTotal),15:N2}");

            return sb.ToString();
        }

        public async Task<string> GenerateEventStatementAsync(string eventId)
        {
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            if (eventItem == null)
                return "Event not found";

            var organization = await _organizationService.GetOrganizationByIdAsync(eventItem.OrganizationId);
            if (organization == null)
                return "Organization not found";

            var sb = new StringBuilder();

            sb.AppendLine($"USJR ORGANIZATION LEDGER - EVENT STATEMENT");
            sb.AppendLine($"Organization: {organization.Name}");
            sb.AppendLine($"Department: {organization.Department}");
            sb.AppendLine($"Event: {eventItem.Name}");
            sb.AppendLine($"Event Date: {eventItem.EventDate:MMMM dd, yyyy}");
            sb.AppendLine($"Date Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
            sb.AppendLine(new string('-', 80));

            var balance = await _eventService.GetEventBalanceAsync(eventId);
            sb.AppendLine($"Event Balance: ₱ {balance:N2}");
            sb.AppendLine();

            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var eventTransactions = transactions
                .Where(t => t.EventId == eventId && t.ApprovalStatus == ApprovalStatus.Approved)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            decimal incomeTotal = 0;
            decimal expenseTotal = 0;

            sb.AppendLine("INCOME:");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Date",-12} {"Detail",-50} {"Amount",15}");
            sb.AppendLine(new string('-', 80));

            foreach (var income in eventTransactions.Where(t => t.Type == TransactionType.Income))
            {
                sb.AppendLine($"{income.CreatedDate:MM/dd/yyyy}  {income.Detail,-50} {income.Amount,15:N2}");
                incomeTotal += income.Amount;
            }

            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Total Income:",-64} {incomeTotal,15:N2}");
            sb.AppendLine();

            sb.AppendLine("EXPENSES:");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Date",-12} {"Detail",-50} {"Amount",15}");
            sb.AppendLine(new string('-', 80));

            foreach (var expense in eventTransactions.Where(t => t.Type == TransactionType.Expense))
            {
                sb.AppendLine($"{expense.CreatedDate:MM/dd/yyyy}  {expense.Detail,-50} {expense.Amount,15:N2}");
                expenseTotal += expense.Amount;
            }

            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"{"Total Expenses:",-64} {expenseTotal,15:N2}");
            sb.AppendLine();
            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"{"Net Balance:",-64} {(incomeTotal - expenseTotal),15:N2}");

            return sb.ToString();
        }

        public async Task<string> GenerateLedgerReportAsync(string organizationId, string schoolYearId = null)
        {
            var organization = await _organizationService.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
                return "Organization not found";

            SchoolYear schoolYear = null;
            if (!string.IsNullOrEmpty(schoolYearId))
            {
                schoolYear = await _schoolYearService.GetSchoolYearByIdAsync(schoolYearId);
            }
            else
            {
                schoolYear = await _schoolYearService.GetActiveSchoolYearAsync(organizationId);
            }

            var sb = new StringBuilder();

            sb.AppendLine($"USJR ORGANIZATION LEDGER REPORT");
            sb.AppendLine($"Organization: {organization.Name}");
            sb.AppendLine($"Department: {organization.Department}");

            if (schoolYear != null)
            {
                sb.AppendLine($"School Year: {schoolYear.Semester} {schoolYear.Year}");
                sb.AppendLine($"Period: {schoolYear.StartDate:MMMM dd, yyyy} - {(schoolYear.EndDate.HasValue ? schoolYear.EndDate.Value.ToString("MMMM dd, yyyy") : "Present")}");
            }

            sb.AppendLine($"Date Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}");
            sb.AppendLine(new string('=', 80));

            // Get transactions
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            IEnumerable<Transaction> filteredTransactions;

            if (schoolYear != null)
            {
                filteredTransactions = transactions
                    .Where(t => t.OrganizationId == organizationId &&
                               t.SchoolYearId == schoolYear.Id &&
                               t.ApprovalStatus == ApprovalStatus.Approved);
            }
            else
            {
                filteredTransactions = transactions
                    .Where(t => t.OrganizationId == organizationId &&
                               t.ApprovalStatus == ApprovalStatus.Approved);
            }

            var sortedTransactions = filteredTransactions.OrderBy(t => t.CreatedDate).ToList();

            // Generate ledger table
            sb.AppendLine($"{"Date",-12} {"Type",-8} {"Category",-10} {"Detail",-30} {"Debit",10} {"Credit",10} {"Balance",14}");
            sb.AppendLine(new string('-', 80));

            decimal runningBalance = 0;

            foreach (var transaction in sortedTransactions)
            {
                string type = transaction.Type == TransactionType.Income ? "Income" : "Expense";
                string category = transaction.Category == TransactionCategory.General ? "General" : "Event";

                decimal debit = transaction.Type == TransactionType.Expense ? transaction.Amount : 0;
                decimal credit = transaction.Type == TransactionType.Income ? transaction.Amount : 0;

                runningBalance += credit - debit;

                sb.AppendLine($"{transaction.CreatedDate:MM/dd/yyyy}  {type,-8} {category,-10} {transaction.Detail,-30} {debit,10:N2} {credit,10:N2} {runningBalance,14:N2}");
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"{"Final Balance:",-62} {runningBalance,14:N2}");

            return sb.ToString();
        }

        public async Task SaveReportToFileAsync(string reportContent, string filename)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string reportsPath = Path.Combine(documentsPath, "USJRLedger", "Reports");

            Directory.CreateDirectory(reportsPath);

            string fullPath = Path.Combine(reportsPath, filename);
            await File.WriteAllTextAsync(fullPath, reportContent);
        }
    }
}