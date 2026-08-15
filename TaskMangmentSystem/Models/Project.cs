using TaskMangmentSystem.Models;

namespace TaskManagement.Models
{
    public class Project
    {
        public int ProjectId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int? CreatedBy { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}