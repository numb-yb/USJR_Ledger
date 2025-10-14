using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class SystemResetService
    {
        private readonly DataService _dataService;
        private readonly AuthService _authService;

        public SystemResetService(DataService dataService)
        {
            _dataService = dataService;
            // We'll use the AuthService if we need to preserve only the admin account
        }

        // Class to hold reset statistics
        public class ResetResults
        {
            public int UsersReset { get; set; }
            public int OrganizationsReset { get; set; }
            public int TransactionsReset { get; set; }
            public int OtherItemsReset { get; set; }
        }

        public async Task<ResetResults> PerformCompleteSystemResetAsync(string adminId)
        {
            var results = new ResetResults();

            // COMPLETE SYSTEM WIPE - Reset ALL data files

            // 1. Reset all users (except the current admin)
            results.UsersReset = await ResetUsersAsync(adminId);

            // 2. Reset all organizations
            results.OrganizationsReset = await ResetOrganizationsAsync();

            // 3. Reset all transactions
            results.TransactionsReset = await ResetTransactionsAsync();

            // 4. Reset ALL other files in the system
            results.OtherItemsReset = await ResetAllOtherFilesAsync();

            return results;
        }

        private async Task<int> ResetUsersAsync(string adminId)
        {
            try
            {
                // Load current users
                var users = await _dataService.LoadFromFileAsync<User>("users.json");
                int count = users.Count - 1; // Subtract 1 for the admin we're keeping

                // Preserve only the current admin account
                var adminUser = users.FirstOrDefault(u => u.Id == adminId);

                if (adminUser != null)
                {
                    // Create a new list with just the admin
                    var newUsersList = new List<User> { adminUser };
                    await _dataService.SaveToFileAsync(newUsersList, "users.json");
                    return count;
                }
                else
                {
                    // If we can't find the admin, just create a default admin account
                    var defaultAdmin = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Username = "admin",
                        Password = "admin123", // Should change this after reset
                        Name = "System Administrator",
                        Role = UserRole.Admin,
                        IsActive = true,
                        IsTemporaryPassword = true,
                        OrganizationId = null
                    };

                    await _dataService.SaveToFileAsync(new List<User> { defaultAdmin }, "users.json");
                    return users.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting users: {ex.Message}");
                return 0;
            }
        }

        private async Task<int> ResetOrganizationsAsync()
        {
            try
            {
                // Load organizations to get count
                var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                int count = organizations.Count;

                // Create empty organizations list
                await _dataService.SaveToFileAsync(new List<Organization>(), "organizations.json");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting organizations: {ex.Message}");
                return 0;
            }
        }

        private async Task<int> ResetTransactionsAsync()
        {
            try
            {
                // Load transactions to get count
                var transactions = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                int count = transactions.Count;

                // Replace with empty list
                await _dataService.SaveToFileAsync(new List<Transaction>(), "transactions.json");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting transactions: {ex.Message}");
                return 0;
            }
        }

        private async Task<int> ResetAllOtherFilesAsync()
        {
            int count = 0;

            // COMPLETE LIST of ALL data files in the system
            string[] allDataFiles = {
                // User data
                "advisers.json",
                "officers.json",
                
                // Organization data
                "organizations.json",
                
                // Financial data
                "transactions.json",
                "budgets.json",
                "expenses.json",
                "income.json",
                "financial_reports.json",
                
                // Event data
                "events.json",
                "event_attendance.json",
                "event_budgets.json",
                
                // School year data
                "school_years.json",
                "semesters.json",
                
                // User generated content
                "reports.json",
                "notes.json",
                "messages.json",
                "notifications.json",
                "attendance.json",
                "adviser_notes.json",
                "admin_reports.json",
                "audit_logs.json",
                "announcements.json",
                "officer_reports.json",
                "draft_requests.json",
                "receipts.json",
                "event_plans.json",
                
                // System data
                "settings.json",
                "system_logs.json",
                "backup_data.json"
            };

            // Reset each file by replacing it with an empty array
            foreach (var file in allDataFiles)
            {
                try
                {
                    if (await _dataService.FileExistsAsync(file))
                    {
                        // Create an empty list and save it
                        // We're using a simple dynamic approach here to avoid type issues
                        await _dataService.SaveToFileAsync(new List<object>(), file);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error resetting {file}: {ex.Message}");
                    // Continue with other files even if one fails
                }
            }

            return count;
        }
    }
}