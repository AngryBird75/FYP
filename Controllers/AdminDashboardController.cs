using AspiraHub.Repositories;
using AspiraHub.Data;
using AspiraHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;
        private readonly AppDbContext _db;

        public AdminDashboardController(IDashboardRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetAdminDashboardAsync();
            return View(vm);
        }

        // Toggle User Active/Inactive
        [HttpPost]
        public async Task<IActionResult> ToggleUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.is_active = !user.is_active;
            await _db.SaveChangesAsync();

            // Log
            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = user.is_active ? "Activated User" : "Deactivated User",
                target_table = "Users",
                target_id = userId,
                details = $"User {user.name} ({user.role}) status changed",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = user.is_active });
        }

        // Add Announcement
        [HttpPost]
        public async Task<IActionResult> AddAnnouncement(string title,
            string content, string targetRole)
        {
            if (!IsAdmin()) return Unauthorized();

            _db.Announcements.Add(new Announcement
            {
                admin_id = GetUserId(),
                title = title,
                content = content,
                target_role = targetRole,
                is_active = true,
                published_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Delete Announcement
        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            if (!IsAdmin()) return Unauthorized();

            var ann = await _db.Announcements.FindAsync(id);
            if (ann != null)
            {
                ann.is_active = false;
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("Role") == "Admin";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}