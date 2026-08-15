using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        public string Role { get; set; } = "user";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}