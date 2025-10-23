using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class AuthService
    {
        private readonly DataService _dataService;
        private readonly string _sessionFilePath =
            Path.Combine(FileSystem.AppDataDirectory, "session.json");

        public User CurrentUser { get; private set; }

        public AuthService(DataService dataService)
        {
            _dataService = dataService;
        }

        //LOGIN with specific deactivation warnings
        public async Task<bool> LoginAsync(string username, string password)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or password.");

            if (!user.IsActive)
            {
                // Custom message depending on role
                string deactivatedBy = user.Role switch
                {
                    UserRole.Officer => "your Adviser",
                    UserRole.Adviser => "the Administrator",
                    UserRole.Admin => "the System Administrator",
                    _ => "an Administrator"
                };

                throw new UnauthorizedAccessException($"Your account has been deactivated by {deactivatedBy}. Please contact them for reactivation.");
            }

            CurrentUser = user;
            await SaveSessionAsync();
            return true;
        }

        // Proper Logout – clears memory + stored session
        public void Logout()
        {
            CurrentUser = null;

            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }

        //Save session to local file
        private async Task SaveSessionAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(CurrentUser);
                await File.WriteAllTextAsync(_sessionFilePath, json);
            }
            catch
            {
                // Silent fail – session save isn’t critical
            }
        }

        //Restore previous login session if still active
        public async Task<bool> RestoreSessionAsync()
        {
            try
            {
                if (!File.Exists(_sessionFilePath))
                    return false;

                string json = await File.ReadAllTextAsync(_sessionFilePath);
                var user = JsonSerializer.Deserialize<User>(json);

                if (user == null)
                    return false;

                // Check if the user still exists and is active
                var users = await _dataService.LoadFromFileAsync<User>("users.json");
                var refreshedUser = users.FirstOrDefault(u => u.Id == user.Id);

                if (refreshedUser == null || !refreshedUser.IsActive)
                {
                    Logout();
                    return false;
                }

                CurrentUser = refreshedUser;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //  Change password for the current user
        public async Task ChangePasswordAsync(string newPassword)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("No user is currently logged in.");

            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var userToUpdate = users.FirstOrDefault(u => u.Id == CurrentUser.Id);

            if (userToUpdate != null)
            {
                userToUpdate.Password = newPassword;
                userToUpdate.IsTemporaryPassword = false;
                await _dataService.SaveToFileAsync(users, "users.json");

                CurrentUser = userToUpdate;
                await SaveSessionAsync(); // Update stored session
            }
        }

        //  Check if user must change temporary password
        public bool RequiresPasswordChange()
        {
            return CurrentUser != null && CurrentUser.IsTemporaryPassword;
        }

        //  Reset password (for Admin or Adviser to use)
        public async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");

            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                (u.Role == UserRole.Adviser || u.Role == UserRole.Officer));

            if (user == null)
                return false;

            user.Password = newPassword;
            user.IsTemporaryPassword = false;

            await _dataService.SaveToFileAsync(users, "users.json");
            return true;
        }
    }
}
