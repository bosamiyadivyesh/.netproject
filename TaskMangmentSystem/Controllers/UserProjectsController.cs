using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.User
{
    [Authorize]
    public class UserProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public UserProjectsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // GET: /UserProjects  -- only projects that contain at least one of my tasks
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();

            var myTaskIds = _context.TaskAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .Union(
                    _context.Tasks
                        .Where(t => t.AssignedTo == userId)
                        .Select(t => t.TaskId)
                );

            var myProjectIds = _context.Tasks
                .Where(t => !t.IsDeleted && myTaskIds.Contains(t.TaskId) && t.ProjectId != null)
                .Select(t => t.ProjectId!.Value)
                .Distinct();

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => myProjectIds.Contains(p.ProjectId))
                .Select(p => new UserProjectRow
                {
                    ProjectId = p.ProjectId,
                    Name = p.Name,
                    Description = p.Description,
                    MyTaskCount = _context.Tasks.Count(t =>
                        !t.IsDeleted && t.ProjectId == p.ProjectId && myTaskIds.Contains(t.TaskId)),
                    MyCompletedCount = _context.Tasks.Count(t =>
                        !t.IsDeleted && t.ProjectId == p.ProjectId && myTaskIds.Contains(t.TaskId) && t.Status == "completed")
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(projects);
        }
    }
}
