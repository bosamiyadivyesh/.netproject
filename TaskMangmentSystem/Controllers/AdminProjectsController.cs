using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    public class AdminProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminProjects
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Projects.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new AdminProjectListRow
                {
                    ProjectId = p.ProjectId,
                    Name = p.Name,
                    Description = p.Description,
                    CreatedByName = p.CreatedByUser != null ? p.CreatedByUser.Name : null,
                    CreatedAt = p.CreatedAt,
                    TotalTasks = _context.Tasks.Count(t => t.ProjectId == p.ProjectId && !t.IsDeleted),
                    CompletedTasks = _context.Tasks.Count(t => t.ProjectId == p.ProjectId && !t.IsDeleted && t.Status == "completed")
                })
                .ToListAsync();

            ViewBag.Search = search;

            return View(projects);
        }

        // GET: /AdminProjects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            var tasks = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.ProjectId == id && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AdminProjectTaskRow
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    Deadline = t.Deadline,
                    AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null
                })
                .ToListAsync();

            var vm = new AdminProjectDetailsViewModel
            {
                ProjectId = project.ProjectId,
                Name = project.Name,
                Description = project.Description,
                CreatedByName = project.CreatedByUser != null ? project.CreatedByUser.Name : null,
                CreatedAt = project.CreatedAt,
                TotalTasks = tasks.Count,
                PendingTasks = tasks.Count(t => t.Status == "pending"),
                InProgressTasks = tasks.Count(t => t.Status == "in_progress"),
                CompletedTasks = tasks.Count(t => t.Status == "completed"),
                CancelledTasks = tasks.Count(t => t.Status == "cancelled"),
                Tasks = tasks
            };

            return View(vm);
        }

        // GET: /AdminProjects/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new AdminProjectFormViewModel
            {
                IsEdit = false,
                UserOptions = await GetUserOptionsAsync()
            };
            return View("Form", vm);
        }

        // POST: /AdminProjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProjectFormViewModel vm)
        {
            vm.IsEdit = false;

            if (!ModelState.IsValid)
            {
                vm.UserOptions = await GetUserOptionsAsync();
                return View("Form", vm);
            }

            var project = new Project
            {
                Name = vm.Name,
                Description = vm.Description,
                CreatedBy = vm.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProjects/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var vm = new AdminProjectFormViewModel
            {
                ProjectId = project.ProjectId,
                Name = project.Name,
                Description = project.Description,
                CreatedBy = project.CreatedBy,
                IsEdit = true,
                UserOptions = await GetUserOptionsAsync()
            };

            return View("Form", vm);
        }

        // POST: /AdminProjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminProjectFormViewModel vm)
        {
            vm.IsEdit = true;

            if (id != vm.ProjectId)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                vm.UserOptions = await GetUserOptionsAsync();
                return View("Form", vm);
            }

            project.Name = vm.Name;
            project.Description = vm.Description;
            project.CreatedBy = vm.CreatedBy;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProjects/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id && !t.IsDeleted);

            return View(project);
        }

        // POST: /AdminProjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Tasks.ProjectId uses OnDelete(SetNull), so existing tasks
            // will NOT be deleted, they will just lose their project link.
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project deleted successfully.";
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
    }
}