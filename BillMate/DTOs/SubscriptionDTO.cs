using BillMate.Models;

namespace BillMate.DTOs
{
    public class    SubscriptionDTO
    {
        public SubscriptionPackage? Package { get; set; }
        public SubscriptionType? Type { get; set; }
        public BillingPeriod? Period { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? PurchaseToken { get; set; }
        public string? SubscriptionId { get; set; }
    }
}
