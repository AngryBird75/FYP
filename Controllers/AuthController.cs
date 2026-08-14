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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService auth, IDegreeService degree, ISkillCatalogService skillCatalog, ILogger<AuthController> logger)
        {
            _auth = auth;
            _degree = degree;
            _skillCatalog = skillCatalog;
            _logger = logger;
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

            var allowed = new[] { "Intermediate", "Undergraduate", "Graduate" };
            if (!allowed.Contains(vm.EducationLevel))
            {
                ModelState.AddModelError(nameof(vm.EducationLevel), "Please select a valid education level.");
                return View(vm);
            }

            HttpContext.Session.SetString("Step1",
                JsonSerializer.Serialize(vm));

            // Intermediate/Undergraduate pick a specific degree program in
            // Step1B — that's what lets Step3 show only the skills relevant
            // to that field of study. Graduate students already hold a
            // completed degree, so there's nothing to track a semester/year
            // for — they go straight to Step2 to name their university, then
            // skip the rest of onboarding entirely (see Step2 POST below).
            if (vm.EducationLevel == "Graduate")
                return RedirectToAction("Step2");

            return RedirectToAction("Step1B");
        }

        // ══════════════════════════════════════════════
        // STEP 1B — Degree Program + Semester
        // Shown for every education level (Intermediate / Undergraduate /
        // Graduate) so we always know the student's field of study.
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Step1B()
        {
            var step1Json = HttpContext.Session.GetString("Step1");
            if (step1Json == null) return RedirectToAction("Step1");

            var step1 = JsonSerializer.Deserialize<OnboardingStep1VM>(step1Json);

            var vm = new OnboardingStep1BVM
            {
                AvailablePrograms = await _degree.GetActiveProgramsByLevelAsync(step1!.EducationLevel)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1B(OnboardingStep1BVM vm)
        {
            var step1Json = HttpContext.Session.GetString("Step1");
            if (step1Json == null) return RedirectToAction("Step1");
            var step1 = JsonSerializer.Deserialize<OnboardingStep1VM>(step1Json);

            vm.AvailablePrograms = await _degree.GetActiveProgramsByLevelAsync(step1!.EducationLevel);

            if (!ModelState.IsValid) return View(vm);

            // Belt-and-braces: the dropdown only shows programs for this
            // student's education level and the JS only enables semesters up
            // to the program's total, but a hand-crafted POST could still
            // send a mismatched program or an out-of-range semester — so we
            // re-check both against the real DB record.
            var program = await _degree.GetProgramByIdAsync(vm.DegreeProgramId);
            if (program == null)
            {
                ModelState.AddModelError(nameof(vm.DegreeProgramId), "Please select a valid degree program.");
                return View(vm);
            }
            if (program.education_level != step1.EducationLevel)
            {
                ModelState.AddModelError(nameof(vm.DegreeProgramId), "That program doesn't match your selected education level.");
                return View(vm);
            }
            if (vm.CurrentSemester < 1 || vm.CurrentSemester > program.total_semesters)
            {
                ModelState.AddModelError(nameof(vm.CurrentSemester),
                    $"{program.program_name} only has {program.total_semesters} semester(s).");
                return View(vm);
            }

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

            var step1ForGet = JsonSerializer.Deserialize<OnboardingStep1VM>(
                HttpContext.Session.GetString("Step1")!)!;

            // Graduate students skip Step1B entirely (see Step1 POST) — every
            // other level must have completed it before reaching here.
            if (step1ForGet.EducationLevel != "Graduate" && HttpContext.Session.GetString("Step1B") == null)
                return RedirectToAction("Step1B");

            var vm = new OnboardingStep2VM
            {
                AvailableUniversities = await _degree.GetUniversityOptionsAsync(step1ForGet.EducationLevel),
                EducationLevel = step1ForGet.EducationLevel
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step2(OnboardingStep2VM vm)
        {
            if (HttpContext.Session.GetString("Step1") == null)
                return RedirectToAction("Step1");

            var step1ForPost = JsonSerializer.Deserialize<OnboardingStep1VM>(
                HttpContext.Session.GetString("Step1")!)!;

            if (step1ForPost.EducationLevel != "Graduate" && HttpContext.Session.GetString("Step1B") == null)
                return RedirectToAction("Step1B");

            vm.EducationLevel = step1ForPost.EducationLevel;
            vm.AvailableUniversities = await _degree.GetUniversityOptionsAsync(step1ForPost.EducationLevel);

            // Graduate students only need to name their university — Major
            // doesn't apply the same way (they may hold a degree in one
            // subject and work in another), so we don't force it here.
            bool isGraduate = step1ForPost.EducationLevel == "Graduate";
            if (isGraduate)
                ModelState.Remove(nameof(vm.Major));

            if (!ModelState.IsValid) return View(vm);

            // Belt-and-braces: confirm the posted id is a real university,
            // not just "a positive number" (which is all [Range] can check).
            var university = await _degree.GetUniversityByIdAsync(vm.UniversityId);
            if (university == null)
            {
                ModelState.AddModelError(nameof(vm.UniversityId), "Please select a valid university from the list.");
                return View(vm);
            }

            HttpContext.Session.SetString("Step2",
                JsonSerializer.Serialize(vm));

            // Graduate students are done onboarding right here — they skip
            // Skills/Interests/Goal (Step3/4/5) and go straight to creating
            // their account. Those can always be filled in later from the
            // profile page once Roadmap-for-graduates ships.
            if (isGraduate)
                return RedirectToAction("Register");

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

            ViewBag.SkillCatalog = await _skillCatalog.GetSkillNamesForProgramAsync(GetSelectedProgramId());
            return View(new OnboardingStep3VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3(OnboardingStep3VM vm)
        {
            ViewBag.SkillCatalog = await _skillCatalog.GetSkillNamesForProgramAsync(GetSelectedProgramId());

            if (vm.SkipSkills)
            {
                vm.Skills = new List<SkillItemVM>();
                HttpContext.Session.SetString("Step3", JsonSerializer.Serialize(vm));
                return RedirectToAction("Step4");
            }

            if (vm.Skills == null || !vm.Skills.Any())
            {
                ModelState.AddModelError("", "Please add at least one skill, or use \"Skip this step\" if you don't have any yet.");
                return View(vm);
            }

            if (vm.Skills.Count > 20)
            {
                ModelState.AddModelError("", "You can add at most 20 skills.");
                return View(vm);
            }

            var dupe = vm.Skills
                .GroupBy(s => (s.SkillName ?? "").Trim().ToLowerInvariant())
                .FirstOrDefault(g => g.Count() > 1);
            if (dupe != null)
            {
                ModelState.AddModelError("", $"'{dupe.First().SkillName}' was added more than once.");
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

        // ══════════════════════════════════════════════
        // STEP 4 — Interests
        // ══════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Step4()
        {
            if (HttpContext.Session.GetString("Step3") == null)
                return RedirectToAction("Step1");

            ViewBag.RecommendedInterests = await GetRecommendedInterestsAsync();
            return View(new OnboardingStep4VM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step4(OnboardingStep4VM vm)
        {
            ViewBag.RecommendedInterests = await GetRecommendedInterestsAsync();

            if (!ModelState.IsValid) return View(vm);

            var picked = vm.Interests?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? new List<string>();
            var custom = vm.CustomInterest?.Trim();

            if (!picked.Any() && string.IsNullOrEmpty(custom))
            {
                ModelState.AddModelError("", "Please select at least one interest, or write your own.");
                return View(vm);
            }

            if (!string.IsNullOrEmpty(custom) &&
                !System.Text.RegularExpressions.Regex.IsMatch(custom, @"^[A-Za-z][A-Za-z\s&\-]{1,49}$"))
            {
                ModelState.AddModelError(nameof(vm.CustomInterest), "Please enter a valid interest (letters only, max 50 characters).");
                return View(vm);
            }

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
            if (HttpContext.Session.GetString("Step1") == null)
                return RedirectToAction("Step1");

            var step1Peek = JsonSerializer.Deserialize<OnboardingStep1VM>(
                HttpContext.Session.GetString("Step1")!);

            // Graduate's onboarding ends at Step2 (university); everyone
            // else must have gone all the way through Step5 (goal).
            bool ready = step1Peek!.EducationLevel == "Graduate"
                ? HttpContext.Session.GetString("Step2") != null
                : HttpContext.Session.GetString("Step5") != null;

            if (!ready) return RedirectToAction("Step1");

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

            if (HttpContext.Session.GetString("Step1") == null)
                return RedirectToAction("Step1");

            var step1 = JsonSerializer.Deserialize<OnboardingStep1VM>(
                HttpContext.Session.GetString("Step1")!);
            bool isGraduate = step1!.EducationLevel == "Graduate";

            // Graduate only goes through Step1 + Step2 (education level +
            // university) — Step1B/3/4/5 don't apply and are expected to be
            // empty. Everyone else must have completed the full 6 steps —
            // if any is missing (tampering, expired session, direct URL
            // hit), bounce back to the start instead of registering with
            // holes.
            bool allStepsPresent = isGraduate
                ? HttpContext.Session.GetString("Step2") != null
                : HttpContext.Session.GetString("Step1B") != null &&
                  HttpContext.Session.GetString("Step2") != null &&
                  HttpContext.Session.GetString("Step3") != null &&
                  HttpContext.Session.GetString("Step4") != null &&
                  HttpContext.Session.GetString("Step5") != null;

            if (!allStepsPresent)
                return RedirectToAction("Step1");

            // Get all step data from session (null for steps Graduate skips)
            var step1b = HttpContext.Session.GetString("Step1B") is string s1b
                ? JsonSerializer.Deserialize<OnboardingStep1BVM>(s1b) : null;
            var step2 = JsonSerializer.Deserialize<OnboardingStep2VM>(
                HttpContext.Session.GetString("Step2")!);
            var step3 = HttpContext.Session.GetString("Step3") is string s3
                ? JsonSerializer.Deserialize<OnboardingStep3VM>(s3) : null;
            var step4 = HttpContext.Session.GetString("Step4") is string s4
                ? JsonSerializer.Deserialize<OnboardingStep4VM>(s4) : null;
            var step5 = HttpContext.Session.GetString("Step5") is string s5
                ? JsonSerializer.Deserialize<OnboardingStep5VM>(s5) : null;

            // Register the user
            var (success, message, user) = await _auth.RegisterAsync(vm);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(vm);
            }

            // Save all onboarding steps. Each of these was already validated
            // in its own step (program/semester match, real university,
            // catalog-matched skills, allowed interests/goal), so failure
            // here should be rare — but we still check, log, and tell the
            // user rather than silently showing a false "success".
            var stepResults = new List<(string Step, bool Ok, string Message)>
            {
                ("Step 1", (await _auth.SaveStep1Async(user!.user_id, step1!)).Item1, "education level"),
            };

            if (!isGraduate)
            {
                bool step1bOk = await _degree.SaveStep1BAsync(user.user_id, step1b!);
                stepResults.Add(("Step 1B", step1bOk, "degree program"));
            }

            var (step2Ok, step2Msg) = await _auth.SaveStep2Async(user.user_id, step2!);
            stepResults.Add(("Step 2", step2Ok, step2Msg));

            if (!isGraduate)
            {
                var (step3Ok, step3Msg) = await _auth.SaveStep3Async(user.user_id, step3!);
                stepResults.Add(("Step 3", step3Ok, step3Msg));

                var (step4Ok, step4Msg) = await _auth.SaveStep4Async(user.user_id, step4!);
                stepResults.Add(("Step 4", step4Ok, step4Msg));

                var (step5Ok, step5Msg) = await _auth.SaveStep5Async(user.user_id, step5!);
                stepResults.Add(("Step 5", step5Ok, step5Msg));
            }
            else
            {
                // Graduate's onboarding is complete right after Step2 —
                // there's no roadmap/skills/goal step for them yet (Roadmap
                // shows as "Coming Soon" on their dashboard), so their
                // profile is considered 100% set up as-is.
                await _auth.MarkGraduateProfileCompleteAsync(user.user_id);
            }

            var failed = stepResults.Where(r => !r.Ok).ToList();
            if (failed.Any())
            {
                _logger.LogWarning("Onboarding save failed for user {UserId}: {Failures}",
                    user.user_id, string.Join("; ", failed.Select(f => $"{f.Step}: {f.Message}")));

                ModelState.AddModelError("",
                    "Your account was created, but we couldn't save all of your profile details. " +
                    "Please log in and complete your profile from the dashboard.");
            }

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

        // Every education level now goes through Step1B, so this is set for
        // all students — it's what lets Step3 filter skills (and Step4
        // suggest interests) to the student's actual field of study.
        private int? GetSelectedProgramId()
        {
            var step1bJson = HttpContext.Session.GetString("Step1B");
            if (step1bJson == null) return null;

            var step1b = JsonSerializer.Deserialize<OnboardingStep1BVM>(step1bJson);
            return step1b?.DegreeProgramId;
        }

        // Maps a degree program's category (Skills.category — "ICS",
        // "Pre-Engineering", "Pre-Medical", "Commerce", "Arts", etc.) to the
        // interest tags that are most likely relevant, so Step4 can show
        // "Recommended for you" instead of six generic, unranked options.
        // Nothing is hidden or blocked — a student can still tick anything —
        // this only guides the order/highlighting.
        private async Task<List<string>> GetRecommendedInterestsAsync()
        {
            var programId = GetSelectedProgramId();
            if (programId == null) return new List<string>();

            var program = await _degree.GetProgramByIdAsync(programId.Value);
            if (program?.category == null) return new List<string>();

            return program.category switch
            {
                "ICS" or "Computer Science" or "Software Engineering" =>
                    new List<string> { "Technology", "Business" },
                "Pre-Engineering" or "Engineering" or "Electrical Engineering"
                    or "Mechanical Engineering" or "Civil Engineering" =>
                    new List<string> { "Engineering", "Technology" },
                "Pre-Medical" or "Medical" =>
                    new List<string> { "Medicine" },
                "Dentistry" =>
                    new List<string> { "Medicine" },
                "Physical Therapy" =>
                    new List<string> { "Medicine" },
                "Pharmacy" =>
                    new List<string> { "Medicine" },
                "Commerce" or "Business" =>
                    new List<string> { "Business" },
                "Accounting & Finance" =>
                    new List<string> { "Business" },
                "Economics" =>
                    new List<string> { "Business", "Technology" },
                "Psychology" =>
                    new List<string> { "Psychology", "Medicine" },
                "English" or "Literature" =>
                    new List<string> { "Civil Services", "Arts" },
                "Mass Communication" =>
                    new List<string> { "Media", "Arts" },
                "Law" =>
                    new List<string> { "Law", "Civil Services" },
                "Architecture" =>
                    new List<string> { "Engineering", "Design" },
                "Fine Arts" =>
                    new List<string> { "Design", "Arts" },
                "Mathematics & Statistics" =>
                    new List<string> { "Technology", "Business" },
                "Arts" or "Humanities" =>
                    new List<string> { "Arts", "Design", "Civil Services" },
                _ => new List<string>()
            };
        }
    }
}