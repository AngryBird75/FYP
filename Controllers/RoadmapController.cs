using AspiraHub.Data;
using AspiraHub.Services;
using AspiraHub.ViewModels.Roadmap;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class RoadmapController : Controller
    {
        private readonly IRoadmapService _roadmap;
        private readonly AppDbContext _db;
        private static readonly int[] AllowedDurations = { 3, 6, 9, 12, 18, 24 };

        public RoadmapController(IRoadmapService roadmap, AppDbContext db)
        {
            _roadmap = roadmap;
            _db = db;
        }

        // ── List my roadmaps ─────────────────────────────────
        public async Task<IActionResult> Index()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _roadmap.GetMyRoadmapsAsync(studentId);
            return View(vm);
        }

        // ── Generate new roadmap ─────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Generate()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            var vm = new GenerateRoadmapVM
            {
                AvailableCareers = await _roadmap.GetCareerOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Generate(GenerateRoadmapVM vm)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            vm.AvailableCareers = await _roadmap.GetCareerOptionsAsync();

            if (vm.CareerId == 0)
            {
                ModelState.AddModelError("CareerId", "Please select a career.");
                return View(vm);
            }

            if (vm.RoadmapType is not ("Career" or "Education" or "Mixed"))
            {
                ModelState.AddModelError("RoadmapType", "Please select a valid roadmap type.");
                return View(vm);
            }

            if (!AllowedDurations.Contains(vm.DurationMonths))
            {
                ModelState.AddModelError("DurationMonths", "Please select a valid duration.");
                return View(vm);
            }

            int studentId = await GetStudentId();

            if (studentId == 0)
            {
                ModelState.AddModelError("", "Student profile not found. Please complete your profile first.");
                return View(vm);
            }

            int roadmapId = await _roadmap.GenerateRoadmapAsync(studentId, vm);

            if (roadmapId == 0)
            {
                ModelState.AddModelError("", "Could not generate roadmap. A roadmap for this career/type may already exist, or your profile needs completing.");
                return View(vm);
            }

            return RedirectToAction("Detail", new { id = roadmapId });
        }

        // ── Detail view ──────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _roadmap.GetRoadmapDetailAsync(id, studentId);
            if (vm == null) return NotFound();

            return View(vm);
        }

        // ── Step status update (AJAX) ────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateStep(int stepId, string status)
        {
            if (!IsStudent()) return Unauthorized();

            int studentId = await GetStudentId();
            bool result = await _roadmap.UpdateStepStatusAsync(stepId, studentId, status);
            return Json(new { success = result });
        }

        // ── Step Resources (Details button — AJAX partial) ──
        [HttpGet]
        public async Task<IActionResult> StepResources(int stepId)
        {
            if (!IsStudent()) return Unauthorized();
            int studentId = await GetStudentId();
            var vm = await _roadmap.GetStepResourcesAsync(stepId, studentId);
            return PartialView("_StepResources", vm);
        }

        // ── Pause / Resume ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Pause(int roadmapId)
        {
            if (!IsStudent()) return Unauthorized();
            int studentId = await GetStudentId();
            bool result = await _roadmap.PauseRoadmapAsync(roadmapId, studentId);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> Resume(int roadmapId)
        {
            if (!IsStudent()) return Unauthorized();
            int studentId = await GetStudentId();
            bool result = await _roadmap.ResumeRoadmapAsync(roadmapId, studentId);
            return Json(new { success = result });
        }

        // ── Update Title ─────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateTitle(int roadmapId, string newTitle)
        {
            if (!IsStudent()) return Unauthorized();
            int studentId = await GetStudentId();
            bool result = await _roadmap.UpdateRoadmapTitleAsync(roadmapId, studentId, newTitle);
            return Json(new { success = result });
        }

        // ── Delete ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int roadmapId)
        {
            if (!IsStudent()) return Unauthorized();
            int studentId = await GetStudentId();
            bool result = await _roadmap.DeleteRoadmapAsync(roadmapId, studentId);
            return Json(new { success = result });
        }

        // ── Report ───────────────────────────────────────────
        public async Task<IActionResult> Report()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Auth");

            int studentId = await GetStudentId();
            var vm = await _roadmap.GenerateReportAsync(studentId);
            return View(vm);
        }

        // ── Helpers ───────────────────────────────────────────
        private bool IsStudent() => HttpContext.Session.GetString("Role") == "Student";
        private int GetUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;

        private async Task<int> GetStudentId()
        {
            int userId = GetUserId();
            if (userId == 0) return 0;

            var profile = await _db.StudentProfiles
                .FirstOrDefaultAsync(s => s.user_id == userId);

            return profile?.student_id ?? 0;
        }
    }
}