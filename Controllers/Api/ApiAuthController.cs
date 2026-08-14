using AspiraHub.DTOs;
using AspiraHub.Repositories;
using AspiraHub.Services;
using AspiraHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mobile-only JSON auth. Reuses the exact same IAuthService the
    // website uses (same password hashing, same validation rules) —
    // only difference is we hand back a JWT instead of a session cookie.
    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IUserRepository _users;
        private readonly IJwtService _jwt;

        public ApiAuthController(IAuthService auth, IUserRepository users, IJwtService jwt)
        {
            _auth = auth;
            _users = users;
            _jwt = jwt;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest req)
        {
            var vm = new LoginVM { LoginIdentifier = req.email, Password = req.password };
            var (success, message, user) = await _auth.LoginAsync(vm);

            if (!success || user == null)
                return Unauthorized(ApiResponse<AuthResponse>.Fail(message));

            bool profileComplete = false;
            if (user.role == "Student")
            {
                var profile = await _users.GetStudentProfileAsync(user.user_id);
                profileComplete = profile != null && profile.profile_completion >= 100;
            }

            var token = _jwt.GenerateToken(user.user_id, user.role, user.email);

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                token = token,
                userId = user.user_id,
                name = user.name,
                email = user.email,
                role = user.role,
                profilePicture = user.profile_picture,
                profileComplete = profileComplete
            }, "Login successful"));
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest req)
        {
            var vm = new RegisterVM
            {
                FullName = req.name,
                Email = req.email,
                Password = req.password,
                ConfirmPassword = req.password,
                Role = req.role
            };

            var (success, message, user) = await _auth.RegisterAsync(vm);
            if (!success || user == null)
                return BadRequest(ApiResponse<AuthResponse>.Fail(message));

            var token = _jwt.GenerateToken(user.user_id, user.role, user.email);

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                token = token,
                userId = user.user_id,
                name = user.name,
                email = user.email,
                role = user.role,
                profileComplete = false
            }, "Account created — complete onboarding next"));
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] string email)
        {
            var (success, message) = await _auth.ForgotPasswordAsync(email);
            return success ? Ok(ApiResponse<string>.Ok("", message)) : BadRequest(ApiResponse<string>.Fail(message));
        }

        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<string>>> VerifyOtp([FromQuery] string email, [FromQuery] string otp)
        {
            var (success, message) = await _auth.VerifyOtpAsync(email, otp);
            return success ? Ok(ApiResponse<string>.Ok("", message)) : BadRequest(ApiResponse<string>.Fail(message));
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
            [FromQuery] string email, [FromQuery] string otp, [FromQuery] string newPassword)
        {
            var (success, message) = await _auth.ResetPasswordAsync(email, otp, newPassword);
            return success ? Ok(ApiResponse<string>.Ok("", message)) : BadRequest(ApiResponse<string>.Fail(message));
        }

        // Quick token check so the app can silently verify a saved token
        // on startup before deciding whether to show Login or Dashboard.
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Me()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound(ApiResponse<AuthResponse>.Fail("User not found"));

            bool profileComplete = false;
            if (user.role == "Student")
            {
                var profile = await _users.GetStudentProfileAsync(user.user_id);
                profileComplete = profile != null && profile.profile_completion >= 100;
            }

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                token = "",
                userId = user.user_id,
                name = user.name,
                email = user.email,
                role = user.role,
                profilePicture = user.profile_picture,
                profileComplete = profileComplete
            }));
        }
    }
}
