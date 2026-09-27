using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class AdminProjectListRow
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? CreatedByName { get; set; }   // from Users table
        public DateTime CreatedAt { get; set; }
        public int TotalTasks { get; set; }           // count from Tasks (not deleted)
        public int CompletedTasks { get; set; }       // Tasks.Status = 'completed'
    }

    // Used for both Create and Edit forms
    public class AdminProjectFormViewModel
    {
        public int ProjectId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? CreatedBy { get; set; } // selected owner (User.Id), optional

        public bool IsEdit { get; set; }

        // For the "Created By" dropdown
        public List<UserOption> UserOptions { get; set; } = new();
    }

    public class UserOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // Shown on the Project Details page
    public class AdminProjectDetailsViewModel
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int CancelledTasks { get; set; }

        public List<AdminProjectTaskRow> Tasks { get; set; } = new();
    }

    public class AdminProjectTaskRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public string? AssignedToName { get; set; }
    }
}
