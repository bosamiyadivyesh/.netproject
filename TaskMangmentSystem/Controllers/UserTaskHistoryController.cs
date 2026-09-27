using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.User
{
    [Authorize]
    public class UserTaskHistoryController : Controller
    {
        private readonly AppDbContext _context;

        public UserTaskHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // PostgreSQL (Npgsql) rule: "timestamp with time zone" columns only accept
        // DateTime with Kind = Utc. An HTML <input type="date"> posts back
        // Kind = Unspecified, so convert before using it in a query.
        private static DateTime? ToUtc(DateTime? value)
        {
            if (value == null) return null;
            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // GET: /UserTaskHistory
        // Shows every task ever assigned to me, completed or not, with filters.
        // Optional filters: status ("completed" or "not_completed"), projectId, fromDate, toDate
        public async Task<IActionResult> Index(string? status, int? projectId, DateTime? fromDate, DateTime? toDate)
        {
            int userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var fromDateUtc = ToUtc(fromDate);
            var toDateUtc = toDate.HasValue ? ToUtc(toDate.Value.AddDays(1).AddTicks(-1)) : null;

            var myTaskIds = _context.TaskAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .Union(
                    _context.Tasks
                        .Where(t => t.AssignedTo == userId)
                        .Select(t => t.TaskId)
                );

            // NOTE: this history intentionally includes soft-deleted tasks too
            // (IsDeleted is not filtered out), so the user still sees a task
            // they completed even if an admin later moved it to the Recycle Bin.
            var query = _context.Tasks
                .AsNoTracking()
                .Where(t => myTaskIds.Contains(t.TaskId));

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = status == "completed"
                    ? query.Where(t => t.Status == "completed")
                    : query.Where(t => t.Status != "completed");
            }

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (fromDateUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDateUtc.Value);
            }

            if (toDateUtc.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDateUtc.Value);
            }

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new UserTaskHistoryRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline,
                    IsOverdue = t.Deadline != null && t.Deadline < now && t.Status != "completed" && t.Status != "cancelled"
                })
                .ToListAsync();

            // Attach completion info (date, notes, pdf) for tasks that have one
            var taskIds = tasks.Select(t => t.TaskId).ToList();
            var completions = await _context.TaskCompletions
                .AsNoTracking()
                .Where(c => taskIds.Contains(c.TaskId))
                .GroupBy(c => c.TaskId)
                .Select(g => g.OrderByDescending(c => c.CompletedAt).First())
                .ToListAsync();

            foreach (var row in tasks)
            {
                var completion = completions.FirstOrDefault(c => c.TaskId == row.TaskId);
                if (completion != null)
                {
                    row.CompletedAt = completion.CompletedAt;
                    row.CompletionNotes = completion.Notes;
                    row.PdfUrl = completion.PdfUrl;
                }
            }

            var summary = new UserTaskHistorySummary
            {
                Total = tasks.Count,
                Completed = tasks.Count(t => t.Status == "completed"),
                Pending = tasks.Count(t => t.Status == "pending"),
                InProgress = tasks.Count(t => t.Status == "in_progress"),
                Cancelled = tasks.Count(t => t.Status == "cancelled"),
                Overdue = tasks.Count(t => t.IsOverdue),
                Rows = tasks
            };

            ViewBag.Status = status;
            ViewBag.ProjectId = projectId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.ProjectOptions = await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ProjectOption { ProjectId = p.ProjectId, Name = p.Name })
                .ToListAsync();

            return View(summary);
        }
    }
}
