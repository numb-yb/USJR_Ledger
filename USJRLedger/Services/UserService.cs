using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class UserService
    {
        private readonly DataService _dataService;

        public UserService(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _dataService.LoadFromFileAsync<User>("users.json");
        }

        public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            return users.Where(u => u.Role == role).ToList();
        }

        public async Task<List<User>> GetUsersByOrganizationAsync(string organizationId)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            return users.Where(u => u.OrganizationId == organizationId).ToList();
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            return users.FirstOrDefault(u => u.Id == id);
        }

        public async Task<User> CreateAdviserAsync(string name, string username, string tempPassword, string organizationId)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");

            // Check if username already exists
            if (users.Any(u => u.Username == username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            var newAdviser = new User
            {
                Name = name,
                Username = username,
                Password = tempPassword,
                Role = UserRole.Adviser,
                OrganizationId = organizationId,
                IsTemporaryPassword = true
            };

            users.Add(newAdviser);
            await _dataService.SaveToFileAsync(users, "users.json");

            return newAdviser;
        }

        public async Task<User> CreateOfficerAsync(string name, string username, string tempPassword,
                                                string organizationId, string position)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");

            // Check if username already exists
            if (users.Any(u => u.Username == username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            var newOfficer = new User
            {
                Name = name,
                Username = username,
                Password = tempPassword,
                Role = UserRole.Officer,
                OrganizationId = organizationId,
                Position = position,
                IsTemporaryPassword = true
            };

            users.Add(newOfficer);
            await _dataService.SaveToFileAsync(users, "users.json");

            return newOfficer;
        }

        public async Task UpdateUserStatusAsync(string id, bool isActive)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                user.IsActive = isActive;
                await _dataService.SaveToFileAsync(users, "users.json");
            }
        }
        public async Task DeleteUserAsync(string id)
        {
            var users = await _dataService.LoadFromFileAsync<User>("users.json");
            var userToRemove = users.FirstOrDefault(u => u.Id == id);

            if (userToRemove != null)
            {
                users.Remove(userToRemove);
                await _dataService.SaveToFileAsync(users, "users.json");
            }
        }


    }
}