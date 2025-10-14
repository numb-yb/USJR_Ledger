using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    public class Event
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("schoolYearId")]
        public string SchoolYearId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("eventDate")]
        public DateTime EventDate { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        public Event()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
        }
    }
}