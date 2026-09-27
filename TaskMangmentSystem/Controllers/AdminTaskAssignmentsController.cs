using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminTaskAssignmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTaskAssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminTaskAssignments
        // Optional filters: taskId (e.g. coming from Task Details "Manage Assignments" link), userId
        public async Task<IActionResult> Index(int? taskId, int? userId)
        {
            var query = _context.TaskAssignments
                .AsNoTracking()
                .AsQueryable();

            if (taskId.HasValue)
            {
                query = query.Where(a => a.TaskId == taskId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            var rows = await query
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new AdminAssignmentListRow
                {
                    Id = a.Id,
                    TaskId = a.TaskId,
                    TaskTitle = a.Task.Title,
                    TaskStatus = a.Task.Status,
                    UserId = a.UserId,
                    UserName = a.User.Name,
                    AssignedAt = a.AssignedAt
                })
                .ToListAsync();

            ViewBag.TaskId = taskId;
            ViewBag.UserId = userId;
            ViewBag.TaskOptions = await GetTaskOptionsAsync();
            ViewBag.UserOptions = await GetUserOptionsAsync();

            return View(rows);
        }

        // GET: /AdminTaskAssignments/Create?taskId=5
        [HttpGet]
        public async Task<IActionResult> Create(int? taskId)
        {
            var vm = new AdminAssignmentFormViewModel
            {
                TaskId = taskId ?? 0,
                TaskOptions = await GetTaskOptionsAsync(),
                UserOptions = await GetUserOptionsAsync()
            };
            return View(vm);
        }

        // POST: /AdminTaskAssignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminAssignmentFormViewModel vm)
        {
            var taskExists = await _context.Tasks.AnyAsync(t => t.TaskId == vm.TaskId && !t.IsDeleted);
            if (!taskExists)
            {
                ModelState.AddModelError("TaskId", "Selected task does not exist.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == vm.UserId);
            if (!userExists)
            {
                ModelState.AddModelError("UserId", "Selected user does not exist.");
            }

            var alreadyAssigned = await _context.TaskAssignments
                .AnyAsync(a => a.TaskId == vm.TaskId && a.UserId == vm.UserId);
            if (alreadyAssigned)
            {
                ModelState.AddModelError("", "This user is already assigned to this task.");
            }

            if (!ModelState.IsValid)
            {
                vm.TaskOptions = await GetTaskOptionsAsync();
                vm.UserOptions = await GetUserOptionsAsync();
                return View(vm);
            }

            var assignment = new TaskAssignment
            {
                TaskId = vm.TaskId,
                UserId = vm.UserId,
                AssignedAt = DateTime.UtcNow
            };

            _context.TaskAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Log in TaskHistories
            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = vm.TaskId,
                ChangedBy = null,
                Action = "updated",
                OldValue = null,
                NewValue = vm.UserId.ToString(),
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "User assigned to task successfully.";
            return RedirectToAction(nameof(Index), new { taskId = vm.TaskId });
        }

        // GET: /AdminTaskAssignments/Delete/5  (confirmation - unassign)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _context.TaskAssignments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new AdminAssignmentListRow
                {
                    Id = a.Id,
                    TaskId = a.TaskId,
                    TaskTitle = a.Task.Title,
                    TaskStatus = a.Task.Status,
                    UserId = a.UserId,
                    UserName = a.User.Name,
                    AssignedAt = a.AssignedAt
                })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        // POST: /AdminTaskAssignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.TaskAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            int taskId = assignment.TaskId;
            int userId = assignment.UserId;

            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = taskId,
                ChangedBy = null,
                Action = "updated",
                OldValue = userId.ToString(),
                NewValue = null,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "User unassigned from task.";
            return RedirectToAction(nameof(Index), new { taskId });
        }

        private async Task<List<TaskOption>> GetTaskOptionsAsync()
        {
            return await _context.Tasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted)
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
