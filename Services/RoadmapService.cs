using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.ViewModels.Roadmap;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly AppDbContext _db;
        private static readonly int[] AllowedDurations = { 3, 6, 9, 12, 18, 24 };

        public RoadmapService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CareerOptionForRoadmap>> GetCareerOptionsAsync()
        {
            return await _db.Careers
                .OrderBy(c => c.title)
                .Select(c => new CareerOptionForRoadmap
                {
                    CareerId = c.career_id,
                    Title = c.title
                })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════
        // GENERATE ROADMAP
        // ══════════════════════════════════════════════════════════
        public async Task<int> GenerateRoadmapAsync(int studentId, GenerateRoadmapVM vm)
        {
            if (studentId == 0) return 0;
            if (vm.CareerId == 0) return 0;

            // 🔒 sirf fixed values allow — koi bhi random duration/type reject
            if (!AllowedDurations.Contains(vm.DurationMonths)) return 0;
            if (vm.RoadmapType is not ("Career" or "Education" or "Mixed")) return 0;

            var student = await _db.StudentProfiles
                .FirstOrDefaultAsync(s => s.student_id == studentId);

            if (student == null) return 0;

            var career = await _db.Careers.FindAsync(vm.CareerId);
            if (career == null) return 0;

            // Ab check career + type dono ke combination pe hai — taake
            // student ek hi career ke Career/Education/Mixed teeno alag bana sake
            var existing = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.student_id == studentId
                                       && r.career_id == vm.CareerId
                                       && r.type == vm.RoadmapType
                                       && r.status == "Active");
            if (existing != null) return existing.roadmap_id;

            var newRoadmap = new Roadmap
            {
                student_id = studentId,
                career_id = vm.CareerId,
                title = $"{career.title} Roadmap ({vm.RoadmapType})",
                type = vm.RoadmapType,
                status = "Active",
                created_at = DateTime.Now,
                duration_months = vm.DurationMonths,
                target_end_date = DateTime.Now.AddMonths(vm.DurationMonths)
            };

            _db.Roadmaps.Add(newRoadmap);
            await _db.SaveChangesAsync();

            var stepsToCreate = await BuildPersonalizedSteps(student, career, vm.RoadmapType);

            int order = 1;
            foreach (var step in stepsToCreate)
            {
                _db.RoadmapSteps.Add(new RoadmapStep
                {
                    roadmap_id = newRoadmap.roadmap_id,
                    step_title = step.Title,
                    description = step.Description,
                    step_order = order
                });
                order++;
            }
            await _db.SaveChangesAsync();

            var createdSteps = await _db.RoadmapSteps
                .Where(s => s.roadmap_id == newRoadmap.roadmap_id)
                .ToListAsync();

            foreach (var step in createdSteps)
            {
                _db.RoadmapProgresses.Add(new RoadmapProgress
                {
                    step_id = step.step_id,
                    student_id = studentId,
                    status = RoadmapStatus.Pending
                });
            }
            await _db.SaveChangesAsync();

            return newRoadmap.roadmap_id;
        }

        // ══════════════════════════════════════════════════════════
        // STEP BUILDER
        // ══════════════════════════════════════════════════════════
        private async Task<List<RoadmapStepDraft>> BuildPersonalizedSteps(
            StudentProfile student, Career career, string roadmapType)
        {
            var steps = new List<RoadmapStepDraft>();

            bool wantsEducation = roadmapType is "Education" or "Mixed";
            bool wantsCareer = roadmapType is "Career" or "Mixed";

            if (wantsEducation)
            {
                var eduSteps = await BuildEducationTrack(student);
                steps.AddRange(eduSteps);
            }

            if (wantsCareer)
            {
                var careerSteps = await BuildCareerTrack(student, career);
                steps.AddRange(careerSteps);
            }

            if (!steps.Any())
            {
                steps.Add(new RoadmapStepDraft(
                    "Explore This Career",
                    $"Start by researching what a {career.title} does day-to-day, " +
                    "and revisit this roadmap once your profile has more details."));
            }

            return steps;
        }

        // ══════════════════════════════════════════════════════════
        // EDUCATION TRACK
        // ══════════════════════════════════════════════════════════
        private async Task<List<RoadmapStepDraft>> BuildEducationTrack(StudentProfile student)
        {
            var steps = new List<RoadmapStepDraft>();

            if (student.degree_program_id.HasValue)
            {
                int fromSemester = student.current_semester ?? 1;

                var milestones = await _db.SemesterMilestones
                    .Where(m => m.program_id == student.degree_program_id.Value
                             && m.semester_number >= fromSemester
                             && m.is_active)
                    .OrderBy(m => m.semester_number)
                    .ToListAsync();

                if (milestones.Any())
                {
                    foreach (var m in milestones)
                    {
                        steps.Add(new RoadmapStepDraft(
                            $"Semester {m.semester_number}: {m.title}",
                            m.description ?? ""));
                    }
                    return steps;
                }
            }

            string level = student.education_level ?? "your current level";
            steps.Add(new RoadmapStepDraft(
                "Complete Your Academic Requirements",
                $"Finish the remaining coursework for {level}. " +
                "Add your degree program and current semester in your profile " +
                "to get a detailed, semester-by-semester academic plan here."));

            return steps;
        }

        // ══════════════════════════════════════════════════════════
        // CAREER TRACK
        // ══════════════════════════════════════════════════════════
        private async Task<List<RoadmapStepDraft>> BuildCareerTrack(StudentProfile student, Career career)
        {
            var steps = new List<RoadmapStepDraft>();

            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == student.student_id)
                .Select(ss => ss.skill_id)
                .ToListAsync();

            var requiredSkills = await _db.CareerSkills
                .Where(cs => cs.career_id == career.career_id)
                .Include(cs => cs.Skill)
                .ToListAsync();

            if (!requiredSkills.Any())
            {
                steps.Add(new RoadmapStepDraft(
                    $"Research {career.title} Requirements",
                    "We don't have detailed skill requirements for this career yet. " +
                    "Talk to professionals in this field or check job postings to learn what's expected."));
                return steps;
            }

            var missingSkills = requiredSkills
                .Where(r => !studentSkillIds.Contains(r.skill_id))
                .OrderByDescending(r => ImportanceWeight(r.importance_level))
                .ToList();

            int haveCount = requiredSkills.Count - missingSkills.Count;

            if (!studentSkillIds.Any())
            {
                steps.Add(new RoadmapStepDraft(
                    "Add Your Skills to Your Profile",
                    "You haven't listed any skills yet. Add them in your profile so this roadmap " +
                    $"can tell you exactly which of the {requiredSkills.Count} skills {career.title} needs you're missing."));
            }
            else if (haveCount > 0)
            {
                var haveNames = requiredSkills
                    .Where(r => studentSkillIds.Contains(r.skill_id))
                    .Select(r => r.Skill.skill_name);

                steps.Add(new RoadmapStepDraft(
                    "Skills You Already Bring",
                    $"You already have {haveCount} of {requiredSkills.Count} skills " +
                    $"{career.title} requires: {string.Join(", ", haveNames)}. Focus your time on the gaps below."));
            }

            foreach (var skill in missingSkills)
            {
                string importance = skill.importance_level ?? "Medium";
                steps.Add(new RoadmapStepDraft(
                    $"Learn {skill.Skill.skill_name}",
                    $"This is a {importance.ToLower()}-importance skill for {career.title} that isn't on your profile yet."));
            }

            if (missingSkills.Any())
            {
                var topGaps = missingSkills.Take(2).Select(s => s.Skill.skill_name);
                steps.Add(new RoadmapStepDraft(
                    "Build a Portfolio Project",
                    $"Apply your {string.Join(" and ", topGaps)} skills in 1-2 real projects you can show employers."));
            }
            else if (studentSkillIds.Any())
            {
                steps.Add(new RoadmapStepDraft(
                    "You're Skill-Ready",
                    $"You already meet the listed skill requirements for {career.title}. Focus on portfolio polish and applying."));
            }

            steps.Add(new RoadmapStepDraft(
                "Find an Internship",
                $"Look for {career.title} internships — Gujranwala has active openings at local software houses."));

            steps.Add(new RoadmapStepDraft(
                "Start Applying for Jobs",
                $"Once your skill gaps are covered, begin applying for {career.title} positions."));

            return steps;
        }

        private static int ImportanceWeight(string? importanceLevel) => importanceLevel switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 2
        };

        // ══════════════════════════════════════════════════════════
        // MY ROADMAPS
        // ══════════════════════════════════════════════════════════
        public async Task<RoadmapListVM> GetMyRoadmapsAsync(int studentId)
        {
            var roadmaps = await _db.Roadmaps
                .Where(r => r.student_id == studentId)
                .OrderByDescending(r => r.created_at)
                .ToListAsync();

            var cards = new List<RoadmapCardVM>();

            foreach (var r in roadmaps)
            {
                var (total, completed) = await GetStepCounts(r.roadmap_id, studentId);

                cards.Add(new RoadmapCardVM
                {
                    RoadmapId = r.roadmap_id,
                    Title = r.title,
                    Type = r.type,
                    Status = r.status,
                    TotalSteps = total,
                    CompletedSteps = completed,
                    ProgressPercent = total > 0 ? (completed * 100 / total) : 0,
                    CreatedAt = r.created_at,
                    DurationMonths = r.duration_months,
                    TargetEndDate = r.target_end_date
                });
            }

            return new RoadmapListVM { Roadmaps = cards };
        }

        // ══════════════════════════════════════════════════════════
        // ROADMAP DETAIL — grouped phases + duration-based target dates
        // ══════════════════════════════════════════════════════════
        public async Task<RoadmapDetailVM?> GetRoadmapDetailAsync(int roadmapId, int studentId)
        {
            var roadmap = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.roadmap_id == roadmapId && r.student_id == studentId);

            if (roadmap == null) return null;

            var steps = await _db.RoadmapSteps
                .Where(s => s.roadmap_id == roadmapId)
                .OrderBy(s => s.step_order)
                .ToListAsync();

            var progressMap = await _db.RoadmapProgresses
                .Where(p => p.student_id == studentId &&
                            steps.Select(s => s.step_id).Contains(p.step_id))
                .ToDictionaryAsync(p => p.step_id, p => p.status);

            var stepVMs = steps.Select(s => new RoadmapStepVM
            {
                StepId = s.step_id,
                StepTitle = s.step_title,
                Description = s.description,
                StepOrder = s.step_order,
                ResourceUrl = s.resource_url,
                ProgressStatus = progressMap.GetValueOrDefault(s.step_id, RoadmapStatus.Pending)
            }).ToList();

            int total = stepVMs.Count;
            int completed = stepVMs.Count(s => s.ProgressStatus == RoadmapStatus.Completed);

            var grouped = stepVMs
                .GroupBy(s => RoadmapPhaseGrouper.GetPhaseName(s.StepTitle))
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StepOrder).ToList());

            var phases = new List<RoadmapPhaseVM>();
            bool previousComplete = true;
            int stepsSoFar = 0;

            foreach (var phaseName in RoadmapPhaseGrouper.PhaseOrder)
            {
                if (!grouped.TryGetValue(phaseName, out var stepsInPhase) || !stepsInPhase.Any())
                    continue;

                bool allDone = stepsInPhase.All(s => s.ProgressStatus == RoadmapStatus.Completed);
                string status = allDone ? "Completed" : (previousComplete ? "Active" : "Locked");

                stepsSoFar += stepsInPhase.Count;
                DateTime? phaseTarget = total > 0
                    ? roadmap.created_at.AddDays(roadmap.duration_months * 30.0 * stepsSoFar / total)
                    : null;

                phases.Add(new RoadmapPhaseVM
                {
                    PhaseName = phaseName,
                    PhaseStatus = status,
                    Description = RoadmapPhaseGrouper.GetDescription(phaseName),
                    Icon = RoadmapPhaseGrouper.GetIcon(phaseName),
                    TargetDate = phaseTarget,
                    Steps = stepsInPhase
                });

                if (!allDone) previousComplete = false;
            }

            return new RoadmapDetailVM
            {
                RoadmapId = roadmap.roadmap_id,
                Title = roadmap.title,
                Type = roadmap.type,
                Status = roadmap.status,
                ProgressPercent = total > 0 ? (completed * 100 / total) : 0,
                DurationMonths = roadmap.duration_months,
                TargetEndDate = roadmap.target_end_date,
                Steps = stepVMs,
                Phases = phases
            };
        }

        // ══════════════════════════════════════════════════════════
        // STEP STATUS UPDATE (locked/completed guards + valid-status guard)
        // ══════════════════════════════════════════════════════════
        public async Task<bool> UpdateStepStatusAsync(int stepId, int studentId, string newStatus)
        {
            if (!RoadmapStatus.All.Contains(newStatus)) return false;

            var step = await _db.RoadmapSteps.FindAsync(stepId);
            if (step == null) return false;

            var existingProgress = await _db.RoadmapProgresses
                .FirstOrDefaultAsync(p => p.step_id == stepId && p.student_id == studentId);

            if (existingProgress?.status == RoadmapStatus.Completed) return false;

            var allSteps = await _db.RoadmapSteps
                .Where(s => s.roadmap_id == step.roadmap_id)
                .ToListAsync();

            var progressMap = await _db.RoadmapProgresses
                .Where(p => p.student_id == studentId && allSteps.Select(s => s.step_id).Contains(p.step_id))
                .ToDictionaryAsync(p => p.step_id, p => p.status);

            int myPhaseIndex = RoadmapPhaseGrouper.PhaseOrder.IndexOf(RoadmapPhaseGrouper.GetPhaseName(step.step_title));

            foreach (var s in allSteps)
            {
                int sIndex = RoadmapPhaseGrouper.PhaseOrder.IndexOf(RoadmapPhaseGrouper.GetPhaseName(s.step_title));
                if (sIndex < myPhaseIndex)
                {
                    string prevStatus = progressMap.GetValueOrDefault(s.step_id, RoadmapStatus.Pending);
                    if (prevStatus != RoadmapStatus.Completed) return false;
                }
            }

            if (existingProgress == null)
            {
                existingProgress = new RoadmapProgress { step_id = stepId, student_id = studentId };
                _db.RoadmapProgresses.Add(existingProgress);
            }

            existingProgress.status = newStatus;
            existingProgress.completed_at = newStatus == RoadmapStatus.Completed ? DateTime.Now : null;

            await _db.SaveChangesAsync();

            var roadmap = await _db.Roadmaps.FindAsync(step.roadmap_id);
            if (roadmap != null)
            {
                roadmap.updated_at = DateTime.Now;
                var (total, completedCount) = await GetStepCounts(roadmap.roadmap_id, studentId);
                if (total > 0 && total == completedCount)
                    roadmap.status = "Completed";
                else if (roadmap.status == "Completed")
                    roadmap.status = "Active";

                await _db.SaveChangesAsync();
            }

            return true;
        }

        // ══════════════════════════════════════════════════════════
        // DELETE / PAUSE / RESUME / RENAME
        // ══════════════════════════════════════════════════════════
        public async Task<bool> DeleteRoadmapAsync(int roadmapId, int studentId)
        {
            var roadmap = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.roadmap_id == roadmapId && r.student_id == studentId);

            if (roadmap == null) return false;

            var steps = await _db.RoadmapSteps
                .Where(s => s.roadmap_id == roadmapId)
                .ToListAsync();

            var stepIds = steps.Select(s => s.step_id).ToList();

            var progressRecords = await _db.RoadmapProgresses
                .Where(p => stepIds.Contains(p.step_id))
                .ToListAsync();

            _db.RoadmapProgresses.RemoveRange(progressRecords);
            _db.RoadmapSteps.RemoveRange(steps);
            _db.Roadmaps.Remove(roadmap);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PauseRoadmapAsync(int roadmapId, int studentId)
        {
            var roadmap = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.roadmap_id == roadmapId && r.student_id == studentId);

            if (roadmap == null) return false;

            roadmap.status = "Paused";
            roadmap.updated_at = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResumeRoadmapAsync(int roadmapId, int studentId)
        {
            var roadmap = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.roadmap_id == roadmapId && r.student_id == studentId);

            if (roadmap == null) return false;

            roadmap.status = "Active";
            roadmap.updated_at = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateRoadmapTitleAsync(int roadmapId, int studentId, string newTitle)
        {
            var roadmap = await _db.Roadmaps
                .FirstOrDefaultAsync(r => r.roadmap_id == roadmapId && r.student_id == studentId);

            if (roadmap == null) return false;

            roadmap.title = newTitle;
            roadmap.updated_at = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        // ══════════════════════════════════════════════════════════
        // REPORT
        // ══════════════════════════════════════════════════════════
        public async Task<RoadmapReportVM> GenerateReportAsync(int studentId)
        {
            var roadmaps = await _db.Roadmaps
                .Where(r => r.student_id == studentId)
                .ToListAsync();

            var breakdown = new List<RoadmapCardVM>();
            int totalStepsAll = 0;
            int completedStepsAll = 0;

            foreach (var r in roadmaps)
            {
                var (total, completed) = await GetStepCounts(r.roadmap_id, studentId);

                totalStepsAll += total;
                completedStepsAll += completed;

                breakdown.Add(new RoadmapCardVM
                {
                    RoadmapId = r.roadmap_id,
                    Title = r.title,
                    Type = r.type,
                    Status = r.status,
                    TotalSteps = total,
                    CompletedSteps = completed,
                    ProgressPercent = total > 0 ? (completed * 100 / total) : 0,
                    CreatedAt = r.created_at,
                    DurationMonths = r.duration_months,
                    TargetEndDate = r.target_end_date
                });
            }

            return new RoadmapReportVM
            {
                TotalRoadmaps = roadmaps.Count,
                ActiveRoadmaps = roadmaps.Count(r => r.status == "Active"),
                CompletedRoadmaps = roadmaps.Count(r => r.status == "Completed"),
                PausedRoadmaps = roadmaps.Count(r => r.status == "Paused"),
                TotalStepsAcrossAll = totalStepsAll,
                CompletedStepsAcrossAll = completedStepsAll,
                OverallProgressPercent = totalStepsAll > 0
                    ? (completedStepsAll * 100 / totalStepsAll)
                    : 0,
                RoadmapBreakdown = breakdown
            };
        }

        // ══════════════════════════════════════════════════════════
        // STEP RESOURCES — "Details" button: kaha se yeh skill/course lein
        // ══════════════════════════════════════════════════════════
        public async Task<StepResourcesVM> GetStepResourcesAsync(int stepId, int studentId)
        {
            var step = await _db.RoadmapSteps.FindAsync(stepId);
            if (step == null) return new StepResourcesVM();

            var vm = new StepResourcesVM { StepTitle = step.step_title };

            if (step.step_title.StartsWith("Learn "))
            {
                string skillName = step.step_title.Substring("Learn ".Length).Trim();

                // Accurate DB link ab: Skill -> CourseSkills -> InstituteCourses
                // (pehle course_name.Contains(skillName) text-search se galat/irrelevant
                // results aa sakte the — ab sirf actual mapped courses hi aayenge)
                var skill = await _db.Skills
                    .FirstOrDefaultAsync(s => s.skill_name.ToLower() == skillName.ToLower());

                List<InstituteCourse> courses;
                if (skill != null)
                {
                    courses = await _db.CourseSkills
                        .Where(cs => cs.skill_id == skill.skill_id)
                        .Include(cs => cs.Course).ThenInclude(c => c!.Institute)
                        .Select(cs => cs.Course!)
                        .Where(c => c != null)
                        .Distinct()
                        .ToListAsync();
                }
                else
                {
                    courses = new List<InstituteCourse>();
                }

                vm.Courses = courses.Select(c => new CourseResourceVM
                {
                    CourseName = c.course_name,
                    InstituteName = c.Institute?.name ?? "Unknown Institute",
                    Duration = c.duration ?? "N/A",
                    Fee = c.fee ?? "N/A",
                    Website = c.Institute?.website,
                    Mode = string.IsNullOrWhiteSpace(c.Institute?.type) ? "Not specified" : c.Institute!.type!
                }).ToList();
            }
            else if (step.step_title.StartsWith("Semester") ||
                     step.step_title == "Complete Your Academic Requirements")
            {
                var student = await _db.StudentProfiles
                    .FirstOrDefaultAsync(s => s.student_id == studentId);

                var allUnis = await _db.Universities.ToListAsync();
                string? term = student?.field_of_study ?? student?.program;

                var matched = !string.IsNullOrWhiteSpace(term)
                    ? allUnis.Where(u => !string.IsNullOrWhiteSpace(u.programs) &&
                                          u.programs.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList()
                    : new List<University>();

                if (!matched.Any())
                    matched = allUnis.OrderBy(u => u.ranking ?? 99).Take(3).ToList();

                vm.Universities = matched.Select(u => new UniversityResourceVM
                {
                    Name = u.name,
                    Location = u.location ?? "N/A",
                    Ranking = u.ranking,
                    Website = u.website
                }).ToList();
            }

            return vm;
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPER
        // ══════════════════════════════════════════════════════════
        private async Task<(int total, int completed)> GetStepCounts(int roadmapId, int studentId)
        {
            var stepIds = await _db.RoadmapSteps
                .Where(s => s.roadmap_id == roadmapId)
                .Select(s => s.step_id)
                .ToListAsync();

            int total = stepIds.Count;

            int completed = await _db.RoadmapProgresses
                .CountAsync(p => stepIds.Contains(p.step_id) &&
                                 p.student_id == studentId &&
                                 p.status == RoadmapStatus.Completed);

            return (total, completed);
        }
    }

    internal record RoadmapStepDraft(string Title, string Description);
}