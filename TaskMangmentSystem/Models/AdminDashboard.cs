namespace TaskManagement.Models
{
    public class AdminDashboard 
    {
        // ---------- USERS table ----------
        public int Id { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }          // Users.Role = 'admin'
        public int TotalNormalUsers { get; set; }     // Users.Role = 'user'

        // ---------- PROJECTS table ----------
        public int TotalProjects { get; set; }

        // ---------- TASKS table (IsDeleted = false only) ----------
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }         // Status = 'pending'
        public int InProgressTasks { get; set; }      // Status = 'in_progress'
        public int CompletedTasks { get; set; }       // Status = 'completed'
        public int CancelledTasks { get; set; }       // Status = 'cancelled'
        public int OverdueTasks { get; set; }         // Deadline passed and not completed/cancelled
        public int UrgentTasks { get; set; }          // Priority = 'urgent' and still open
        public int RecurringTasks { get; set; }       // IsRecurring = true
        public int DeletedTasks { get; set; }         // IsDeleted = true (recycle bin)

        // ---------- OTHER tables ----------
        public int TotalAssignments { get; set; }     // TaskAssignments
        public int TotalCompletions { get; set; }     // TaskCompletions
        public int TotalComments { get; set; }        // TaskComments
        public int UnreadNotifications { get; set; }  // Notifications.IsRead = false

        // ---------- Recent lists ----------
        public List<RecentTaskRow> RecentTasks { get; set; } = new();
        public List<RecentActivityRow> RecentActivity { get; set; } = new();
        public List<RecentCompletionRow> RecentCompletions { get; set; } = new();
    }

    public class RecentTaskRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentActivityRow
    {
        public int HistoryId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class RecentCompletionRow
    {
        public int CompletionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string? CompletedByName { get; set; }
        public DateTime CompletedAt { get; set; }
        public string? PdfUrl { get; set; }
    }
}

