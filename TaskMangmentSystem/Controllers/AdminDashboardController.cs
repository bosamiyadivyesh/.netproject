using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
namespace TaskManagement.Areas.Admin.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;
        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            // Tasks that are not in the recycle bin
            var activeTasks = _context.Tasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted);

            var vm = new AdminDashboard
            {
                // USERS
                TotalUsers = await _context.Users.CountAsync(),
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == "admin"),
                TotalNormalUsers = await _context.Users.CountAsync(u => u.Role == "user"),

                // PROJECTS
                TotalProjects = await _context.Projects.CountAsync(),

                // TASKS
                TotalTasks = await activeTasks.CountAsync(),
                PendingTasks = await activeTasks.CountAsync(t => t.Status == "pending"),
                InProgressTasks = await activeTasks.CountAsync(t => t.Status == "in_progress"),
                CompletedTasks = await activeTasks.CountAsync(t => t.Status == "completed"),
                CancelledTasks = await activeTasks.CountAsync(t => t.Status == "cancelled"),
                OverdueTasks = await activeTasks.CountAsync(t =>
                    t.Deadline != null &&
                    t.Deadline < now &&
                    t.Status != "completed" &&
                    t.Status != "cancelled"),
                UrgentTasks = await activeTasks.CountAsync(t =>
                    t.Priority == "urgent" &&
                    t.Status != "completed" &&
                    t.Status != "cancelled"),
                RecurringTasks = await activeTasks.CountAsync(t => t.IsRecurring),
                DeletedTasks = await _context.Tasks.CountAsync(t => t.IsDeleted),

                // OTHER TABLES
                TotalAssignments = await _context.TaskAssignments.CountAsync(),
                TotalCompletions = await _context.TaskCompletions.CountAsync(),
                TotalComments = await _context.TaskComments.CountAsync(),
                UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead)
            };

            // Latest 5 tasks
            vm.RecentTasks = await activeTasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new RecentTaskRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline,
                    AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // Latest 10 history entries (TaskHistories)
            vm.RecentActivity = await _context.TaskHistories
                .AsNoTracking()
                .OrderByDescending(h => h.ChangedAt)
                .Take(10)
                .Select(h => new RecentActivityRow
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

            // Latest 5 completions (TaskCompletions)
            vm.RecentCompletions = await _context.TaskCompletions
                .AsNoTracking()
                .OrderByDescending(c => c.CompletedAt)
                .Take(5)
                .Select(c => new RecentCompletionRow
                {
                    CompletionId = c.CompletionId,
                    TaskId = c.TaskId,
                    TaskTitle = c.Task.Title,
                    CompletedByName = c.CompletedByUser != null ? c.CompletedByUser.Name : null,
                    CompletedAt = c.CompletedAt,
                    PdfUrl = c.PdfUrl
                })
                .ToListAsync();

            return View(vm);
        }
    }
}