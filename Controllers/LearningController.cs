using AspiraHub.Data;
using AspiraHub.Services;
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

        public async Task<IActionResult> UniversityRecs()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _learning.GetUniversityRecommendationsAsync(studentId);
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