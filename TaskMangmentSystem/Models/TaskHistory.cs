namespace TaskManagement.Models
{
    public class TaskHistory
    {
        public int HistoryId { get; set; }

        public int TaskId { get; set; }

        public TaskItem Task { get; set; } = null!;

        public int? ChangedBy { get; set; }

        public User? ChangedByUser { get; set; }

        public string Action { get; set; } = null!;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}