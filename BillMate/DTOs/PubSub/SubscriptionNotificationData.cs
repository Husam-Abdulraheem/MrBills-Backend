namespace BillMate.DTOs.PubSub
{
    public class SubscriptionNotificationData
    {
        public int NotificationType { get; set; }
        public string PurchaseToken { get; set; }
        public string version { get; set; }
    }
}
