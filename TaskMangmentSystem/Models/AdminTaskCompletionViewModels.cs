namespace TaskManagement.Models
{
    // Row shown in the Task Completions list table
    public class AdminCompletionListRow
    {
        public int CompletionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskStatus { get; set; } = "";
        public string? ProjectName { get; set; }
        public string? CompletedByName { get; set; }
        public DateTime CompletedAt { get; set; }
        public string? PdfUrl { get; set; }
        public string? Notes { get; set; }
    }

    // Shown on the Completion Details page
    public class AdminCompletionDetailsViewModel
    {
        public int CompletionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string TaskStatus { get; set; } = "";
        public string? TaskDescription { get; set; }
        public string? ProjectName { get; set; }
        public string? CompletedByName { get; set; }
        public string? CompletedByEmail { get; set; }
        public DateTime CompletedAt { get; set; }
        public string? PdfUrl { get; set; }
        public string? Notes { get; set; }
    }
}
