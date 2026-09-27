namespace TaskManagement.Models
{
    public class UserNotificationRow
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
    }
}
