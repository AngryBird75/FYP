using System.Linq;
using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using AspiraHub.Services;
using AspiraHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mirrors the website's onboarding flow (AuthController Step1..Step5 +
    // Register), just collapsed into two calls instead of six page loads:
    // the app collects everything locally on-device across its onboarding
    // screens, then (1) calls auth/register to create the account, and (2)
    // immediately calls onboarding/complete with everything it collected —
    // reusing the exact same IAuthService.SaveStepXAsync methods the
    // website's Register POST calls, so the saved data is identical either
    // way.
    [ApiController]
    [Route("api/onboarding")]
    public class ApiOnboardingController : ControllerBase
    {
        private readonly IDegreeService _degree;
        private readonly IAuthService _auth;
        private readonly IUserRepository _users;
        private readonly IJwtService _jwt;

        private static readonly string[] AllowedEducationLevels = { "Intermediate", "Undergraduate", "Graduate" };

        public ApiOnboardingController(IDegreeService degree, IAuthService auth, IUserRepository users, IJwtService jwt)
        {
            _degree = degree;
            _auth = auth;
            _users = users;
            _jwt = jwt;
        }

        // Public (no account exists yet at this point in the flow — same as
        // the website's Step1B/Step2 GETs, which only need Session, not a
        // logged-in user). educationLevel narrows degreePrograms + the
        // university/college list exactly like Step1B/Step2 do; omit it to
        // get everything unfiltered.
        [HttpGet("options")]
        public async Task<IActionResult> Options([FromQuery] string? educationLevel)
        {
            if (!string.IsNullOrWhiteSpace(educationLevel) && !AllowedEducationLevels.Contains(educationLevel))
                return BadRequest(ApiResponse<object>.Fail("Invalid education level."));

            var programs = string.IsNullOrWhiteSpace(educationLevel)
                ? await _degree.GetActiveProgramsAsync()
                : await _degree.GetActiveProgramsByLevelAsync(educationLevel);

            var universities = await _degree.GetUniversityOptionsAsync(educationLevel);

            return Ok(ApiResponse<object>.Ok(new
            {
                degreePrograms = programs.Select(p => new
                {
                    programId = p.ProgramId,
                    programName = p.ProgramName,
                    fullName = p.FullName,
                    totalSemesters = p.TotalSemesters
                }),
                universities = universities.Select(u => new
                {
                    universityId = u.UniversityId,
                    name = u.Name
                })
            }));
        }

        // Called right after auth/register succeeds — the JWT from that
        // call authenticates this request. Saves Step1/1B/2/3/4/5 the same
        // way the website's Register POST does, then returns a fresh
        // AuthResponse with profileComplete = true (and a new token, same
        // as Login/Register issue) so the app can go straight to Dashboard.
        [Authorize(Roles = "Student")]
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] OnboardingCompleteRequest req)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (string.IsNullOrWhiteSpace(req.EducationLevel) || !AllowedEducationLevels.Contains(req.EducationLevel))
                return BadRequest(ApiResponse<object>.Fail("Please select a valid education level."));

            bool isGraduate = req.EducationLevel == "Graduate";

            var (s1Ok, s1Msg) = await _auth.SaveStep1Async(userId, new OnboardingStep1VM { EducationLevel = req.EducationLevel });
            if (!s1Ok) return BadRequest(ApiResponse<object>.Fail(s1Msg));

            if (!isGraduate)
            {
                if (req.DegreeProgramId == null || req.CurrentSemester == null)
                    return BadRequest(ApiResponse<object>.Fail("Degree program and current semester are required."));

                var program = await _degree.GetProgramByIdAsync(req.DegreeProgramId.Value);
                if (program == null)
                    return BadRequest(ApiResponse<object>.Fail("Please select a valid degree program."));
                if (program.education_level != req.EducationLevel)
                    return BadRequest(ApiResponse<object>.Fail("That program doesn't match your selected education level."));
                if (req.CurrentSemester < 1 || req.CurrentSemester > program.total_semesters)
                    return BadRequest(ApiResponse<object>.Fail($"{program.program_name} only has {program.total_semesters} semester(s)."));

                var step1b = new OnboardingStep1BVM { DegreeProgramId = req.DegreeProgramId.Value, CurrentSemester = req.CurrentSemester.Value };
                if (!await _degree.SaveStep1BAsync(userId, step1b))
                    return BadRequest(ApiResponse<object>.Fail("Could not save your degree program."));
            }

            if (req.UniversityId == null)
                return BadRequest(ApiResponse<object>.Fail("Please select your university."));

            var university = await _degree.GetUniversityByIdAsync(req.UniversityId.Value);
            if (university == null)
                return BadRequest(ApiResponse<object>.Fail("Please select a valid university from the list."));

            var step2 = new OnboardingStep2VM
            {
                UniversityId = req.UniversityId.Value,
                Major = req.Major ?? ""
            };
            var (s2Ok, s2Msg) = await _auth.SaveStep2Async(userId, step2);
            if (!s2Ok) return BadRequest(ApiResponse<object>.Fail(s2Msg));

            if (!isGraduate)
            {
                var step3 = new OnboardingStep3VM
                {
                    Skills = (req.Skills ?? new()).Select(s => new SkillItemVM { SkillName = s.SkillName, SkillLevel = s.SkillLevel }).ToList(),
                    SkipSkills = req.Skills == null || req.Skills.Count == 0
                };
                var (s3Ok, s3Msg) = await _auth.SaveStep3Async(userId, step3);
                if (!s3Ok) return BadRequest(ApiResponse<object>.Fail(s3Msg));

                var picked = (req.Interests ?? new()).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                var custom = req.CustomInterest?.Trim();
                if (!picked.Any() && string.IsNullOrEmpty(custom))
                    return BadRequest(ApiResponse<object>.Fail("Please select at least one interest, or write your own."));

                var step4 = new OnboardingStep4VM { Interests = picked, CustomInterest = custom };
                var (s4Ok, s4Msg) = await _auth.SaveStep4Async(userId, step4);
                if (!s4Ok) return BadRequest(ApiResponse<object>.Fail(s4Msg));

                if (string.IsNullOrWhiteSpace(req.Goal))
                    return BadRequest(ApiResponse<object>.Fail("Please select your main goal."));

                var step5 = new OnboardingStep5VM { Goal = req.Goal };
                var (s5Ok, s5Msg) = await _auth.SaveStep5Async(userId, step5);
                if (!s5Ok) return BadRequest(ApiResponse<object>.Fail(s5Msg));
            }
            else
            {
                await _auth.MarkGraduateProfileCompleteAsync(userId);
            }

            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));

            var profile = await _users.GetStudentProfileAsync(userId);
            bool profileComplete = profile != null && profile.profile_completion >= 100;

            var token = _jwt.GenerateToken(user.user_id, user.role, user.email);

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                token = token,
                userId = user.user_id,
                name = user.name,
                email = user.email,
                role = user.role,
                profilePicture = user.profile_picture,
                profileComplete = profileComplete,
                uniqueKey = user.unique_key
            }, "Profile complete"));
        }
    }
}
