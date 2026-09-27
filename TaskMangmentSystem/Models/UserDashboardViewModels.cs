namespace TaskManagement.Models
{
    public class UserDashboardViewModel
    {
        public string UserName { get; set; } = "";

        // Counts for tasks assigned to (or created by) the logged-in user
        public int TotalMyTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int UnreadNotifications { get; set; }

        public List<UserUpcomingTaskRow> UpcomingDeadlines { get; set; } = new();
        public List<UserRecentNotificationRow> RecentNotifications { get; set; } = new();
    }

    public class UserUpcomingTaskRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
    }

    public class UserRecentNotificationRow
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TaskId { get; set; }
    }
}
