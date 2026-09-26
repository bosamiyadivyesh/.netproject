using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class AdminUserListRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int TotalAssignedTasks { get; set; } // count from TaskAssignments
    }
    public class AdminUserFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = "";

        // Required only when creating a new user.
        // Leave blank on Edit to keep the current password.
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = "user"; // "user" or "admin"

        public bool IsEdit { get; set; }
    }
}
