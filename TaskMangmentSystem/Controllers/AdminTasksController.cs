using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminTasksController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminTasks
        // Filters: search (title), status, priority, projectId, assignedTo
        public async Task<IActionResult> Index(string? search, string? status, string? priority, int? projectId, int? assignedTo)
        {
            var query = _context.Tasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted) // recycle bin has its own page
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (assignedTo.HasValue)
            {
                query = query.Where(t => t.AssignedTo == assignedTo.Value);
            }

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AdminTaskListRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline,
                    IsRecurring = t.IsRecurring,
                    AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                    CreatedByName = t.CreatedByUser != null ? t.CreatedByUser.Name : null,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.ProjectId = projectId;
            ViewBag.AssignedTo = assignedTo;
            ViewBag.ProjectOptions = await GetProjectOptionsAsync();
            ViewBag.UserOptions = await GetUserOptionsAsync();

            return View(tasks);
        }

        // GET: /AdminTasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
            {
                return NotFound();
            }

            var vm = new AdminTaskDetailsViewModel
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                ProjectName = task.Project != null ? task.Project.Name : null,
                Status = task.Status,
                Priority = task.Priority,
                Deadline = task.Deadline,
                IsRecurring = task.IsRecurring,
                CreatedByName = task.CreatedByUser != null ? task.CreatedByUser.Name : null,
                AssignedToName = task.AssignedToUser != null ? task.AssignedToUser.Name : null,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };

            vm.Assignments = await _context.TaskAssignments
                .AsNoTracking()
                .Where(a => a.TaskId == id)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new AdminTaskAssignmentRow
                {
                    Id = a.Id,
                    UserName = a.User.Name,
                    AssignedAt = a.AssignedAt
                })
                .ToListAsync();

            vm.Comments = await _context.TaskComments
                .AsNoTracking()
                .Where(c => c.TaskId == id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new AdminTaskCommentRow
                {
                    CommentId = c.CommentId,
                    UserName = c.User != null ? c.User.Name : null,
                    Comment = c.Comment,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            vm.History = await _context.TaskHistories
                .AsNoTracking()
                .Where(h => h.TaskId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new AdminTaskHistoryRow
                {
                    HistoryId = h.HistoryId,
                    Action = h.Action,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    ChangedByName = h.ChangedByUser != null ? h.ChangedByUser.Name : null,
                    ChangedAt = h.ChangedAt
                })
                .ToListAsync();

            vm.Completion = await _context.TaskCompletions
                .AsNoTracking()
                .Where(c => c.TaskId == id)
                .OrderByDescending(c => c.CompletedAt)
                .Select(c => new AdminTaskCompletionRow
                {
                    CompletionId = c.CompletionId,
                    CompletedByName = c.CompletedByUser != null ? c.CompletedByUser.Name : null,
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl,
                    Notes = c.Notes
                })
                .FirstOrDefaultAsync();

            return View(vm);
        }

        // GET: /AdminTasks/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new AdminTaskFormViewModel
            {
                IsEdit = false,
                Status = "pending",
                Priority = "medium",
                ProjectOptions = await GetProjectOptionsAsync(),
                UserOptions = await GetUserOptionsAsync()
            };
            return View("Form", vm);
        }

        // POST: /AdminTasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminTaskFormViewModel vm)
        {
            vm.IsEdit = false;

            if (!ModelState.IsValid)
            {
                vm.ProjectOptions = await GetProjectOptionsAsync();
                vm.UserOptions = await GetUserOptionsAsync();
                return View("Form", vm);
            }

            var task = new TaskItem
            {
                Title = vm.Title,
                Description = vm.Description,
                ProjectId = vm.ProjectId,
                Status = vm.Status,
                Priority = vm.Priority,
                Deadline = ToUtc(vm.Deadline),
                IsRecurring = vm.IsRecurring,
                AssignedTo = vm.AssignedTo,
                CreatedBy = vm.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Log creation in TaskHistories
            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.TaskId,
                ChangedBy = vm.CreatedBy,
                Action = "created",
                OldValue = null,
                NewValue = task.Status,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminTasks/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null || task.IsDeleted)
            {
                return NotFound();
            }

            var vm = new AdminTaskFormViewModel
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                ProjectId = task.ProjectId,
                Status = task.Status,
                Priority = task.Priority,
                Deadline = task.Deadline,
                IsRecurring = task.IsRecurring,
                AssignedTo = task.AssignedTo,
                CreatedBy = task.CreatedBy,
                IsEdit = true,
                ProjectOptions = await GetProjectOptionsAsync(),
                UserOptions = await GetUserOptionsAsync()
            };

            return View("Form", vm);
        }

        // POST: /AdminTasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminTaskFormViewModel vm)
        {
            vm.IsEdit = true;

            if (id != vm.TaskId)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null || task.IsDeleted)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                vm.ProjectOptions = await GetProjectOptionsAsync();
                vm.UserOptions = await GetUserOptionsAsync();
                return View("Form", vm);
            }

            string oldStatus = task.Status;

            task.Title = vm.Title;
            task.Description = vm.Description;
            task.ProjectId = vm.ProjectId;
            task.Status = vm.Status;
            task.Priority = vm.Priority;
            task.Deadline = ToUtc(vm.Deadline);
            task.IsRecurring = vm.IsRecurring;
            task.AssignedTo = vm.AssignedTo;
            task.CreatedBy = vm.CreatedBy;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log in TaskHistories: "status_changed" if the status itself changed,
            // otherwise a generic "updated" entry (allowed values are fixed by a DB check constraint).
            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.TaskId,
                ChangedBy = vm.CreatedBy,
                Action = oldStatus != task.Status ? "status_changed" : "updated",
                OldValue = oldStatus != task.Status ? oldStatus : null,
                NewValue = oldStatus != task.Status ? task.Status : null,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminTasks/Delete/5  (soft delete confirmation)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaskId == id && !t.IsDeleted);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: /AdminTasks/Delete/5  (soft delete -> goes to Recycle Bin page)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsDeleted = true;
            task.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.TaskId,
                ChangedBy = null,
                Action = "deleted",
                OldValue = null,
                NewValue = null,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task moved to Recycle Bin.";
            return RedirectToAction(nameof(Index));
        }

        // PostgreSQL (Npgsql) only accepts DateTime with Kind=Utc for "timestamp with time zone"
        // columns. A datetime-local <input> posts back Kind=Unspecified, so we must convert it
        // before saving, or EF Core throws: "Cannot write DateTime with Kind=Unspecified ...".
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

        private async Task<List<ProjectOption>> GetProjectOptionsAsync()
        {
            return await _context.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ProjectOption { ProjectId = p.ProjectId, Name = p.Name })
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
