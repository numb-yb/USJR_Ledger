using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class OrganizationService
    {
        private readonly DataService _dataService;

        public OrganizationService(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<List<Organization>> GetAllOrganizationsAsync()
        {
            return await _dataService.LoadFromFileAsync<Organization>("organizations.json");
        }

        public async Task<Organization> GetOrganizationByIdAsync(string id)
        {
            var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
            return organizations.FirstOrDefault(o => o.Id == id);
        }

        public async Task<Organization> CreateOrganizationAsync(string name, string department)
        {
            var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");

            var newOrg = new Organization
            {
                Name = name,
                Department = department
            };

            organizations.Add(newOrg);
            await _dataService.SaveToFileAsync(organizations, "organizations.json");

            return newOrg;
        }

        public async Task UpdateOrganizationStatusAsync(string id, bool isActive)
        {
            var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
            var org = organizations.FirstOrDefault(o => o.Id == id);

            if (org != null)
            {
                org.IsActive = isActive;
                if (!isActive)
                {
                    org.DeactivationDate = DateTime.Now;
                }
                else
                {
                    org.DeactivationDate = null;
                }

                await _dataService.SaveToFileAsync(organizations, "organizations.json");
            }
        }

        public async Task<decimal> GetOrganizationBalanceAsync(string orgId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var orgTransactions = transactions.Where(t => t.OrganizationId == orgId && t.ApprovalStatus == ApprovalStatus.Approved);

            decimal totalIncome = orgTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal totalExpense = orgTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            return totalIncome - totalExpense;
        }

        public async Task<(List<Transaction> GeneralIncome, List<Transaction> EventIncome,
                          List<Transaction> GeneralExpenses, List<Transaction> EventExpenses)>
            GetOrganizationTransactionsAsync(string orgId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var orgTransactions = transactions.Where(t => t.OrganizationId == orgId && t.ApprovalStatus == ApprovalStatus.Approved);

            var generalIncome = orgTransactions.Where(t => t.Type == TransactionType.Income &&
                                                          t.Category == TransactionCategory.General).ToList();

            var eventIncome = orgTransactions.Where(t => t.Type == TransactionType.Income &&
                                                       t.Category == TransactionCategory.Event).ToList();

            var generalExpenses = orgTransactions.Where(t => t.Type == TransactionType.Expense &&
                                                           t.Category == TransactionCategory.General).ToList();

            var eventExpenses = orgTransactions.Where(t => t.Type == TransactionType.Expense &&
                                                         t.Category == TransactionCategory.Event).ToList();

            return (generalIncome, eventIncome, generalExpenses, eventExpenses);
        }
    }
}