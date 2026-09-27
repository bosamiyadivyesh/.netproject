using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    // Row shown in the Tasks list table
    public class AdminTaskListRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public bool IsRecurring { get; set; }
        public string? AssignedToName { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Used for both Create and Edit forms
    public class AdminTaskFormViewModel
    {
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(2000)]
        public string? Description { get; set; }

        public int? ProjectId { get; set; }

        [Required]
        public string Status { get; set; } = "pending"; // pending | in_progress | completed | cancelled

        [Required]
        public string Priority { get; set; } = "medium"; // low | medium | high | urgent

        public DateTime? Deadline { get; set; }

        public bool IsRecurring { get; set; }

        public int? AssignedTo { get; set; }

        public int? CreatedBy { get; set; }

        public bool IsEdit { get; set; }

        // Dropdown sources
        public List<ProjectOption> ProjectOptions { get; set; } = new();
        public List<UserOption> UserOptions { get; set; } = new();
    }

    public class ProjectOption
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
    }

    // Shown on Task Details page
    public class AdminTaskDetailsViewModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public bool IsRecurring { get; set; }
        public string? CreatedByName { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<AdminTaskAssignmentRow> Assignments { get; set; } = new();
        public List<AdminTaskCommentRow> Comments { get; set; } = new();
        public List<AdminTaskHistoryRow> History { get; set; } = new();
        public AdminTaskCompletionRow? Completion { get; set; } // null if not completed yet
    }

    public class AdminTaskAssignmentRow
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public DateTime AssignedAt { get; set; }
    }

    public class AdminTaskCommentRow
    {
        public int CommentId { get; set; }
        public string? UserName { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class AdminTaskHistoryRow
    {
        public int HistoryId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class AdminTaskCompletionRow
    {
        public int CompletionId { get; set; }
        public string? CompletedByName { get; set; }
        public DateTime CompletedAt { get; set; }
        public string? PdfUrl { get; set; }
        public string? Notes { get; set; }
    }
}
