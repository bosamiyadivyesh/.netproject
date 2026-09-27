using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class UserProfileViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = "";

        // Leave blank to keep current password
        [StringLength(100, MinimumLength = 6)]
        public string? NewPassword { get; set; }

        // Required only if NewPassword is filled in
        public string? ConfirmPassword { get; set; }

        public string Role { get; set; } = "";      // read-only display
        public DateTime CreatedAt { get; set; }      // read-only display
    }
}
