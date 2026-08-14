using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using AspiraHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    [ApiController]
    [Route("api/jobs")]
    [Authorize(Roles = "Student")]
    public class ApiJobController : ControllerBase
    {
        private readonly IJobService _jobs;
        private readonly IUserRepository _users;

        public ApiJobController(IJobService jobs, IUserRepository users)
        {
            _jobs = jobs;
            _users = users;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private async Task<int?> StudentIdAsync()
        {
            var profile = await _users.GetStudentProfileAsync(UserId);
            return profile?.student_id;
        }

        [HttpGet]
        public async Task<IActionResult> Browse([FromQuery] string? search, [FromQuery] string? jobType)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var result = await _jobs.BrowseJobsAsync(studentId.Value, search, jobType);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("saved")]
        public async Task<IActionResult> Saved()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var result = await _jobs.GetSavedJobsAsync(UserId, studentId.Value);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("my-applications")]
        public async Task<IActionResult> MyApplications()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var result = await _jobs.GetMyApplicationsAsync(studentId.Value);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply(ApplyJobRequest req)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _jobs.ApplyJobAsync(studentId.Value, req.jobId, req.coverLetter);
            return ok ? Ok(ApiResponse<string>.Ok("", "Application submitted"))
                       : BadRequest(ApiResponse<string>.Fail("Could not apply (already applied?)"));
        }

        [HttpPost("{jobId:int}/save")]
        public async Task<IActionResult> Save(int jobId)
        {
            var ok = await _jobs.SaveJobAsync(UserId, jobId);
            return ok ? Ok(ApiResponse<string>.Ok("", "Saved")) : BadRequest(ApiResponse<string>.Fail("Could not save"));
        }

        [HttpDelete("{jobId:int}/save")]
        public async Task<IActionResult> Unsave(int jobId)
        {
            var ok = await _jobs.UnsaveJobAsync(UserId, jobId);
            return ok ? Ok(ApiResponse<string>.Ok("", "Removed")) : BadRequest(ApiResponse<string>.Fail("Could not remove"));
        }
    }
}
