using AspiraHub.Models;
using AspiraHub.ViewModels;

namespace AspiraHub.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)>
            RegisterAsync(RegisterVM vm);

        Task<(bool Success, string Message, User? User)>
            LoginAsync(LoginVM vm);

        Task<(bool Success, string Message)>
            SaveStep1Async(int userId, OnboardingStep1VM vm);

        Task<(bool Success, string Message)>
            SaveStep2Async(int userId, OnboardingStep2VM vm);

        // NEW: for Step2 university dropdown (validated, DB-driven)
        Task<List<UniversityOption>> GetUniversitiesAsync();

        Task<(bool Success, string Message)>
            SaveStep3Async(int userId, OnboardingStep3VM vm);

        Task<(bool Success, string Message)>
            SaveStep4Async(int userId, OnboardingStep4VM vm);

        Task<(bool Success, string Message)>
            SaveStep5Async(int userId, OnboardingStep5VM vm);

        Task<(bool Success, string Message)>
            ForgotPasswordAsync(string email);

        Task<(bool Success, string Message)>
            VerifyOtpAsync(string email, string otp);

        Task<(bool Success, string Message)>
            ResetPasswordAsync(string email, string otp, string newPassword);
    }
}