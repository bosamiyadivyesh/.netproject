using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers.User
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly AppDbContext _context;

        public UserProfileController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }

        // GET: /UserProfile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new UserProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return View(vm);
        }

        // POST: /UserProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfileViewModel vm)
        {
            int userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == vm.Email && u.Id != userId);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(vm.NewPassword) && vm.NewPassword != vm.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                vm.Role = user.Role;
                vm.CreatedAt = user.CreatedAt;
                return View(vm);
            }

            user.Name = vm.Name;
            user.Email = vm.Email;

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
