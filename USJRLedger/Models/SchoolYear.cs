using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    public class SchoolYear
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("semester")]
        public string Semester { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        public SchoolYear()
        {
            Id = Guid.NewGuid().ToString();
            StartDate = DateTime.Now;
            IsActive = true;
        }
    }
}