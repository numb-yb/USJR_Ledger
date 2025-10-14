using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    public class Organization
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("deactivationDate")]
        public DateTime? DeactivationDate { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        public Organization()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            IsActive = true;
        }
    }
}