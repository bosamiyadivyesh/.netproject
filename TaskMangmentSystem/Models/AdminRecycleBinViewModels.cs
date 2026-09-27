namespace TaskManagement.Models
{
    // Row shown in the Recycle Bin list table
    public class AdminDeletedTaskRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
