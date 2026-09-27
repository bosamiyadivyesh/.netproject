using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminTaskHistoryController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTaskHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminTaskHistory
        // Read-only audit log. Optional filters: taskId, action, userId, fromDate, toDate
        public async Task<IActionResult> Index(int? taskId, string? action, int? userId, DateTime? fromDate, DateTime? toDate)
        {
            // PostgreSQL (Npgsql) only accepts DateTime with Kind=Utc for "timestamp with time zone"
            // columns. A date <input> posts back Kind=Unspecified, so convert before using in a query.
            var fromDateUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var toDateUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

            var query = _context.TaskHistories
                .AsNoTracking()
                .AsQueryable();

            if (taskId.HasValue)
            {
                query = query.Where(h => h.TaskId == taskId.Value);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(h => h.Action == action);
            }

            if (userId.HasValue)
            {
                query = query.Where(h => h.ChangedBy == userId.Value);
            }

            if (fromDateUtc.HasValue)
            {
                query = query.Where(h => h.ChangedAt >= fromDateUtc.Value);
            }

            if (toDateUtc.HasValue)
            {
                query = query.Where(h => h.ChangedAt <= toDateUtc.Value);
            }

            var rows = await query
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new AdminHistoryListRow
                {
                    HistoryId = h.HistoryId,
                    TaskId = h.TaskId,
                    TaskTitle = h.Task.Title,
                    Action = h.Action,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    ChangedByName = h.ChangedByUser != null ? h.ChangedByUser.Name : null,
                    ChangedAt = h.ChangedAt
                })
                .ToListAsync();

            // Distinct list of actions actually used, for the filter dropdown
            var distinctActions = await _context.TaskHistories
                .AsNoTracking()
                .Select(h => h.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            ViewBag.TaskId = taskId;
            ViewBag.Action = action;
            ViewBag.UserId = userId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.ActionOptions = distinctActions;
            ViewBag.TaskOptions = await GetTaskOptionsAsync();
            ViewBag.UserOptions = await GetUserOptionsAsync();

            return View(rows);
        }

        private async Task<List<TaskOption>> GetTaskOptionsAsync()
        {
            return await _context.Tasks
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TaskOption { TaskId = t.TaskId, Title = t.Title })
                .ToListAsync();
        }

        private async Task<List<UserOption>> GetUserOptionsAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new UserOption { Id = u.Id, Name = u.Name })
                .ToListAsync();
        }
    }
}
