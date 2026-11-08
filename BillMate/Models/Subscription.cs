using BillMate.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillMate.Models
{
    public class Subscription
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public SubscriptionType Type { get; set; }
        public SubscriptionPackage? Package { get; set; }
        public BillingPeriod? Period { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? ReceiptUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsTrial{  get; set; } = false;
        public int? GroupId { get; set; }
        public Group? Group { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public bool? AutoRenewing { get; set; }
        public int? PaymentState { get; set; }
        public string? CountryCode { get; set; }
        public string? OrderId { get; set; }
        public decimal PriceAmount { get; set; }
        public string? PriceCurrencyCode { get; set; }
        public int? CancelReason { get; set; }
        public string? RandomNumber { get; set; }
        public string? OrderNumber { get; set; }
        public string? PurchaseToken { get; set; }
        public string? SubscriptionId { get; set; }

    }

    public enum SubscriptionType
    {
        Free,
        Paid
    }

    public enum SubscriptionPackage
    {
        Small_3Users,
        Medium_5Users,
        Large_7Users
    }

    public enum BillingPeriod
    {
        Monthly,
        Yearly
    }
    public enum PaymentMethod
    {
        GooglePlay,
        Shopier
    }
}
