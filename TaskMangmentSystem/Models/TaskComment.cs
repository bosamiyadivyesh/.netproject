namespace TaskManagement.Models
{
    public class TaskComment
    {
        public int CommentId { get; set; }

        public int TaskId { get; set; }

        public TaskItem Task { get; set; } = null!;

        public int? UserId { get; set; }

        public User? User { get; set; }

        public string Comment { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}