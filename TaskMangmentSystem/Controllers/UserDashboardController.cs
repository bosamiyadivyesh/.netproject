using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.User
{
    [Authorize] // any logged-in user (admin or user) can see their own dashboard
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public UserDashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /   
        public async Task<IActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();
            var now = DateTime.UtcNow; // already Kind=Utc, safe to compare against PostgreSQL timestamptz

            // A task "belongs" to the user if they are the primary AssignedTo
            // OR they appear in TaskAssignments (multi-assignment support).
            var myTaskIdsQuery = _context.TaskAssignments
                .Where(a => a.UserId == currentUserId)
                .Select(a => a.TaskId)
                .Union(
                    _context.Tasks
                        .Where(t => t.AssignedTo == currentUserId)
                        .Select(t => t.TaskId)
                );

            var myTasks = _context.Tasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted && myTaskIdsQuery.Contains(t.TaskId));

            var vm = new UserDashboardViewModel
            {
                UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "",
                TotalMyTasks = await myTasks.CountAsync(),
                PendingTasks = await myTasks.CountAsync(t => t.Status == "pending"),
                InProgressTasks = await myTasks.CountAsync(t => t.Status == "in_progress"),
                CompletedTasks = await myTasks.CountAsync(t => t.Status == "completed"),
                OverdueTasks = await myTasks.CountAsync(t =>
                    t.Deadline != null &&
                    t.Deadline < now &&
                    t.Status != "completed" &&
                    t.Status != "cancelled"),
                UnreadNotifications = await _context.Notifications
                    .CountAsync(n => n.UserId == currentUserId && !n.IsRead)
            };

            vm.UpcomingDeadlines = await myTasks
                .Where(t => t.Deadline != null && t.Status != "completed" && t.Status != "cancelled")
                .OrderBy(t => t.Deadline)
                .Take(5)
                .Select(t => new UserUpcomingTaskRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline
                })
                .ToListAsync();

            vm.RecentNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == currentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new UserRecentNotificationRow
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TaskId = n.TaskId
                })
                .ToListAsync();

            return View(vm);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
