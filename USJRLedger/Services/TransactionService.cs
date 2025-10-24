using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;
using System.IO;

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

        //Save receipt to a consistent folder (Receipts/{OrganizationId})
        private async Task<string> SaveReceiptAsync(byte[] receiptData, string receiptFileName, string organizationId)
        {
            if (receiptData == null || string.IsNullOrWhiteSpace(receiptFileName))
                return null;

            try
            {
                string baseDir = FileSystem.AppDataDirectory;
                string receiptDir = Path.Combine(baseDir, "Receipts", organizationId);

                if (!Directory.Exists(receiptDir))
                    Directory.CreateDirectory(receiptDir);

                // Prevent duplicate file names
                string uniqueFileName = $"{Guid.NewGuid()}_{receiptFileName}";
                string fullPath = Path.Combine(receiptDir, uniqueFileName);

                await File.WriteAllBytesAsync(fullPath, receiptData);
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving receipt: {ex.Message}");
                return null;
            }
        }

        public async Task<Transaction> AddExpenseAsync(string organizationId, string schoolYearId,
                                                      string eventId, TransactionCategory category,
                                                      string detail, decimal amount,
                                                      byte[] receiptData, string receiptFileName,
                                                      string createdBy)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            string receiptPath = await SaveReceiptAsync(receiptData, receiptFileName, organizationId);

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

            string receiptPath = await SaveReceiptAsync(receiptData, receiptFileName, organizationId);

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

        //  Reset all pending transactions (both income & expense)
        public async Task<int> ResetAllPendingTransactionsAsync(string adminId)
        {
            var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");

            var pendingTransactions = transactions
                .Where(t => t.ApprovalStatus == ApprovalStatus.Pending)
                .ToList();

            foreach (var transaction in pendingTransactions)
            {
                transaction.ApprovalStatus = ApprovalStatus.Rejected;
                transaction.ApprovedBy = adminId;
                transaction.ApprovalDate = DateTime.Now;
            }

            await _dataService.SaveToFileAsync(transactions, "transactions.json");

            return pendingTransactions.Count;
        }
    }
}
