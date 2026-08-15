using Microsoft.AspNetCore.Mvc;
using TaskManagement.Data;
using TaskManagement.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /User/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // Check email already exists
            bool emailExists = _context.Users
                .Any(u => u.Email == user.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists."
                );

                return View(user);
            }

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(
                user.Password
            );

            user.Role = "user";
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: /User/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(
                password,
                user.Password
            );

            if (!passwordValid)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Create claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            // Create identity
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            // Create principal
            var principal = new ClaimsPrincipal(identity);

            // Create authentication cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Index", "Home");
        }
    }
}