using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class TransactionService
    {
        private readonly DataService _dataService;

        public TransactionService(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
        }

        public async Task<List<Transaction>> GetTransactionsByOrganizationAsync(string organizationId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            return transactions.Where(t => t.OrganizationId == organizationId).ToList();
        }

        public async Task<List<Transaction>> GetTransactionsByEventAsync(string eventId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            return transactions.Where(t => t.EventId == eventId).ToList();
        }

        public async Task<List<Transaction>> GetTransactionsBySchoolYearAsync(string schoolYearId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            return transactions.Where(t => t.SchoolYearId == schoolYearId).ToList();
        }

        public async Task<List<Transaction>> GetPendingExpensesAsync(string organizationId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            return transactions.Where(t => t.OrganizationId == organizationId &&
                                          t.Type == TransactionType.Expense &&
                                          t.ApprovalStatus == ApprovalStatus.Pending)
                              .ToList();
        }

        public async Task<List<Transaction>> GetPendingIncomeAsync(string organizationId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            return transactions.Where(t => t.OrganizationId == organizationId &&
                                          t.Type == TransactionType.Income &&
                                          t.ApprovalStatus == ApprovalStatus.Pending)
                              .ToList();
        }

        public async Task<Transaction> AddExpenseAsync(string organizationId, string schoolYearId,
                                                      string eventId, TransactionCategory category,
                                                      string detail, decimal amount,
                                                      byte[] receiptData, string receiptFileName,
                                                      string createdBy)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            string receiptPath = null;
            if (receiptData != null && receiptFileName != null)
            {
                receiptPath = await _dataService.SaveReceiptAsync(receiptData, receiptFileName);
            }

            var newExpense = new Transaction
            {
                OrganizationId = organizationId,
                SchoolYearId = schoolYearId,
                EventId = eventId,
                Type = TransactionType.Expense,
                Category = category,
                Detail = detail,
                Amount = amount,
                ReceiptPath = receiptPath,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedBy = createdBy
            };

            transactions.Add(newExpense);
            await _dataService.SaveToFileAsync(transactions, "transactions.json");

            return newExpense;
        }

        public async Task<Transaction> AddIncomeAsync(string organizationId, string schoolYearId,
                                                     string eventId, TransactionCategory category,
                                                     string detail, decimal amount,
                                                     byte[] receiptData, string receiptFileName,
                                                     string createdBy)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            string receiptPath = null;
            if (receiptData != null && receiptFileName != null)
            {
                receiptPath = await _dataService.SaveReceiptAsync(receiptData, receiptFileName);
            }

            var newIncome = new Transaction
            {
                OrganizationId = organizationId,
                SchoolYearId = schoolYearId,
                EventId = eventId,
                Type = TransactionType.Income,
                Category = category,
                Detail = detail,
                Amount = amount,
                ReceiptPath = receiptPath,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedBy = createdBy
            };

            transactions.Add(newIncome);
            await _dataService.SaveToFileAsync(transactions, "transactions.json");

            return newIncome;
        }

        public async Task UpdateTransactionApprovalAsync(string transactionId, ApprovalStatus status, string approvedBy)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
            var transaction = transactions.FirstOrDefault(t => t.Id == transactionId);

            if (transaction != null)
            {
                transaction.ApprovalStatus = status;
                transaction.ApprovedBy = approvedBy;
                transaction.ApprovalDate = DateTime.Now;

                await _dataService.SaveToFileAsync(transactions, "transactions.json");
            }
        }

        // Add this new method for resetting all pending transactions
        public async Task<int> ResetAllPendingTransactionsAsync(string adminId)
        {
            // Load all transactions
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            // Find all pending transactions (both income and expense)
            var pendingTransactions = transactions
                .Where(t => t.ApprovalStatus == ApprovalStatus.Pending)
                .ToList();

            // Update each transaction to rejected status
            foreach (var transaction in pendingTransactions)
            {
                transaction.ApprovalStatus = ApprovalStatus.Rejected;
                transaction.ApprovedBy = adminId;
                transaction.ApprovalDate = DateTime.Now;
            }

            // Save the updated transactions back to the file
            await _dataService.SaveToFileAsync(transactions, "transactions.json");

            // Return the count of reset transactions
            return pendingTransactions.Count;
        }
    }
}