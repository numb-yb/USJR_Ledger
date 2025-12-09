using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    // FIX 1: specific converter added here so it saves as text ("Income") not a number (0)
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionType
    {
        Income,
        Expense
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionCategory
    {
        General,
        Event
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Transaction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("organizationId")]
        public string OrganizationId { get; set; }

        [JsonPropertyName("schoolYearId")]
        public string SchoolYearId { get; set; }

        // Nullable because not all transactions belong to an event
        [JsonPropertyName("eventId")]
        public string? EventId { get; set; }

        [JsonPropertyName("type")]
        public TransactionType Type { get; set; }

        [JsonPropertyName("category")]
        public TransactionCategory Category { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("receiptPath")]
        public string ReceiptPath { get; set; }

        [JsonPropertyName("approvalStatus")]
        public ApprovalStatus ApprovalStatus { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; } // Nullable until approved

        [JsonPropertyName("approvalDate")]
        public DateTime? ApprovalDate { get; set; } // Nullable until approved

        public Transaction()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            ApprovalStatus = ApprovalStatus.Pending;
        }
    }
}