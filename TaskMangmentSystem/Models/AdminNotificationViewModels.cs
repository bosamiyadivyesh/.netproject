using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    // Row shown in the Notifications list table
    public class AdminNotificationListRow
    {
        public int NotificationId { get; set; }
        public string? UserName { get; set; }
        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Used for the "Send Notification" form
    public class AdminNotificationFormViewModel
    {
        [Required]
        public int UserId { get; set; }

        public int? TaskId { get; set; } // optional link to a task

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = "";

        public List<UserOption> UserOptions { get; set; } = new();
        public List<TaskOption> TaskOptions { get; set; } = new();
    }
}
