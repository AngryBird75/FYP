using AspiraHub.Data;
using AspiraHub.Services;
using AspiraHub.ViewModels.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class LearningController : Controller
    {
        private readonly ILearningService _learning;
        private readonly AppDbContext _db;

        public LearningController(ILearningService learning, AppDbContext db)
        {
            _learning = learning;
            _db = db;
        }

        public async Task<IActionResult> RecommendedCourses()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _learning.GetRecommendedCoursesAsync(studentId);
            return View(vm);
        }

        // ── University Explorer: search + filters ──
        [HttpGet]
        public async Task<IActionResult> Explorer(string? searchTerm, string? city, string? type, string? environment, int? maxBudget)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var filter = new UniversitySearchFilter
            {
                SearchTerm = searchTerm,
                City = city,
                Type = type,
                Environment = environment,
                MaxBudget = maxBudget
            };

            var vm = await _learning.SearchUniversitiesAsync(filter);
            ViewBag.SuggestMsg = TempData["SuggestMsg"];
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuggestUniversity(SuggestUniversityVM vm)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            bool ok = await _learning.SuggestUniversityAsync(studentId, vm);

            TempData["SuggestMsg"] = ok
                ? "Thanks! We'll review and add it soon."
                : "Please enter a valid university name.";

            return RedirectToAction("Explorer");
        }

        // Purana link kaam karta rahe — Explorer pe forward
        public IActionResult UniversityRecs() => RedirectToAction("Explorer");

        // ── Courses / Institutes Explorer ──
        [HttpGet]
        public async Task<IActionResult> CoursesExplorer(string? searchTerm, string? city, string? mode, string? type)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var filter = new InstituteSearchFilter
            {
                SearchTerm = searchTerm,
                City = city,
                Mode = mode,
                Type = type
            };

            var vm = await _learning.SearchInstitutesAsync(filter);
            return View(vm);
        }

        public async Task<IActionResult> MyProgress()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _learning.GetMyProgressAsync(studentId);
            return View(vm);
        }

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