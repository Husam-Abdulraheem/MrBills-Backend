namespace BillMate.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? GroupId { get; set; }
        public string Title { get; set; }
        public string? Message { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NotificationType Type { get; set; }

        public bool IsGlobal => UserId == null;
    }


    public enum NotificationType
    {
        Subscription,
        Bill,
        Reminder,
        General,
        System
    }

}
