using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using AspiraHub.Services;
using AspiraHub.ViewModels.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    [ApiController]
    [Route("api/learning")]
    [Authorize(Roles = "Student")]
    public class ApiLearningController : ControllerBase
    {
        private readonly ILearningService _learning;
        private readonly IUserRepository _users;

        public ApiLearningController(ILearningService learning, IUserRepository users)
        {
            _learning = learning;
            _users = users;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private async Task<int?> StudentIdAsync()
        {
            var profile = await _users.GetStudentProfileAsync(UserId);
            return profile?.student_id;
        }

        [HttpGet("recommended-courses")]
        public async Task<IActionResult> RecommendedCourses()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var result = await _learning.GetRecommendedCoursesAsync(studentId.Value);
            return Ok(ApiResponse<object>.Ok(result.Courses));
        }

        [HttpGet("university-recommendations")]
        public async Task<IActionResult> UniversityRecommendations()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            return Ok(ApiResponse<object>.Ok(await _learning.GetUniversityRecommendationsAsync(studentId.Value)));
        }

        // NOTE: "GET universities" used to live here (search/filter Explorer
        // action) but was REMOVED — it collided with the new dedicated
        // ApiLearningUniversitiesController, which now owns
        // "GET api/learning/universities" and is what the Android app
        // actually calls. Having both registered the same route crashed
        // every request to it with AmbiguousMatchException. If the
        // search+filter Explorer behavior is needed again later, give it a
        // different route (e.g. "universities/search") instead of reusing
        // this one.

        [HttpPost("universities/suggest")]
        public async Task<IActionResult> SuggestUniversity(SuggestUniversityVM vm)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));

            bool ok = await _learning.SuggestUniversityAsync(studentId.Value, vm);
            return ok
                ? Ok(ApiResponse<string>.Ok("", "Thanks! We'll review and add it soon."))
                : BadRequest(ApiResponse<string>.Fail("Please enter a valid university name."));
        }

        // ── Courses / Institutes Explorer ──
        [HttpGet("courses-explorer")]
        public async Task<IActionResult> CoursesExplorer([FromQuery] string? searchTerm, [FromQuery] string? city,
            [FromQuery] string? mode, [FromQuery] string? type)
        {
            var filter = new InstituteSearchFilter
            {
                SearchTerm = searchTerm,
                City = city,
                Mode = mode,
                Type = type
            };

            var vm = await _learning.SearchInstitutesAsync(filter);
            return Ok(ApiResponse<object>.Ok(vm));
        }

        [HttpGet("my-progress")]
        public async Task<IActionResult> MyProgress()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            return Ok(ApiResponse<object>.Ok(await _learning.GetMyProgressAsync(studentId.Value)));
        }
    }
}