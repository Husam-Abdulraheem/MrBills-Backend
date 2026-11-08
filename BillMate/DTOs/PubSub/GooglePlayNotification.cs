namespace BillMate.DTOs.PubSub
{
    public class GooglePlayNotification
    {
        public string Version { get; set; }
        public string PackageName { get; set; }
        public long EventTimeMillis { get; set; }
        public SubscriptionNotificationData? SubscriptionNotification { get; set; }
    }
}
