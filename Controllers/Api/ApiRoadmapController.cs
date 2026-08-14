using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using AspiraHub.Services;
using AspiraHub.ViewModels.Roadmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    [ApiController]
    [Route("api/roadmap")]
    [Authorize(Roles = "Student")]
    public class ApiRoadmapController : ControllerBase
    {
        private readonly IRoadmapService _roadmap;
        private readonly IUserRepository _users;

        public ApiRoadmapController(IRoadmapService roadmap, IUserRepository users)
        {
            _roadmap = roadmap;
            _users = users;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private async Task<int?> StudentIdAsync()
        {
            var profile = await _users.GetStudentProfileAsync(UserId);
            return profile?.student_id;
        }

        [HttpGet]
        public async Task<IActionResult> MyRoadmaps()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var result = await _roadmap.GetMyRoadmapsAsync(studentId.Value);
            return Ok(ApiResponse<object>.Ok(result.Roadmaps));
        }

        [HttpGet("{roadmapId:int}")]
        public async Task<IActionResult> Detail(int roadmapId)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var detail = await _roadmap.GetRoadmapDetailAsync(roadmapId, studentId.Value);
            return detail == null ? NotFound(ApiResponse<object>.Fail("Not found")) : Ok(ApiResponse<object>.Ok(detail));
        }

        [HttpGet("career-options")]
        public async Task<IActionResult> CareerOptions()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            return Ok(ApiResponse<object>.Ok(await _roadmap.GetCareerOptionsAsync(studentId.Value)));
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate(GenerateRoadmapVM vm)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var newId = await _roadmap.GenerateRoadmapAsync(studentId.Value, vm);
            return Ok(ApiResponse<int>.Ok(newId, "Roadmap generated"));
        }

        [HttpPut("steps/{stepId:int}/status")]
        public async Task<IActionResult> UpdateStepStatus(int stepId, UpdateStepStatusRequest req)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _roadmap.UpdateStepStatusAsync(stepId, studentId.Value, req.newStatus);
            return ok ? Ok(ApiResponse<string>.Ok("", "Updated")) : BadRequest(ApiResponse<string>.Fail("Could not update"));
        }

        // ── Step Resources (Details button) ──
        [HttpGet("steps/{stepId:int}/resources")]
        public async Task<IActionResult> StepResources(int stepId)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var vm = await _roadmap.GetStepResourcesAsync(stepId, studentId.Value);
            return Ok(ApiResponse<object>.Ok(vm));
        }

        public class UpdateTitleRequest { public string NewTitle { get; set; } = ""; }

        [HttpPut("{roadmapId:int}/title")]
        public async Task<IActionResult> UpdateTitle(int roadmapId, UpdateTitleRequest req)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _roadmap.UpdateRoadmapTitleAsync(roadmapId, studentId.Value, req.NewTitle);
            return ok ? Ok(ApiResponse<string>.Ok("", "Updated")) : BadRequest(ApiResponse<string>.Fail("Could not update"));
        }

        // ── Report ───────────────────────────────────────────
        [HttpGet("report")]
        public async Task<IActionResult> Report()
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var vm = await _roadmap.GenerateReportAsync(studentId.Value);
            return Ok(ApiResponse<object>.Ok(vm));
        }

        [HttpPost("{roadmapId:int}/pause")]
        public async Task<IActionResult> Pause(int roadmapId)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _roadmap.PauseRoadmapAsync(roadmapId, studentId.Value);
            return ok ? Ok(ApiResponse<string>.Ok("", "Paused")) : BadRequest(ApiResponse<string>.Fail("Failed"));
        }

        [HttpPost("{roadmapId:int}/resume")]
        public async Task<IActionResult> Resume(int roadmapId)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _roadmap.ResumeRoadmapAsync(roadmapId, studentId.Value);
            return ok ? Ok(ApiResponse<string>.Ok("", "Resumed")) : BadRequest(ApiResponse<string>.Fail("Failed"));
        }

        [HttpDelete("{roadmapId:int}")]
        public async Task<IActionResult> Delete(int roadmapId)
        {
            var studentId = await StudentIdAsync();
            if (studentId == null) return NotFound(ApiResponse<object>.Fail("Complete onboarding first"));
            var ok = await _roadmap.DeleteRoadmapAsync(roadmapId, studentId.Value);
            return ok ? Ok(ApiResponse<string>.Ok("", "Deleted")) : BadRequest(ApiResponse<string>.Fail("Failed"));
        }
    }
}
