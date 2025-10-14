using System;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;
namespace USJRLedger.Services
{
    public class AuthService
    {
        private readonly DataService _dataService;
        public User CurrentUser { get; private set; }
        public AuthService(DataService dataService)
        {
            _dataService = dataService;
        }
        public async Task<bool> LoginAsync(string username, string password)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password && u.IsActive);
            if (user != null)
            {
                CurrentUser = user;
                return true;
            }
            return false;
        }

        // Add this new method for verifying passwords
        public async Task<bool> VerifyUserPassword(string userId, string password)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user != null)
            {
                return user.Password == password;
            }
            return false;
        }

        public async Task ChangePasswordAsync(string newPassword)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("No user is currently logged in");
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var userToUpdate = users.FirstOrDefault(u => u.Id == CurrentUser.Id);
            if (userToUpdate != null)
            {
                userToUpdate.Password = newPassword;
                userToUpdate.IsTemporaryPassword = false;
                await _dataService.SaveToFileAsync(users, "users.json");
                CurrentUser = userToUpdate;
            }
        }
        public void Logout()
        {
            CurrentUser = null;
        }
        public bool RequiresPasswordChange()
        {
            return CurrentUser != null && CurrentUser.IsTemporaryPassword;
        }
    }
}