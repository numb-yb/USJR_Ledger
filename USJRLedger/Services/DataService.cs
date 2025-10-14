using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using USJRLedger.Models;

namespace USJRLedger.Services
{
    public class DataService
    {
        private readonly string _baseDataPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public DataService()
        {
            // Get the application data path
            _baseDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "USJRLedger");

            // Create data directory if it doesn't exist
            Directory.CreateDirectory(_baseDataPath);
            Directory.CreateDirectory(Path.Combine(_baseDataPath, "Receipts"));

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // Initialize data files
            InitializeDataFilesAsync().Wait();
        }

        private async Task InitializeDataFilesAsync()
        {
            // Initialize users.json with default admin if it doesn't exist
            string usersPath = Path.Combine(_baseDataPath, "users.json");
            if (!File.Exists(usersPath))
            {
                var defaultAdmin = new User
                {
                    Username = "admin",
                    Password = "admin123", // In a real app, this should be hashed
                    Name = "System Administrator",
                    Role = UserRole.Admin,
                    IsTemporaryPassword = false
                };

                await SaveToFileAsync(new List<User> { defaultAdmin }, "users.json");
            }

            // Initialize other data files if they don't exist
            if (!File.Exists(Path.Combine(_baseDataPath, "organizations.json")))
                await SaveToFileAsync(new List<Organization>(), "organizations.json");

            if (!File.Exists(Path.Combine(_baseDataPath, "schoolyears.json")))
                await SaveToFileAsync(new List<SchoolYear>(), "schoolyears.json");

            if (!File.Exists(Path.Combine(_baseDataPath, "events.json")))
                await SaveToFileAsync(new List<Event>(), "events.json");

            if (!File.Exists(Path.Combine(_baseDataPath, "transactions.json")))
                await SaveToFileAsync(new List<Transaction>(), "transactions.json");
        }

        public async Task<List<T>> LoadFromFileAsync<T>(string filename)
        {
            string filePath = Path.Combine(_baseDataPath, filename);

            if (!File.Exists(filePath))
                return new List<T>();

            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions) ?? new List<T>();
        }

        public async Task SaveToFileAsync<T>(List<T> data, string filename)
        {
            string filePath = Path.Combine(_baseDataPath, filename);
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, data, _jsonOptions);
        }

        public async Task<string> SaveReceiptAsync(byte[] fileData, string fileName)
        {
            string receiptsFolderPath = Path.Combine(_baseDataPath, "Receipts");
            string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            string filePath = Path.Combine(receiptsFolderPath, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, fileData);
            return filePath;
        }

        public async Task<byte[]> LoadReceiptAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }
        public async Task<bool> FileExistsAsync(string filename)
        {
            try
            {
                // Try to load the file - if it works, the file exists
                await LoadFromFileAsync<object>(filename);
                return true;
            }
            catch
            {
                // If an exception occurs, the file likely doesn't exist
                return false;
            }
        }
        public async Task WriteRawJsonAsync(string filename, string jsonContent)
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, filename);
            await File.WriteAllTextAsync(filePath, jsonContent);
        }
    }
}