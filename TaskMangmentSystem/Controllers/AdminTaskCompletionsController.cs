using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminTaskCompletionsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTaskCompletionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminTaskCompletions
        // Optional filters: taskId, userId (completedBy), from/to date, hasPdf
        public async Task<IActionResult> Index(int? taskId, int? userId, DateTime? fromDate, DateTime? toDate, bool? hasPdf)
        {
            // PostgreSQL (Npgsql) only accepts DateTime with Kind=Utc for "timestamp with time zone"
            // columns. A date <input> posts back Kind=Unspecified, so convert before using in a query.
            var fromDateUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var toDateUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

            var query = _context.TaskCompletions
                .AsNoTracking()
                .AsQueryable();

            if (taskId.HasValue)
            {
                query = query.Where(c => c.TaskId == taskId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(c => c.CompletedBy == userId.Value);
            }

            if (fromDateUtc.HasValue)
            {
                query = query.Where(c => c.CompletedAt >= fromDateUtc.Value);
            }

            if (toDateUtc.HasValue)
            {
                query = query.Where(c => c.CompletedAt <= toDateUtc.Value);
            }

            if (hasPdf.HasValue)
            {
                query = hasPdf.Value
                    ? query.Where(c => c.PdfUrl != null && c.PdfUrl != "")
                    : query.Where(c => c.PdfUrl == null || c.PdfUrl == "");
            }

            var rows = await query
                .OrderByDescending(c => c.CompletedAt)
                .Select(c => new AdminCompletionListRow
                {
                    CompletionId = c.CompletionId,
                    TaskId = c.TaskId,
                    TaskTitle = c.Task.Title,
                    TaskStatus = c.Task.Status,
                    ProjectName = c.Task.Project != null ? c.Task.Project.Name : null,
                    CompletedByName = c.CompletedByUser != null ? c.CompletedByUser.Name : null,
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl,
                    Notes = c.Notes
                })
                .ToListAsync();

            ViewBag.TaskId = taskId;
            ViewBag.UserId = userId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.HasPdf = hasPdf;
            ViewBag.TaskOptions = await GetTaskOptionsAsync();
            ViewBag.UserOptions = await GetUserOptionsAsync();

            return View(rows);
        }

        // GET: /AdminTaskCompletions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _context.TaskCompletions
                .AsNoTracking()
                .Where(c => c.CompletionId == id)
                .Select(c => new AdminCompletionDetailsViewModel
                {
                    CompletionId = c.CompletionId,
                    TaskId = c.TaskId,
                    TaskTitle = c.Task.Title,
                    TaskStatus = c.Task.Status,
                    TaskDescription = c.Task.Description,
                    ProjectName = c.Task.Project != null ? c.Task.Project.Name : null,
                    CompletedByName = c.CompletedByUser != null ? c.CompletedByUser.Name : null,
                    CompletedByEmail = c.CompletedByUser != null ? c.CompletedByUser.Email : null,
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl,
                    Notes = c.Notes
                })
                .FirstOrDefaultAsync();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        // GET: /AdminTaskCompletions/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _context.TaskCompletions
                .AsNoTracking()
                .Where(c => c.CompletionId == id)
                .Select(c => new AdminCompletionListRow
                {
                    CompletionId = c.CompletionId,
                    TaskId = c.TaskId,
                    TaskTitle = c.Task.Title,
                    TaskStatus = c.Task.Status,
                    CompletedByName = c.CompletedByUser != null ? c.CompletedByUser.Name : null,
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl,
                    Notes = c.Notes
                })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        // POST: /AdminTaskCompletions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var completion = await _context.TaskCompletions.FindAsync(id);
            if (completion == null)
            {
                return NotFound();
            }

            int taskId = completion.TaskId;

            _context.TaskCompletions.Remove(completion);
            await _context.SaveChangesAsync();

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = taskId,
                ChangedBy = null,
                Action = "deleted",
                OldValue = null,
                NewValue = null,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Completion record deleted.";
            return RedirectToAction(nameof(Index));
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
