namespace TaskManagement.Models
{
    public class TaskCompletion
    {
        public int CompletionId { get; set; }

        public int TaskId { get; set; }

        public TaskItem Task { get; set; } = null!;

        public int? CompletedBy { get; set; }

        public User? CompletedByUser { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public string? PdfUrl { get; set; }

        public string? Notes { get; set; }
    }
}