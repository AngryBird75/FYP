using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mirrors Views/Learning/Explorer.cshtml + Views/Learning/UniversityRecs.cshtml.
    // Separate controller from wherever learning/recommended-courses already
    // lives — same "api/learning" route prefix, different action, so both
    // controllers coexist without any route conflict.
    [ApiController]
    [Route("api/learning")]
    [Authorize(Roles = "Student")]
    public class ApiLearningUniversitiesController : ControllerBase
    {
        private readonly IUniversityRepository _universities;

        public ApiLearningUniversitiesController(IUniversityRepository universities)
        {
            _universities = universities;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("universities")]
        public async Task<IActionResult> Universities([FromQuery] string? search)
        {
            var result = await _universities.GetRecommendedAsync(CurrentUserId, search);
            return Ok(ApiResponse<List<UniversityDto>>.Ok(result));
        }
    }
}
