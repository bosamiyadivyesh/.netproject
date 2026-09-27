using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    // Row shown in "My Tasks" list
    public class UserTaskListRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public bool IsOverdue { get; set; }
    }

    // Shown on Task Details page (user side)
    public class UserTaskDetailsViewModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<UserTaskCommentRow> Comments { get; set; } = new();
        public UserTaskCompletionRow? Completion { get; set; }

        [StringLength(500)]
        public string? NewComment { get; set; } // used by the "Add Comment" mini form
    }

    public class UserTaskCommentRow
    {
        public int CommentId { get; set; }
        public string? UserName { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class UserTaskCompletionRow
    {
        public DateTime CompletedAt { get; set; }
        public string? PdfUrl { get; set; }
        public string? Notes { get; set; }
    }

    // Used by the "Update Status" form
    public class UserUpdateStatusViewModel
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";

        [Required]
        public string Status { get; set; } = "pending"; // pending | in_progress | cancelled
        // "completed" is intentionally NOT settable here — use the Complete Task page instead,
        // since completing requires (optional) notes + a PDF upload.
    }

    // Used by the "Complete Task" form
    public class UserCompleteTaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";

        [StringLength(1000)]
        public string? Notes { get; set; }

        // The uploaded file itself is bound separately via IFormFile in the controller action,
        // not on this view model, to keep the model simple for validation.
    }
}
