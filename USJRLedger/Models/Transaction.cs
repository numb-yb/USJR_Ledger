using System;
using System.Text.Json.Serialization;

namespace USJRLedger.Models
{
    public enum TransactionType
    {
        Income,
        Expense
    }

    public enum TransactionCategory
    {
        General,
        Event
    }

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

        [JsonPropertyName("eventId")]
        public string EventId { get; set; }

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
        public string ApprovedBy { get; set; }

        [JsonPropertyName("approvalDate")]
        public DateTime? ApprovalDate { get; set; }

        public Transaction()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            ApprovalStatus = ApprovalStatus.Pending;
        }
    }
}