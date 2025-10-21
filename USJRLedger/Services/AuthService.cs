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

        //  Login and store session
        public async Task<bool> LoginAsync(string username, string password)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password && u.IsActive);

            if (user != null)
            {
                CurrentUser = user;

                // Save session for persistence
                await SaveSessionAsync();
                return true;
            }

            return false;
        }

        //  Proper Logout – clears memory + stored session
        public void Logout()
        {
            CurrentUser = null;

            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }

        //  Save session to a small file
        private async Task SaveSessionAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(CurrentUser);
                await File.WriteAllTextAsync(_sessionFilePath, json);
            }
            catch
            {
                // Fail silently – session save isn’t critical
            }
        }

        //  Restore session if user was logged in previously
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

                // Check if still active
                var users = await _dataService.LoadFromFileAsync<User>("users.json");
                var refreshedUser = users.FirstOrDefault(u => u.Id == user.Id && u.IsActive);

                if (refreshedUser != null)
                {
                    CurrentUser = refreshedUser;
                    return true;
                }

                // Invalid user or deactivated
                Logout();
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Change password for the currently logged-in user
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
                await SaveSessionAsync(); // Update stored session with new password
            }
        }
        // Check if the user is required to change their password
        public bool RequiresPasswordChange()
        {
            return CurrentUser != null && CurrentUser.IsTemporaryPassword;
        }
        public async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");

            // Allow only Adviser and Officer accounts to be reset
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                (u.Role == UserRole.Adviser || u.Role == UserRole.Officer));

            if (user == null)
                return false;

            user.Password = newPassword;
            user.IsTemporaryPassword = false; // mark as updated

            await _dataService.SaveToFileAsync(users, "users.json");
            return true;
        }


    }
}
