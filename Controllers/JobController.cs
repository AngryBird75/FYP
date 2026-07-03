using AspiraHub.Data;
using AspiraHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class JobController : Controller
    {
        private readonly IJobService _job;
        private readonly AppDbContext _db;

        public JobController(IJobService job, AppDbContext db)
        {
            _job = job;
            _db = db;
        }

        public async Task<IActionResult> Browse(string? search, string? jobType)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            await _job.RunMatchingForStudentAsync(studentId);

            var vm = await _job.BrowseJobsAsync(studentId, search, jobType);
            return View(vm);
        }

        public async Task<IActionResult> SavedJobs()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int userId = GetUserId();
            int studentId = await GetStudentId();

            var list = await _job.GetSavedJobsAsync(userId, studentId);
            return View(list);
        }

        public async Task<IActionResult> MyApplications()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var list = await _job.GetMyApplicationsAsync(studentId);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Apply(int jobId, string? coverLetter)
        {
            if (!IsStudent()) return Unauthorized();

            int studentId = await GetStudentId();
            bool result = await _job.ApplyJobAsync(studentId, jobId, coverLetter);

            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSave(int jobId, bool isSaved)
        {
            if (!IsStudent()) return Unauthorized();

            int userId = GetUserId();
            bool result = isSaved
                ? await _job.UnsaveJobAsync(userId, jobId)
                : await _job.SaveJobAsync(userId, jobId);

            return Json(new { success = result });
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