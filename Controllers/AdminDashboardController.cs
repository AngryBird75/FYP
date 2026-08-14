using AspiraHub.Repositories;
using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.Services;
using AspiraHub.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly IDashboardRepository _repo;
        private readonly AppDbContext _db;
        private readonly IPlatformSettingsService _settings;

        public AdminDashboardController(IDashboardRepository repo, AppDbContext db, IPlatformSettingsService settings)
        {
            _repo = repo;
            _db = db;
            _settings = settings;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var vm = await _repo.GetAdminDashboardAsync();
            return View(vm);
        }

        public class UserIdRequest { public int UserId { get; set; } }

        // Toggle User Active/Inactive
        [HttpPost]
        public async Task<IActionResult> ToggleUser([FromBody] UserIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            user.is_active = !user.is_active;
            await _db.SaveChangesAsync();

            // Log
            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = user.is_active ? "Activated User" : "Deactivated User",
                target_table = "Users",
                target_id = req.UserId,
                details = $"User {user.name} ({user.role}) status changed",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = user.is_active });
        }

        public class AddAnnouncementRequest
        {
            public string Title { get; set; } = "";
            public string Content { get; set; } = "";
            public string TargetRole { get; set; } = "";
        }

        // Add Announcement
        [HttpPost]
        public async Task<IActionResult> AddAnnouncement([FromBody] AddAnnouncementRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.Title))
                return Json(new { success = false, message = "Title is required" });

            _db.Announcements.Add(new Announcement
            {
                admin_id = GetUserId(),
                title = req.Title,
                content = req.Content,
                // The "All Users" option in the form sends an empty string,
                // but the DB's CK_Announcements_Role constraint only allows
                // 'Student' / 'Company' / NULL — not "". NULL is what the
                // rest of the app already treats as "all users" (see the
                // `ann.target_role ?? "All"` display logic).
                target_role = string.IsNullOrWhiteSpace(req.TargetRole) ? null : req.TargetRole,
                is_active = true,
                published_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class IdRequest { public int Id { get; set; } }

        // Delete Announcement
        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement([FromBody] IdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var ann = await _db.Announcements.FindAsync(req.Id);
            if (ann != null)
            {
                ann.is_active = false;
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        public class MetricsSettingsRequest
        {
            public double MarketingSpend { get; set; }
            public double AvgRevenuePerCustomer { get; set; }
        }

        // Update the assumptions used by the CPA / CLV formulas
        // (Marketing Spend + Avg Revenue per Customer)
        [HttpPost]
        public async Task<IActionResult> UpdateMetricsSettings([FromBody] MetricsSettingsRequest request)
        {
            if (!IsAdmin()) return Unauthorized();
            if (request == null) return Json(new { success = false, message = "Invalid request" });

            if (request.MarketingSpend < 0 || request.AvgRevenuePerCustomer < 0)
                return Json(new { success = false, message = "Values cannot be negative" });

            await _settings.SaveSettingsAsync(new PlatformSettings
            {
                MarketingSpend = request.MarketingSpend,
                AvgRevenuePerCustomer = request.AvgRevenuePerCustomer
            });

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Updated Analytics Assumptions",
                target_table = "PlatformSettings",
                details = $"Marketing Spend = {request.MarketingSpend}, Avg Revenue/Customer = {request.AvgRevenuePerCustomer}",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════
        // USERS MANAGEMENT PAGE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Users(string? search, string? role, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => u.name.ToLower().Contains(s) || u.email.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                query = query.Where(u => u.role == role);
            }

            int totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.created_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserRowVM
                {
                    UserId = u.user_id,
                    Name = u.name,
                    Email = u.email,
                    Role = u.role,
                    IsActive = u.is_active,
                    CreatedAt = u.created_at,
                    UniqueKey = u.unique_key
                })
                .ToListAsync();

            var vm = new AdminUsersListVM
            {
                Users = users,
                Search = search ?? "",
                RoleFilter = role ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        public class ChangeRoleRequest { public int UserId { get; set; } public string NewRole { get; set; } = ""; }

        // Change a user's role (Student / Company / Admin)
        [HttpPost]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || req.NewRole is not ("Student" or "Company" or "Admin"))
                return Json(new { success = false, message = "Invalid role" });

            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            var oldRole = user.role;
            var newRole = req.NewRole;
            if (oldRole == newRole) return Json(new { success = true });

            user.role = newRole;
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Changed User Role",
                target_table = "Users",
                target_id = req.UserId,
                details = $"{user.name}: {oldRole} → {newRole}",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Permanently delete a user and all their dependent data
        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] UserIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            int userId = req.UserId;

            if (userId == GetUserId())
                return Json(new { success = false, message = "You cannot delete your own account while logged in." });

            var user = await _db.Users
                .Include(u => u.StudentProfile)
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (user == null) return Json(new { success = false, message = "User not found" });

            var name = user.name;
            var role = user.role;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (user.StudentProfile != null)
                {
                    int sid = user.StudentProfile.student_id;
                    var roadmapIds = await _db.Roadmaps.Where(r => r.student_id == sid).Select(r => r.roadmap_id).ToListAsync();

                    _db.RoadmapProgresses.RemoveRange(_db.RoadmapProgresses.Where(rp => rp.student_id == sid));
                    _db.RoadmapSteps.RemoveRange(_db.RoadmapSteps.Where(rs => roadmapIds.Contains(rs.roadmap_id)));
                    _db.Roadmaps.RemoveRange(_db.Roadmaps.Where(r => r.student_id == sid));
                    _db.JobApplications.RemoveRange(_db.JobApplications.Where(a => a.student_id == sid));
                    _db.JobMatchings.RemoveRange(_db.JobMatchings.Where(m => m.student_id == sid));
                    _db.StudentSkills.RemoveRange(_db.StudentSkills.Where(ss => ss.student_id == sid));
                    _db.StudentCourses.RemoveRange(_db.StudentCourses.Where(sc => sc.student_id == sid));
                    _db.SkillGapAnalyses.RemoveRange(_db.SkillGapAnalyses.Where(g => g.student_id == sid));
                    _db.CareerComparisons.RemoveRange(_db.CareerComparisons.Where(c => c.student_id == sid));
                }

                if (user.CompanyProfile != null)
                {
                    int cid = user.CompanyProfile.company_id;
                    var jobIds = await _db.JobPostings.Where(j => j.company_id == cid).Select(j => j.job_id).ToListAsync();

                    _db.JobApplications.RemoveRange(_db.JobApplications.Where(a => jobIds.Contains(a.job_id)));
                    _db.JobSkills.RemoveRange(_db.JobSkills.Where(js => jobIds.Contains(js.job_id)));
                    _db.JobMatchings.RemoveRange(_db.JobMatchings.Where(m => jobIds.Contains(m.job_id)));
                    _db.JobPostings.RemoveRange(_db.JobPostings.Where(j => j.company_id == cid));
                }

                _db.SavedItems.RemoveRange(_db.SavedItems.Where(si => si.user_id == userId));
                _db.Notifications.RemoveRange(_db.Notifications.Where(n => n.user_id == userId));
                _db.ChatMessages.RemoveRange(_db.ChatMessages.Where(c => c.user_id == userId));
                _db.PasswordResets.RemoveRange(_db.PasswordResets.Where(p => p.user_id == userId));

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();

                _db.AdminLogs.Add(new AdminLog
                {
                    admin_id = GetUserId(),
                    action = "Deleted User",
                    target_table = "Users",
                    target_id = userId,
                    details = $"Permanently deleted {name} ({role}) and all related data",
                    created_at = DateTime.Now
                });
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = "Could not delete this user — please try again or deactivate the account instead." });
            }
        }

        // ══════════════════════════════════════════════
        // DEGREE PROGRAMS MANAGEMENT PAGE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Programs(string? search, string? level, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.DegreePrograms.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.program_name.ToLower().Contains(s)
                                       || (p.full_name != null && p.full_name.ToLower().Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(level) && level != "All")
            {
                query = query.Where(p => p.education_level == level);
            }

            int totalCount = await query.CountAsync();

            var programs = await query
                .OrderBy(p => p.education_level).ThenBy(p => p.program_name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProgramRowVM
                {
                    ProgramId = p.program_id,
                    ProgramName = p.program_name,
                    FullName = p.full_name,
                    EducationLevel = p.education_level,
                    TotalSemesters = p.total_semesters,
                    Category = p.category,
                    IsActive = p.is_active,
                    StudentsCount = _db.StudentProfiles.Count(sp => sp.degree_program_id == p.program_id)
                })
                .ToListAsync();

            var vm = new AdminProgramsListVM
            {
                Programs = programs,
                Search = search ?? "",
                LevelFilter = level ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        public class AddProgramRequest
        {
            public string ProgramName { get; set; } = "";
            public string? FullName { get; set; }
            public string EducationLevel { get; set; } = "";
            public int TotalSemesters { get; set; } = 8;
            public string? Category { get; set; }
        }

        // Add a brand-new degree program to the catalog
        [HttpPost]
        public async Task<IActionResult> AddProgram([FromBody] AddProgramRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.ProgramName))
                return Json(new { success = false, message = "Program name is required" });
            if (req.EducationLevel is not ("Intermediate" or "Undergraduate" or "Graduate"))
                return Json(new { success = false, message = "Please select a valid education level" });
            if (req.TotalSemesters < 1 || req.TotalSemesters > 12)
                return Json(new { success = false, message = "Total semesters must be between 1 and 12" });

            bool exists = await _db.DegreePrograms.AnyAsync(p =>
                p.program_name.ToLower() == req.ProgramName.Trim().ToLower() &&
                p.education_level == req.EducationLevel);
            if (exists)
                return Json(new { success = false, message = "A program with this name already exists for this education level" });

            var program = new DegreeProgram
            {
                program_name = req.ProgramName.Trim(),
                full_name = string.IsNullOrWhiteSpace(req.FullName) ? null : req.FullName.Trim(),
                education_level = req.EducationLevel,
                total_semesters = req.TotalSemesters,
                category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category.Trim(),
                is_active = true
            };
            _db.DegreePrograms.Add(program);
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Added Program",
                target_table = "DegreePrograms",
                target_id = program.program_id,
                details = $"Added program '{program.program_name}' ({program.education_level})",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, id = program.program_id });
        }

        public class UpdateProgramRequest
        {
            public int ProgramId { get; set; }
            public string ProgramName { get; set; } = "";
            public string? FullName { get; set; }
            public string EducationLevel { get; set; } = "";
            public int TotalSemesters { get; set; } = 8;
            public string? Category { get; set; }
        }

        // Edit an existing degree program
        [HttpPost]
        public async Task<IActionResult> UpdateProgram([FromBody] UpdateProgramRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.ProgramName))
                return Json(new { success = false, message = "Program name is required" });
            if (req.EducationLevel is not ("Intermediate" or "Undergraduate" or "Graduate"))
                return Json(new { success = false, message = "Please select a valid education level" });
            if (req.TotalSemesters < 1 || req.TotalSemesters > 12)
                return Json(new { success = false, message = "Total semesters must be between 1 and 12" });

            var program = await _db.DegreePrograms.FindAsync(req.ProgramId);
            if (program == null) return Json(new { success = false, message = "Program not found" });

            bool exists = await _db.DegreePrograms.AnyAsync(p =>
                p.program_id != req.ProgramId &&
                p.program_name.ToLower() == req.ProgramName.Trim().ToLower() &&
                p.education_level == req.EducationLevel);
            if (exists)
                return Json(new { success = false, message = "A program with this name already exists for this education level" });

            program.program_name = req.ProgramName.Trim();
            program.full_name = string.IsNullOrWhiteSpace(req.FullName) ? null : req.FullName.Trim();
            program.education_level = req.EducationLevel;
            program.total_semesters = req.TotalSemesters;
            program.category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category.Trim();
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Updated Program",
                target_table = "DegreePrograms",
                target_id = program.program_id,
                details = $"Updated program '{program.program_name}'",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Toggle a program between Active / Inactive (soft delete — hides it from
        // the onboarding picker without breaking students already enrolled in it)
        [HttpPost]
        public async Task<IActionResult> ToggleProgramActive([FromBody] IdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var program = await _db.DegreePrograms.FindAsync(req.Id);
            if (program == null) return Json(new { success = false, message = "Program not found" });

            program.is_active = !program.is_active;
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = program.is_active ? "Activated Program" : "Deactivated Program",
                target_table = "DegreePrograms",
                target_id = program.program_id,
                details = $"Program '{program.program_name}' status changed",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = program.is_active });
        }

        // Permanently delete a program — only allowed when no student is enrolled in it
        [HttpPost]
        public async Task<IActionResult> DeleteProgram([FromBody] IdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var program = await _db.DegreePrograms.FindAsync(req.Id);
            if (program == null) return Json(new { success = false, message = "Program not found" });

            bool inUse = await _db.StudentProfiles.AnyAsync(sp => sp.degree_program_id == req.Id);
            if (inUse)
                return Json(new { success = false, message = "This program is assigned to one or more students — deactivate it instead of deleting." });

            var name = program.program_name;

            try
            {
                _db.DegreePrograms.Remove(program);
                await _db.SaveChangesAsync();

                _db.AdminLogs.Add(new AdminLog
                {
                    admin_id = GetUserId(),
                    action = "Deleted Program",
                    target_table = "DegreePrograms",
                    target_id = req.Id,
                    details = $"Deleted program '{name}'",
                    created_at = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not delete this program — please try again or deactivate it instead." });
            }
        }

        // ══════════════════════════════════════════════
        // CAREERS / GOALS MANAGEMENT PAGE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Careers(string? search, string? demand, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.Careers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.title.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(demand) && demand != "All")
            {
                query = query.Where(c => c.demand_level == demand);
            }

            int totalCount = await query.CountAsync();

            var pageIds = await query
                .OrderBy(c => c.title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => c.career_id)
                .ToListAsync();

            var skillCounts = await _db.CareerSkills
                .Where(cs => pageIds.Contains(cs.career_id))
                .GroupBy(cs => cs.career_id)
                .Select(g => new { CareerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var templateCounts = await _db.RoadmapTemplates
                .Where(t => t.career_id != null && pageIds.Contains(t.career_id.Value))
                .GroupBy(t => t.career_id)
                .Select(g => new { CareerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var careers = await _db.Careers
                .Where(c => pageIds.Contains(c.career_id))
                .OrderBy(c => c.title)
                .Select(c => new AdminCareerRowVM
                {
                    CareerId = c.career_id,
                    Title = c.title,
                    Description = c.description,
                    AverageSalary = c.average_salary,
                    Scope = c.scope,
                    DemandLevel = c.demand_level,
                    JobMarketTrend = c.job_market_trend
                })
                .ToListAsync();

            foreach (var c in careers)
            {
                c.SkillsCount = skillCounts.FirstOrDefault(s => s.CareerId == c.CareerId)?.Count ?? 0;
                c.RoadmapTemplatesCount = templateCounts.FirstOrDefault(t => t.CareerId == c.CareerId)?.Count ?? 0;
            }

            var vm = new AdminCareersListVM
            {
                Careers = careers,
                Search = search ?? "",
                DemandFilter = demand ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        public class AddCareerRequest
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public int? AverageSalary { get; set; }
            public string? Scope { get; set; }
            public string? DemandLevel { get; set; }
            public string? JobMarketTrend { get; set; }
        }

        // Add a brand-new career / goal to the catalog
        [HttpPost]
        public async Task<IActionResult> AddCareer([FromBody] AddCareerRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.Title))
                return Json(new { success = false, message = "Career title is required" });
            if (req.AverageSalary.HasValue && req.AverageSalary.Value < 0)
                return Json(new { success = false, message = "Average salary cannot be negative" });

            bool exists = await _db.Careers.AnyAsync(c => c.title.ToLower() == req.Title.Trim().ToLower());
            if (exists)
                return Json(new { success = false, message = "A career with this title already exists" });

            var career = new Career
            {
                title = req.Title.Trim(),
                description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                average_salary = req.AverageSalary,
                scope = string.IsNullOrWhiteSpace(req.Scope) ? null : req.Scope.Trim(),
                demand_level = string.IsNullOrWhiteSpace(req.DemandLevel) ? "Medium" : req.DemandLevel,
                job_market_trend = string.IsNullOrWhiteSpace(req.JobMarketTrend) ? "Stable" : req.JobMarketTrend
            };
            _db.Careers.Add(career);
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Added Career",
                target_table = "Careers",
                target_id = career.career_id,
                details = $"Added career goal '{career.title}'",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, id = career.career_id });
        }

        public class UpdateCareerRequest
        {
            public int CareerId { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public int? AverageSalary { get; set; }
            public string? Scope { get; set; }
            public string? DemandLevel { get; set; }
            public string? JobMarketTrend { get; set; }
        }

        // Edit an existing career / goal
        [HttpPost]
        public async Task<IActionResult> UpdateCareer([FromBody] UpdateCareerRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null || string.IsNullOrWhiteSpace(req.Title))
                return Json(new { success = false, message = "Career title is required" });
            if (req.AverageSalary.HasValue && req.AverageSalary.Value < 0)
                return Json(new { success = false, message = "Average salary cannot be negative" });

            var career = await _db.Careers.FindAsync(req.CareerId);
            if (career == null) return Json(new { success = false, message = "Career not found" });

            bool exists = await _db.Careers.AnyAsync(c =>
                c.career_id != req.CareerId && c.title.ToLower() == req.Title.Trim().ToLower());
            if (exists)
                return Json(new { success = false, message = "A career with this title already exists" });

            career.title = req.Title.Trim();
            career.description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            career.average_salary = req.AverageSalary;
            career.scope = string.IsNullOrWhiteSpace(req.Scope) ? null : req.Scope.Trim();
            career.demand_level = string.IsNullOrWhiteSpace(req.DemandLevel) ? "Medium" : req.DemandLevel;
            career.job_market_trend = string.IsNullOrWhiteSpace(req.JobMarketTrend) ? "Stable" : req.JobMarketTrend;
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Updated Career",
                target_table = "Careers",
                target_id = career.career_id,
                details = $"Updated career goal '{career.title}'",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Permanently delete a career — blocked while students have real data tied to
        // it (roadmaps, comparisons, skill-gap reports); its catalog-only mappings
        // (skills, roadmap templates) are cleaned up automatically.
        [HttpPost]
        public async Task<IActionResult> DeleteCareer([FromBody] IdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var career = await _db.Careers.FindAsync(req.Id);
            if (career == null) return Json(new { success = false, message = "Career not found" });

            bool inUse = await _db.Roadmaps.AnyAsync(r => r.career_id == req.Id)
                || await _db.SkillGapAnalyses.AnyAsync(g => g.career_id == req.Id)
                || await _db.CareerComparisons.AnyAsync(c => c.career_id_1 == req.Id || c.career_id_2 == req.Id);
            if (inUse)
                return Json(new { success = false, message = "This career goal is already in use by students (roadmaps, comparisons or skill-gap reports) and can't be deleted." });

            var title = career.title;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var templateIds = await _db.RoadmapTemplates.Where(t => t.career_id == req.Id).Select(t => t.template_id).ToListAsync();
                _db.RoadmapTemplateSteps.RemoveRange(_db.RoadmapTemplateSteps.Where(s => templateIds.Contains(s.template_id)));
                _db.RoadmapTemplates.RemoveRange(_db.RoadmapTemplates.Where(t => t.career_id == req.Id));
                _db.CareerSkills.RemoveRange(_db.CareerSkills.Where(cs => cs.career_id == req.Id));

                _db.Careers.Remove(career);
                await _db.SaveChangesAsync();

                _db.AdminLogs.Add(new AdminLog
                {
                    admin_id = GetUserId(),
                    action = "Deleted Career",
                    target_table = "Careers",
                    target_id = req.Id,
                    details = $"Deleted career goal '{title}' and its skill/roadmap-template mappings",
                    created_at = DateTime.Now
                });
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = "Could not delete this career — please try again." });
            }
        }

        // ══════════════════════════════════════════════
        // COMPANIES MANAGEMENT PAGE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Companies(string? search, string? verified, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.CompanyProfiles.Include(c => c.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.company_name.ToLower().Contains(s)
                                       || c.User.email.ToLower().Contains(s));
            }
            if (verified == "Verified") query = query.Where(c => c.is_verified);
            else if (verified == "Unverified") query = query.Where(c => !c.is_verified);

            int totalCount = await query.CountAsync();

            var companies = await query
                .OrderByDescending(c => c.User.created_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new AdminCompanyRowVM
                {
                    CompanyId = c.company_id,
                    UserId = c.user_id,
                    CompanyName = c.company_name,
                    Email = c.User.email,
                    Industry = c.industry,
                    Location = c.location,
                    LogoUrl = c.logo_url,
                    IsVerified = c.is_verified,
                    IsActive = c.User.is_active,
                    CreatedAt = c.User.created_at,
                    JobsCount = _db.JobPostings.Count(j => j.company_id == c.company_id)
                })
                .ToListAsync();

            var vm = new AdminCompaniesListVM
            {
                Companies = companies,
                Search = search ?? "",
                VerifiedFilter = verified ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // Click on a company opens its full detail page
        public async Task<IActionResult> CompanyDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var company = await _db.CompanyProfiles.Include(c => c.User)
                .FirstOrDefaultAsync(c => c.company_id == id);
            if (company == null) return NotFound();

            var jobs = await _db.JobPostings
                .Where(j => j.company_id == id)
                .OrderByDescending(j => j.posted_date)
                .Select(j => new AdminCompanyJobRowVM
                {
                    JobId = j.job_id,
                    Title = j.title,
                    Status = j.status,
                    Applications = j.applications_count,
                    Views = j.views_count,
                    PostedDate = j.posted_date
                })
                .ToListAsync();

            var vm = new AdminCompanyDetailVM
            {
                CompanyId = company.company_id,
                UserId = company.user_id,
                CompanyName = company.company_name,
                Email = company.User.email,
                Industry = company.industry,
                Location = company.location,
                Description = company.description,
                Website = company.website,
                LogoUrl = company.logo_url,
                EmployeeCount = company.employee_count,
                FoundedYear = company.founded_year,
                IsVerified = company.is_verified,
                IsActive = company.User.is_active,
                CreatedAt = company.User.created_at,
                UniqueKey = company.User.unique_key,
                Jobs = jobs
            };

            return View(vm);
        }

        public class CompanyIdRequest { public int CompanyId { get; set; } }

        // Toggle a company's verified badge (Verify / Unverify)
        [HttpPost]
        public async Task<IActionResult> ToggleVerifyCompany([FromBody] CompanyIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var company = await _db.CompanyProfiles.FindAsync(req.CompanyId);
            if (company == null) return Json(new { success = false, message = "Company not found" });

            company.is_verified = !company.is_verified;
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = company.is_verified ? "Verified Company" : "Unverified Company",
                target_table = "CompanyProfiles",
                target_id = req.CompanyId,
                details = $"Company '{company.company_name}' verification → {company.is_verified}",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, isVerified = company.is_verified });
        }

        // ══════════════════════════════════════════════
        // JOBS MANAGEMENT PAGE
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Jobs(string? search, string? status, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.JobPostings.Include(j => j.Company).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(j => j.title.ToLower().Contains(s)
                                       || j.Company.company_name.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(j => j.status == status);
            }

            int totalCount = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.posted_date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new AdminJobRowVM
                {
                    JobId = j.job_id,
                    Title = j.title,
                    Company = j.Company.company_name,
                    JobType = j.job_type ?? "",
                    Location = j.location ?? "",
                    Status = j.status,
                    Views = j.views_count,
                    Applications = j.applications_count,
                    PostedDate = j.posted_date,
                    Deadline = j.deadline
                })
                .ToListAsync();

            var vm = new AdminJobsListVM
            {
                Jobs = jobs,
                Search = search ?? "",
                StatusFilter = status ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        public class JobIdRequest { public int JobId { get; set; } }

        // Toggle a job posting between Active / Closed
        [HttpPost]
        public async Task<IActionResult> ToggleJobStatus([FromBody] JobIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var job = await _db.JobPostings.FindAsync(req.JobId);
            if (job == null) return Json(new { success = false, message = "Job not found" });

            job.status = job.status == "Active" ? "Closed" : "Active";
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = job.status == "Active" ? "Reactivated Job" : "Closed Job",
                target_table = "JobPostings",
                target_id = req.JobId,
                details = $"Job '{job.title}' status → {job.status}",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, status = job.status });
        }

        // Permanently delete a job posting (and its applications/matches via DB cascade)
        [HttpPost]
        public async Task<IActionResult> DeleteJob([FromBody] JobIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var job = await _db.JobPostings.FindAsync(req.JobId);
            if (job == null) return Json(new { success = false, message = "Job not found" });

            var title = job.title;

            try
            {
                _db.JobPostings.Remove(job);
                await _db.SaveChangesAsync();

                _db.AdminLogs.Add(new AdminLog
                {
                    admin_id = GetUserId(),
                    action = "Deleted Job",
                    target_table = "JobPostings",
                    target_id = req.JobId,
                    details = $"Deleted job posting '{title}'",
                    created_at = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not delete this job posting — please try again." });
            }
        }

        // ══════════════════════════════════════════════
        // CONTACT MESSAGES (from the public "Send us a message" form)
        // ══════════════════════════════════════════════
        public async Task<IActionResult> Messages(string? search, string? status, int page = 1)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            const int pageSize = 15;
            var query = _db.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    m.full_name.ToLower().Contains(s) ||
                    m.email.ToLower().Contains(s) ||
                    m.message.ToLower().Contains(s));
            }
            if (status == "Unread")
            {
                query = query.Where(m => !m.is_read);
            }
            else if (status == "Read")
            {
                query = query.Where(m => m.is_read);
            }

            int totalCount = await query.CountAsync();
            int unreadCount = await _db.ContactMessages.CountAsync(m => !m.is_read);

            var messages = await query
                .OrderByDescending(m => m.submitted_at)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new AdminMessageRowVM
                {
                    ContactMessageId = m.contact_message_id,
                    FullName = m.full_name,
                    Email = m.email,
                    InquiryType = m.inquiry_type ?? "Other",
                    Message = m.message,
                    SubmittedAt = m.submitted_at,
                    IsRead = m.is_read
                })
                .ToListAsync();

            var vm = new AdminMessagesListVM
            {
                Messages = messages,
                Search = search ?? "",
                StatusFilter = status ?? "All",
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };

            return View(vm);
        }

        public class ContactMessageIdRequest { public int Id { get; set; } }

        // Mark a message as read (called when the admin opens/expands it)
        [HttpPost]
        public async Task<IActionResult> MarkMessageRead([FromBody] ContactMessageIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var msg = await _db.ContactMessages.FindAsync(req.Id);
            if (msg == null) return Json(new { success = false, message = "Message not found" });

            if (!msg.is_read)
            {
                msg.is_read = true;
                msg.read_by_admin_id = GetUserId();
                msg.read_at = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // Mark a message back to unread
        [HttpPost]
        public async Task<IActionResult> MarkMessageUnread([FromBody] ContactMessageIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var msg = await _db.ContactMessages.FindAsync(req.Id);
            if (msg == null) return Json(new { success = false, message = "Message not found" });

            msg.is_read = false;
            msg.read_by_admin_id = null;
            msg.read_at = null;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Permanently delete a contact message
        [HttpPost]
        public async Task<IActionResult> DeleteMessage([FromBody] ContactMessageIdRequest req)
        {
            if (!IsAdmin()) return Unauthorized();
            if (req == null) return Json(new { success = false, message = "Invalid request" });

            var msg = await _db.ContactMessages.FindAsync(req.Id);
            if (msg == null) return Json(new { success = false, message = "Message not found" });

            _db.ContactMessages.Remove(msg);
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                admin_id = GetUserId(),
                action = "Deleted Contact Message",
                target_table = "ContactMessages",
                target_id = req.Id,
                details = $"Deleted message from {msg.full_name} ({msg.email})",
                created_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("Role") == "Admin";

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}