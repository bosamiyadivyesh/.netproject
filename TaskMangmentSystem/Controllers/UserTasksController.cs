using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.User
{
    [Authorize]
    public class UserTasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserTasksController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // Every task assigned to the current user, either as primary AssignedTo
        // or via the TaskAssignments join table (multi-assignment support).
        private IQueryable<TaskItem> MyTasksQuery(int userId)
        {
            var myTaskIds = _context.TaskAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .Union(
                    _context.Tasks
                        .Where(t => t.AssignedTo == userId)
                        .Select(t => t.TaskId)
                );

            return _context.Tasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted && myTaskIds.Contains(t.TaskId));
        }

        // GET: /UserTasks
        // Optional filters: status, priority, search
        public async Task<IActionResult> Index(string? status, string? priority, string? search)
        {
            int userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var query = MyTasksQuery(userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Title.Contains(search));
            }

            var tasks = await query
                .OrderBy(t => t.Deadline == null)
                .ThenBy(t => t.Deadline)
                .Select(t => new UserTaskListRow
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

            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Search = search;

            return View(tasks);
        }

        // GET: /UserTasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            int userId = GetCurrentUserId();

            var task = await MyTasksQuery(userId)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
            {
                return Forbid(); // not my task
            }

            var vm = new UserTaskDetailsViewModel
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                ProjectName = task.Project != null ? task.Project.Name : null,
                Status = task.Status,
                Priority = task.Priority,
                Deadline = task.Deadline,
                IsRecurring = task.IsRecurring,
                CreatedAt = task.CreatedAt
            };

            vm.Comments = await _context.TaskComments
                .AsNoTracking()
                .Where(c => c.TaskId == id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new UserTaskCommentRow
                {
                    CommentId = c.CommentId,
                    UserName = c.User != null ? c.User.Name : null,
                    Comment = c.Comment,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            vm.Completion = await _context.TaskCompletions
                .AsNoTracking()
                .Where(c => c.TaskId == id)
                .OrderByDescending(c => c.CompletedAt)
                .Select(c => new UserTaskCompletionRow
                {
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl,
                    Notes = c.Notes
                })
                .FirstOrDefaultAsync();

            return View(vm);
        }

        // POST: /UserTasks/AddComment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string newComment)
        {
            int userId = GetCurrentUserId();

            var isMyTask = await MyTasksQuery(userId).AnyAsync(t => t.TaskId == id);
            if (!isMyTask)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(newComment))
            {
                _context.TaskComments.Add(new TaskComment
                {
                    TaskId = id,
                    UserId = userId,
                    Comment = newComment,
                    CreatedAt = DateTime.UtcNow // already Kind=Utc, safe for PostgreSQL
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /UserTasks/UpdateStatus/5
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            int userId = GetCurrentUserId();

            var task = await MyTasksQuery(userId).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null)
            {
                return Forbid();
            }

            var vm = new UserUpdateStatusViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.Title,
                Status = task.Status == "completed" ? "in_progress" : task.Status
            };

            return View(vm);
        }

        // POST: /UserTasks/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, UserUpdateStatusViewModel vm)
        {
            int userId = GetCurrentUserId();

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && !t.IsDeleted);
            var isMyTask = task != null && await MyTasksQuery(userId).AnyAsync(t => t.TaskId == id);

            if (task == null || !isMyTask)
            {
                return Forbid();
            }

            // Users may only set pending / in_progress / cancelled here.
            // "completed" must go through the Complete Task page (requires notes/PDF).
            var allowed = new[] { "pending", "in_progress", "cancelled" };
            if (!allowed.Contains(vm.Status))
            {
                ModelState.AddModelError("Status", "Invalid status.");
            }

            if (!ModelState.IsValid)
            {
                vm.TaskId = id;
                vm.TaskTitle = task!.Title;
                return View(vm);
            }

            string oldStatus = task!.Status;
            task.Status = vm.Status;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (oldStatus != task.Status)
            {
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.TaskId,
                    ChangedBy = userId,
                    Action = "status_changed", // one of the DB's allowed CK_task_history_action values
                    OldValue = oldStatus,
                    NewValue = task.Status,
                    ChangedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Status updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /UserTasks/Complete/5
        [HttpGet]
        public async Task<IActionResult> Complete(int id)
        {
            int userId = GetCurrentUserId();

            var task = await MyTasksQuery(userId).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null)
            {
                return Forbid();
            }

            var vm = new UserCompleteTaskViewModel
            {
                TaskId = task.TaskId,
                TaskTitle = task.Title
            };

            return View(vm);
        }

        // POST: /UserTasks/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max upload
        public async Task<IActionResult> Complete(int id, UserCompleteTaskViewModel vm, IFormFile? proofPdf)
        {
            int userId = GetCurrentUserId();

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id && !t.IsDeleted);
            var isMyTask = task != null && await MyTasksQuery(userId).AnyAsync(t => t.TaskId == id);

            if (task == null || !isMyTask)
            {
                return Forbid();
            }

            string? pdfUrl = null;

            if (proofPdf != null && proofPdf.Length > 0)
            {
                if (!proofPdf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
                    !proofPdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("", "Only PDF files are allowed.");
                }
                else
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "completions");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"task{id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(proofPdf.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await proofPdf.CopyToAsync(stream);
                    }

                    pdfUrl = $"/uploads/completions/{fileName}";
                }
            }

            if (!ModelState.IsValid)
            {
                vm.TaskId = id;
                vm.TaskTitle = task.Title;
                return View(vm);
            }

            string oldStatus = task.Status;
            task.Status = "completed";
            task.UpdatedAt = DateTime.UtcNow;

            _context.TaskCompletions.Add(new TaskCompletion
            {
                TaskId = id,
                CompletedBy = userId,
                CompletedAt = DateTime.UtcNow, // already Kind=Utc, safe for PostgreSQL
                PdfUrl = pdfUrl,
                Notes = vm.Notes
            });

            await _context.SaveChangesAsync();

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.TaskId,
                ChangedBy = userId,
                Action = "completed", // one of the DB's allowed CK_task_history_action values
                OldValue = oldStatus,
                NewValue = "completed",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task marked as completed.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
