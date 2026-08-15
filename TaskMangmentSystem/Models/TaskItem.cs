using TaskMangmentSystem.Models;

namespace TaskManagement.Models
{
    public class TaskItem
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int? ProjectId { get; set; }

        public Project? Project { get; set; }

        public string Status { get; set; } = "pending";

        public string Priority { get; set; } = "medium";

        public DateTime? Deadline { get; set; }

        public bool IsRecurring { get; set; } = false;

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public int? AssignedTo { get; set; }

        public User? AssignedToUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public ICollection<TaskAssignment> Assignments { get; set; }
            = new List<TaskAssignment>();

        public ICollection<TaskCompletion> Completions { get; set; }
            = new List<TaskCompletion>();

        public ICollection<TaskHistory> Histories { get; set; }
            = new List<TaskHistory>();

        public ICollection<TaskComment> Comments { get; set; }
            = new List<TaskComment>();
    }
}