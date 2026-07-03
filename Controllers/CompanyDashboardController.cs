using AspiraHub.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    public class CompanyDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;

        public CompanyDashboardController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsCompany()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetCompanyDashboardAsync(GetUserId());
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!IsCompany()) return Unauthorized();

            await _repo.MarkNotificationReadAsync(id, GetUserId());
            return Json(new { success = true });
        }

        private bool IsCompany()
            => HttpContext.Session.GetString("Role") == "Company";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}