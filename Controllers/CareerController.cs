using AspiraHub.Data;
using AspiraHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class CareerController : Controller
    {
        private readonly ICareerService _career;
        private readonly AppDbContext _db;

        public CareerController(ICareerService career, AppDbContext db)
        {
            _career = career;
            _db = db;
        }

        // ── Explore Careers ──────────────────────────────────
        public async Task<IActionResult> Explore(string? search, string? demand)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _career.ExploreCareersAsync(studentId, search, demand);
            return View(vm);
        }

        // ── Skill Gap Analysis ───────────────────────────────
        public async Task<IActionResult> SkillGap(int careerId)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _career.AnalyzeSkillGapAsync(studentId, careerId);
            return View(vm);
        }

        // ── Compare Careers ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Compare()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var vm = await _career.GetCompareOptionsAsync();
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Compare(int careerIdA, int careerIdB)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _career.CompareCareersAsync(studentId, careerIdA, careerIdB);
            return View(vm);
        }

        // ── Save / Unsave Career (AJAX) ──────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleSave(int careerId, bool isSaved)
        {
            if (!IsStudent()) return Unauthorized();

            int userId = GetUserId();
            bool result = isSaved
                ? await _career.UnsaveCareerAsync(userId, careerId)
                : await _career.SaveCareerAsync(userId, careerId);

            return Json(new { success = result });
        }

        // ── Saved Careers List ────────────────────────────────
        public async Task<IActionResult> SavedCareers()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int userId = GetUserId();
            int studentId = await GetStudentId();

            var list = await _career.GetSavedCareersAsync(userId, studentId);
            return View(list);
        }

        // ── Helpers ───────────────────────────────────────────
        private bool IsStudent() => HttpContext.Session.GetString("Role") == "Student";
        private int GetUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        private async Task<int> GetStudentId()
        {
            int userId = GetUserId();
            var profile = await _db.StudentProfiles
                .FirstOrDefaultAsync(s => s.user_id == userId);
            return profile?.student_id ?? 0;
        }
    }
}