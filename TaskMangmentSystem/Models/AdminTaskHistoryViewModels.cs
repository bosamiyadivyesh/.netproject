namespace TaskManagement.Models
{
    // Row shown in the Task History (Audit Log) list table
    public class AdminHistoryListRow
    {
        public int HistoryId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = "";
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
