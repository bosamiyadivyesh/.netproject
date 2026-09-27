using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    // Row shown in the Task Assignments list table
    public class AdminAssignmentListRow
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskStatus { get; set; } = "";
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime AssignedAt { get; set; }
    }

    // Used for the "Assign a user to a task" form
    public class AdminAssignmentFormViewModel
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public int UserId { get; set; }

        public List<TaskOption> TaskOptions { get; set; } = new();
        public List<UserOption> UserOptions { get; set; } = new();
    }

    public class TaskOption
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
    }
}
