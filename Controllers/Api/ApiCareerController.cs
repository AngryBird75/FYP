using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mirrors Views/Career/Explore.cshtml, Views/Career/SavedCareers.cshtml
    // and Views/Career/SkillGap.cshtml. Matches the routes the Android app
    // already calls in ApiService.kt: career/explore, career/saved,
    // career/{id}/save (POST + DELETE), career/{id}/skill-gap.
    [ApiController]
    [Route("api/career")]
    [Authorize(Roles = "Student")]
    public class ApiCareerController : ControllerBase
    {
        private readonly ICareerRepository _careers;

        public ApiCareerController(ICareerRepository careers)
        {
            _careers = careers;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("explore")]
        public async Task<IActionResult> Explore()
        {
            var careers = await _careers.GetExploreCareersAsync(CurrentUserId);
            return Ok(ApiResponse<List<CareerDto>>.Ok(careers));
        }

        [HttpGet("saved")]
        public async Task<IActionResult> Saved()
        {
            var careers = await _careers.GetSavedCareersAsync(CurrentUserId);
            return Ok(ApiResponse<List<CareerDto>>.Ok(careers));
        }

        [HttpPost("{id}/save")]
        public async Task<IActionResult> Save(int id)
        {
            var ok = await _careers.SaveCareerAsync(CurrentUserId, id);
            if (!ok) return NotFound(ApiResponse<object>.Fail("Career not found."));
            return Ok(ApiResponse<string>.Ok("Saved", "Career saved"));
        }

        [HttpDelete("{id}/save")]
        public async Task<IActionResult> Unsave(int id)
        {
            await _careers.UnsaveCareerAsync(CurrentUserId, id);
            return Ok(ApiResponse<string>.Ok("Removed", "Career removed from saved"));
        }

        [HttpGet("{id}/skill-gap")]
        public async Task<IActionResult> SkillGap(int id)
        {
            var gap = await _careers.GetSkillGapAsync(CurrentUserId, id);
            if (gap == null) return NotFound(ApiResponse<object>.Fail("Career not found."));
            return Ok(ApiResponse<SkillGapResponse>.Ok(gap));
        }
    }
}
