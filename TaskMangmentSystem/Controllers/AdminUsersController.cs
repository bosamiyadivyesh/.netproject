using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.Admin
{

    [Authorize(Roles = "admin")]
    public class AdminUsersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }
        // GET: /AdminUsers
        // Optional query params: search (name/email), role
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.Name.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserListRow
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    TotalAssignedTasks = _context.TaskAssignments.Count(a => a.UserId == u.Id)
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Role = role;

            return View(users);
        }
        // GET: /AdminUsers/Create
        [HttpGet]
        public IActionResult Create()
        {
            var vm = new AdminUserFormViewModel { IsEdit = false, Role = "user" };
            return View("Form", vm);
        }
        // POST: /AdminUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserFormViewModel vm)
        {
            vm.IsEdit = false;

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError("Password", "Password is required when creating a new user.");
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == vm.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View("Form", vm);
            }

            var user = new User
            {
                Name = vm.Name,
                Email = vm.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                Role = vm.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }
        // GET: /AdminUsers/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new AdminUserFormViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Password = null, // never show the hash, blank means "keep current password"
                IsEdit = true
            };

            return View("Form", vm);
        }
        // POST: /AdminUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminUserFormViewModel vm)
        {
            vm.IsEdit = true;

            if (id != vm.Id)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == vm.Email && u.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View("Form", vm);
            }

            user.Name = vm.Name;
            user.Email = vm.Email;
            user.Role = vm.Role;

            // Only change the password if the admin typed a new one
            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(vm.Password);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminUsers/Delete/5  (confirmation page)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
        // POST: /AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent an admin deleting their own account by mistake
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}