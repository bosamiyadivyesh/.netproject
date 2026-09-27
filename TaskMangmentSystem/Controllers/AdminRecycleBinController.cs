using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminRecycleBinController : Controller
    {
        private readonly AppDbContext _context;

        public AdminRecycleBinController(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------
        // IMPORTANT (PostgreSQL/Npgsql rule used across this whole project):
        // "timestamp with time zone" columns only accept DateTime with
        // Kind = Utc. Any DateTime that comes from an HTML <input> (date,
        // datetime-local) arrives with Kind = Unspecified and MUST be
        // converted with DateTime.SpecifyKind(x, DateTimeKind.Utc) (or
        // ToUniversalTime() if it is actually Local) before it is used in
        // a query or saved to the database. DateTime.UtcNow is already
        // safe because its Kind is already Utc.
        // ----------------------------------------------------------------
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

        // GET: /AdminRecycleBin
        // Optional filters: search (title), fromDate, toDate (based on DeletedAt)
        public async Task<IActionResult> Index(string? search, DateTime? fromDate, DateTime? toDate)
        {
            var fromDateUtc = ToUtc(fromDate);
            var toDateUtc = toDate.HasValue ? ToUtc(toDate.Value.AddDays(1).AddTicks(-1)) : null;

            var query = _context.Tasks
                .AsNoTracking()
                .Where(t => t.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Title.Contains(search));
            }

            if (fromDateUtc.HasValue)
            {
                query = query.Where(t => t.DeletedAt != null && t.DeletedAt >= fromDateUtc.Value);
            }

            if (toDateUtc.HasValue)
            {
                query = query.Where(t => t.DeletedAt != null && t.DeletedAt <= toDateUtc.Value);
            }

            var tasks = await query
                .OrderByDescending(t => t.DeletedAt)
                .Select(t => new AdminDeletedTaskRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                    CreatedAt = t.CreatedAt,
                    DeletedAt = t.DeletedAt
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(tasks);
        }

        // POST: /AdminRecycleBin/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null || !task.IsDeleted)
            {
                return NotFound();
            }

            task.IsDeleted = false;
            task.DeletedAt = null;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.TaskId,
                ChangedBy = null,
                Action = "updated", // "restored" is not in the DB's allowed action list
                OldValue = "deleted",
                NewValue = "restored",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task restored successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminRecycleBin/DeletePermanently/5 (confirmation page)
        [HttpGet]
        public async Task<IActionResult> DeletePermanently(int id)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaskId == id && t.IsDeleted);

            if (task == null)
            {
                return NotFound();
            }

            ViewBag.AssignmentCount = await _context.TaskAssignments.CountAsync(a => a.TaskId == id);
            ViewBag.CompletionCount = await _context.TaskCompletions.CountAsync(c => c.TaskId == id);
            ViewBag.CommentCount = await _context.TaskComments.CountAsync(c => c.TaskId == id);
            ViewBag.HistoryCount = await _context.TaskHistories.CountAsync(h => h.TaskId == id);

            return View(task);
        }

        // POST: /AdminRecycleBin/DeletePermanently/5
        [HttpPost, ActionName("DeletePermanently")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanentlyConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null || !task.IsDeleted)
            {
                return NotFound();
            }

            // Remove dependent rows first (assignments, completions, comments, history)
            // so the hard delete of the task itself does not violate any foreign key.
            var assignments = _context.TaskAssignments.Where(a => a.TaskId == id);
            var completions = _context.TaskCompletions.Where(c => c.TaskId == id);
            var comments = _context.TaskComments.Where(c => c.TaskId == id);
            var history = _context.TaskHistories.Where(h => h.TaskId == id);
            var notifications = _context.Notifications.Where(n => n.TaskId == id);

            _context.TaskAssignments.RemoveRange(assignments);
            _context.TaskCompletions.RemoveRange(completions);
            _context.TaskComments.RemoveRange(comments);
            _context.TaskHistories.RemoveRange(history);
            _context.Notifications.RemoveRange(notifications);
            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Task permanently deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
