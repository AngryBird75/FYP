using AspiraHub.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    public class StudentDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;

        public StudentDashboardController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetStudentDashboardAsync(GetUserId());
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!IsStudent()) return Unauthorized();

            await _repo.MarkNotificationReadAsync(id, GetUserId());
            return Json(new { success = true });
        }

        private bool IsStudent()
            => HttpContext.Session.GetString("Role") == "Student";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}