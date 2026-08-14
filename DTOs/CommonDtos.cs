namespace AspiraHub.DTOs
{
    // ── Generic wrapper every endpoint returns, so the Android app has
    // one predictable shape to parse (success/message/data). ──
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public T? data { get; set; }

        public static ApiResponse<T> Ok(T data, string msg = "OK") =>
            new() { success = true, message = msg, data = data };

        public static ApiResponse<T> Fail(string msg) =>
            new() { success = false, message = msg, data = default };
    }

    public class LoginRequest
    {
        public string email { get; set; } = "";
        public string password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string password { get; set; } = "";
        public string role { get; set; } = "Student"; // Student / Company
    }

    public class AuthResponse
    {
        public string token { get; set; } = "";
        public int userId { get; set; }
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string role { get; set; } = "";
        public string? profilePicture { get; set; }
        public bool profileComplete { get; set; }
        // Only populated right after Student registration/onboarding
        // completes — same moment the website's ShowKey reveal happens.
        // Null on ordinary logins (the key doesn't change, so there's no
        // need to resend it every time — the dashboard already shows it).
        public string? uniqueKey { get; set; }
    }

    public class ApplyJobRequest
    {
        public int jobId { get; set; }
        public string? coverLetter { get; set; }
    }

    public class UpdateStepStatusRequest
    {
        public string newStatus { get; set; } = ""; // NotStarted / InProgress / Completed
    }

    public class RegisterDeviceRequest
    {
        public string fcmToken { get; set; } = "";
    }

    // Matches the Android app's OnboardingCompleteRequest exactly (field
    // for field) — the whole point is a single POST after auth/register
    // instead of six separate step calls.
    public class OnboardingCompleteRequest
    {
        public string EducationLevel { get; set; } = ""; // Intermediate / Undergraduate / Graduate
        public int? DegreeProgramId { get; set; }
        public int? CurrentSemester { get; set; }
        public int? UniversityId { get; set; }
        public string? Major { get; set; }
        public List<OnboardingSkillDto> Skills { get; set; } = new();
        public List<string> Interests { get; set; } = new();
        public string? CustomInterest { get; set; }
        public string Goal { get; set; } = ""; // Get a Job / Freelancing / Higher Education / Start a Business / Just Learning
    }

    public class OnboardingSkillDto
    {
        public string SkillName { get; set; } = "";
        public string SkillLevel { get; set; } = "";
    }
}
