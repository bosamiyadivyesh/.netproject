namespace TaskManagement.Models
{
    // Row shown on "My Task History" - every task ever assigned to me, completed or not
    public class UserTaskHistoryRow
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string? ProjectName { get; set; }
        public string Status { get; set; } = "";  // pending | in_progress | completed | cancelled
        public string Priority { get; set; } = "";
        public DateTime? Deadline { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime? CompletedAt { get; set; }   // from TaskCompletions, if any
        public string? CompletionNotes { get; set; }
        public string? PdfUrl { get; set; }
    }

    public class UserTaskHistorySummary
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Cancelled { get; set; }
        public int Overdue { get; set; }
        public List<UserTaskHistoryRow> Rows { get; set; } = new();
    }
}
