using AspiraHub.Services;
using AspiraHub.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AspiraHub.Controllers
{
    public class AuthController : Controller
    {

        // Constructor mein ISkillCatalogService add karein
        private readonly IAuthService _auth;
        private readonly IDegreeService _degree;
        private readonly ISkillCatalogService _skillCatalog;

        public AuthController(IAuthService auth, IDegreeService degree, ISkillCatalogService skillCatalog)
        {
            _auth = auth;
            _degree = degree;
            _skillCatalog = skillCatalog;
        }

        // ══════════════════════════════════════════════
        // INDEX — Welcome / Splash Screen
        // ══════════════════════════════════════════════
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                string role = HttpContext.Session.GetString("Role") ?? "";
                return role switch
                {
                    "Student" => RedirectToAction("Index", "StudentDashboard"),
                    "Company" => RedirectToAction("Index", "CompanyDashboard"),
                    "Admin" => RedirectToAction("Index", "AdminDashboard"),
                    _ => View()
                };
            }
            return View();
        }

        // ══════════════════════════════════════════════
        // CHOOSE ROLE
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult ChooseRole()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index");
            return View();
        }

        // ══════════════════════════════════════════════
        // STEP 1 — Education Level
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Step1()
            => View(new OnboardingStep1VM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step1(OnboardingStep1VM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            HttpContext.Session.SetString("Step1",
                JsonSerializer.Serialize(vm));

            // Undergraduate students go through Step1B first
            if (vm.EducationLevel == "Undergraduate")
                return RedirectToAction("Step1B");

            return RedirectToAction("Step2");
        }

        // ══════════════════════════════════════════════
        // STEP 1B — Degree Program + Semester
        // Only shown if EducationLevel = "Undergraduate"
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Step1B()
        {
            var step1Json = HttpContext.Session.GetString("Step1");
            if (step1Json == null) return RedirectToAction("Step1");

            var step1 = JsonSerializer.Deserialize<OnboardingStep1VM>(step1Json);

            // Skip this step for non-Undergraduate students
            if (step1!.EducationLevel != "Undergraduate")
                return RedirectToAction("Step2");

            var vm = new OnboardingStep1BVM
            {
                AvailablePrograms = await _degree.GetActiveProgramsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1B(OnboardingStep1BVM vm)
        {
            vm.AvailablePrograms = await _degree.GetActiveProgramsAsync();

            if (!ModelState.IsValid) return View(vm);

            HttpContext.Session.SetString("Step1B",
                JsonSerializer.Serialize(vm));

            return RedirectToAction("Step2");
        }

        // ══════════════════════════════════════════════
        // STEP 2 — Academic Details
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Step2()
        {
            if (HttpContext.Session.GetString("Step1") == null)
                return RedirectToAction("Step1");

            var vm = new OnboardingStep2VM
            {
                AvailableUniversities = await _auth.GetUniversitiesAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step2(OnboardingStep2VM vm)
        {
            vm.AvailableUniversities = await _auth.GetUniversitiesAsync();

            if (!ModelState.IsValid) return View(vm);

            // extra safety: confirm the selected id actually exists in DB
            if (!vm.AvailableUniversities.Any(u => u.UniversityId == vm.UniversityId))
            {
                ModelState.AddModelError(nameof(vm.UniversityId), "Please select a valid university from the list.");
                return View(vm);
            }

            HttpContext.Session.SetString("Step2",
                JsonSerializer.Serialize(vm));

            return RedirectToAction("Step3");
        }

        // ══════════════════════════════════════════════
        // STEP 3 — Skills (UPDATED: real catalog + validation)
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Step3()
        {
            if (HttpContext.Session.GetString("Step2") == null)
                return RedirectToAction("Step1");

            ViewBag.SkillCatalog = await GetRelevantSkillCatalogAsync();
            return View(new OnboardingStep3VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3(OnboardingStep3VM vm)
        {
            ViewBag.SkillCatalog = await GetRelevantSkillCatalogAsync();

            if (vm.Skills == null || !vm.Skills.Any())
            {
                ModelState.AddModelError("", "Please add at least one skill.");
                return View(vm);
            }

            var allowedLevels = new[] { "Beginner", "Intermediate", "Advanced" };
            foreach (var s in vm.Skills)
            {
                if (!allowedLevels.Contains(s.SkillLevel))
                {
                    ModelState.AddModelError("", $"Invalid level for '{s.SkillName}'.");
                    return View(vm);
                }

                var matched = await _skillCatalog.FindByNameAsync(s.SkillName);
                if (matched == null)
                {
                    ModelState.AddModelError("", $"'{s.SkillName}' isn't a recognized skill — please pick from the suggestions.");
                    return View(vm);
                }
            }

            HttpContext.Session.SetString("Step3", JsonSerializer.Serialize(vm));
            return RedirectToAction("Step4");
        }

        // Step1B (Undergraduate students) session mein DegreeProgramId hoti hai —
        // usi se sirf us department ki relevant skills nikalte hain.
        // Agar Step1B nahi hua (Intermediate/Graduate), poori catalog dikhti hai.
        private async Task<List<string>> GetRelevantSkillCatalogAsync()
        {
            var step1bJson = HttpContext.Session.GetString("Step1B");
            if (step1bJson != null)
            {
                var step1b = JsonSerializer.Deserialize<OnboardingStep1BVM>(step1bJson);
                if (step1b != null && step1b.DegreeProgramId > 0)
                    return await _skillCatalog.GetSkillNamesByProgramAsync(step1b.DegreeProgramId);
            }

            return await _skillCatalog.GetAllSkillNamesAsync();
        }

        // ══════════════════════════════════════════════
        // STEP 4 — Interests
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Step4()
        {
            if (HttpContext.Session.GetString("Step3") == null)
                return RedirectToAction("Step1");
            return View(new OnboardingStep4VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step4(OnboardingStep4VM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            HttpContext.Session.SetString("Step4",
                JsonSerializer.Serialize(vm));

            return RedirectToAction("Step5");
        }

        // ══════════════════════════════════════════════
        // STEP 5 — Goals
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Step5()
        {
            if (HttpContext.Session.GetString("Step4") == null)
                return RedirectToAction("Step1");
            return View(new OnboardingStep5VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step5(OnboardingStep5VM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            HttpContext.Session.SetString("Step5",
                JsonSerializer.Serialize(vm));

            return RedirectToAction("Register");
        }

        // ══════════════════════════════════════════════
        // REGISTER + SHOW KEY — Last Step (Student)
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("Step5") == null)
                return RedirectToAction("Step1");

            return View(new RegisterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (vm.Password != vm.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return View(vm);
            }

            // Get all step data from session
            var step1 = JsonSerializer.Deserialize<OnboardingStep1VM>(
                HttpContext.Session.GetString("Step1")!);
            var step1bJson = HttpContext.Session.GetString("Step1B");
            var step1b = step1bJson != null
                ? JsonSerializer.Deserialize<OnboardingStep1BVM>(step1bJson)
                : null;
            var step2 = JsonSerializer.Deserialize<OnboardingStep2VM>(
                HttpContext.Session.GetString("Step2")!);
            var step3 = JsonSerializer.Deserialize<OnboardingStep3VM>(
                HttpContext.Session.GetString("Step3")!);
            var step4 = JsonSerializer.Deserialize<OnboardingStep4VM>(
                HttpContext.Session.GetString("Step4")!);
            var step5 = JsonSerializer.Deserialize<OnboardingStep5VM>(
                HttpContext.Session.GetString("Step5")!);

            // Register the user
            var (success, message, user) = await _auth.RegisterAsync(vm);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(vm);
            }

            // Save all onboarding steps
            await _auth.SaveStep1Async(user!.user_id, step1!);
            if (step1b != null)
                await _degree.SaveStep1BAsync(user.user_id, step1b);
            await _auth.SaveStep2Async(user.user_id, step2!);
            await _auth.SaveStep3Async(user.user_id, step3!);
            await _auth.SaveStep4Async(user.user_id, step4!);
            await _auth.SaveStep5Async(user.user_id, step5!);

            // Clear step session data
            HttpContext.Session.Remove("Step1");
            HttpContext.Session.Remove("Step1B");
            HttpContext.Session.Remove("Step2");
            HttpContext.Session.Remove("Step3");
            HttpContext.Session.Remove("Step4");
            HttpContext.Session.Remove("Step5");

            // Set user session
            HttpContext.Session.SetInt32("UserId", user.user_id);
            HttpContext.Session.SetString("Role", user.role);
            HttpContext.Session.SetString("Username", user.name);
            HttpContext.Session.SetString("UniqueKey", user.unique_key ?? "");

            // Show the generated key on the same page
            ViewBag.GeneratedKey = user.unique_key;
            ViewBag.Registered = true;

            return View(vm);
        }

        // ══════════════════════════════════════════════
        // COMPANY REGISTER
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult CompanyRegister()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index");
            return View(new RegisterVM { Role = "Company" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyRegister(RegisterVM vm)
        {
            vm.Role = "Company";

            if (!ModelState.IsValid) return View(vm);

            if (vm.Password != vm.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return View(vm);
            }

            var (success, message, user) = await _auth.RegisterAsync(vm);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(vm);
            }

            HttpContext.Session.SetInt32("UserId", user!.user_id);
            HttpContext.Session.SetString("Role", user.role);
            HttpContext.Session.SetString("Username", user.name);
            HttpContext.Session.SetString("UniqueKey", "");

            ViewBag.Registered = true;
            return View(vm);
        }

        // ══════════════════════════════════════════════
        // LOGIN
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index");
            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (success, message, user) = await _auth.LoginAsync(vm);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(vm);
            }

            HttpContext.Session.SetInt32("UserId", user!.user_id);
            HttpContext.Session.SetString("Role", user.role);
            HttpContext.Session.SetString("Username", user.name);
            HttpContext.Session.SetString("UniqueKey", user.unique_key ?? "");

            if (user.role == "Student")
                return RedirectToAction("Index", "StudentDashboard");

            if (user.role == "Company")
                return RedirectToAction("Index", "CompanyDashboard");

            if (user.role == "Admin")
                return RedirectToAction("Index", "AdminDashboard");

            return RedirectToAction("Index");
        }

        // ══════════════════════════════════════════════
        // ADMIN LOGIN
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (HttpContext.Session.GetString("Role") == "Admin")
                return RedirectToAction("Index", "AdminDashboard");
            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (success, message, user) = await _auth.LoginAsync(vm);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(vm);
            }

            // Only the Admin role is allowed through this screen
            if (user!.role != "Admin")
            {
                ModelState.AddModelError("", "Access denied. Admin credentials required.");
                return View(vm);
            }

            HttpContext.Session.SetInt32("UserId", user.user_id);
            HttpContext.Session.SetString("Role", user.role);
            HttpContext.Session.SetString("Username", user.name);
            HttpContext.Session.SetString("UniqueKey", "");

            return RedirectToAction("Index", "AdminDashboard");
        }

        // ══════════════════════════════════════════════
        // FORGOT PASSWORD
        // ══════════════════════════════════════════════
        [HttpGet]
        public IActionResult ForgotPassword()
            => View(new ForgotPasswordVM());

        // Step 1: email submitted → send OTP, move UI to Step 2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Email) || !new EmailAddressAttribute().IsValid(vm.Email))
            {
                ModelState.AddModelError(nameof(vm.Email), "Please enter a valid email.");
                vm.Step = 1;
                return View(vm);
            }

            var (success, message) = await _auth.ForgotPasswordAsync(vm.Email);

            ViewBag.Success = success;
            ViewBag.Message = message;
            vm.Step = 2;

            return View(vm);
        }

        // Step 2: OTP submitted → verify, move UI to Step 3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(ForgotPasswordVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.OtpCode))
            {
                ModelState.AddModelError(nameof(vm.OtpCode), "Please enter the OTP.");
                vm.Step = 2;
                return View("ForgotPassword", vm);
            }

            var (success, message) = await _auth.VerifyOtpAsync(vm.Email, vm.OtpCode);

            if (!success)
            {
                ModelState.AddModelError("", message);
                vm.Step = 2;
                return View("ForgotPassword", vm);
            }

            ViewBag.Success = true;
            vm.Step = 3;
            return View("ForgotPassword", vm);
        }

        // Step 3: new password submitted → reset it
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ForgotPasswordVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewPassword) || vm.NewPassword.Length < 8)
            {
                ModelState.AddModelError(nameof(vm.NewPassword), "Password must be at least 8 characters.");
                vm.Step = 3;
                return View("ForgotPassword", vm);
            }

            if (vm.NewPassword != vm.ConfirmNewPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmNewPassword), "Passwords do not match.");
                vm.Step = 3;
                return View("ForgotPassword", vm);
            }

            var (success, message) = await _auth.ResetPasswordAsync(vm.Email, vm.OtpCode ?? "", vm.NewPassword);

            if (!success)
            {
                ModelState.AddModelError("", message);
                vm.Step = 2; // OTP may have expired between steps — bounce back to Step 2
                return View("ForgotPassword", vm);
            }

            TempData["ResetSuccess"] = "Password reset successful. Please log in with your new password.";
            return RedirectToAction("Login");
        }

        // ══════════════════════════════════════════════
        // LOGOUT
        // ══════════════════════════════════════════════
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData.Clear();
            return RedirectToAction("Index");
        }


        // ══════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════
        private bool IsLoggedIn()
            => HttpContext.Session.GetInt32("UserId") != null;

        private bool IsStudent()
            => HttpContext.Session.GetString("Role") == "Student";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}