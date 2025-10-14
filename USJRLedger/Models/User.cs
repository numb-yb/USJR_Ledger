using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    public enum UserRole
    {
        Admin,
        Adviser,
        Officer
    }

    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("role")]
        public UserRole Role { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isTemporaryPassword")]
        public bool IsTemporaryPassword { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("position")]
        public string Position { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            IsActive = true;
            IsTemporaryPassword = true;
        }
    }
}