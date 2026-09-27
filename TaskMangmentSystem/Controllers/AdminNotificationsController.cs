using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminNotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminNotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------
        // PostgreSQL (Npgsql) rule: "timestamp with time zone" columns only
        // accept DateTime with Kind = Utc. Any DateTime coming from an HTML
        // <input> (date / datetime-local) has Kind = Unspecified and must be
        // converted with DateTime.SpecifyKind(x, DateTimeKind.Utc) before it
        // is used in a query or saved. DateTime.UtcNow is already safe.
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

        // GET: /AdminNotifications
        // Optional filters: userId, isRead, fromDate, toDate (based on CreatedAt)
        public async Task<IActionResult> Index(int? userId, bool? isRead, DateTime? fromDate, DateTime? toDate)
        {
            var fromDateUtc = ToUtc(fromDate);
            var toDateUtc = toDate.HasValue ? ToUtc(toDate.Value.AddDays(1).AddTicks(-1)) : null;

            var query = _context.Notifications
                .AsNoTracking()
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(n => n.UserId == userId.Value);
            }

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            if (fromDateUtc.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= fromDateUtc.Value);
            }

            if (toDateUtc.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= toDateUtc.Value);
            }

            var rows = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new AdminNotificationListRow
                {
                    NotificationId = n.NotificationId,
                    UserName = n.User != null ? n.User.Name : null,
                    TaskId = n.TaskId,
                    TaskTitle = n.Task != null ? n.Task.Title : null,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            ViewBag.UserId = userId;
            ViewBag.IsRead = isRead;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.UserOptions = await GetUserOptionsAsync();

            return View(rows);
        }

        // GET: /AdminNotifications/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new AdminNotificationFormViewModel
            {
                UserOptions = await GetUserOptionsAsync(),
                TaskOptions = await GetTaskOptionsAsync()
            };
            return View(vm);
        }

        // POST: /AdminNotifications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminNotificationFormViewModel vm)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == vm.UserId);
            if (!userExists)
            {
                ModelState.AddModelError("UserId", "Selected user does not exist.");
            }

            if (!ModelState.IsValid)
            {
                vm.UserOptions = await GetUserOptionsAsync();
                vm.TaskOptions = await GetTaskOptionsAsync();
                return View(vm);
            }

            var notification = new Notification
            {
                UserId = vm.UserId,
                TaskId = vm.TaskId,
                Message = vm.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow // already Kind=Utc, safe for PostgreSQL
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification sent successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminNotifications/MarkRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification marked as read.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminNotifications/MarkUnread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnread(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification marked as unread.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminNotifications/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.NotificationId == id)
                .Select(n => new AdminNotificationListRow
                {
                    NotificationId = n.NotificationId,
                    UserName = n.User != null ? n.User.Name : null,
                    TaskId = n.TaskId,
                    TaskTitle = n.Task != null ? n.Task.Title : null,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        // POST: /AdminNotifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<UserOption>> GetUserOptionsAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new UserOption { Id = u.Id, Name = u.Name })
                .ToListAsync();
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
    }
}
