namespace TaskManagement.Models
{
    // Row shown on "My Projects" (projects that contain at least one of my tasks)
    public class UserProjectRow
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int MyTaskCount { get; set; }
        public int MyCompletedCount { get; set; }
    }
}
