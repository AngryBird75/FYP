using AspiraHub.Data;
using AspiraHub.ViewModels.Learning;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class LearningService : ILearningService
    {
        private readonly AppDbContext _db;

        public LearningService(AppDbContext db)
        {
            _db = db;
        }

        // ══════════════════════════════════════════════════════════
        // RECOMMENDED COURSES — rule-based: match missing skills
        // (from skill gap data) against InstituteCourses by name
        // ══════════════════════════════════════════════════════════
        public async Task<RecommendedCoursesVM> GetRecommendedCoursesAsync(int studentId)
        {
            // pull the most recent skill gap records for this student
            var gapSkills = await _db.SkillGapAnalyses
                .Where(g => g.student_id == studentId && g.current_level == "None")
                .Include(g => g.Skill)
                .Where(g => g.Skill != null)
                .Select(g => g.Skill!)
                .Distinct()
                .ToListAsync();

            var recommendations = new List<RecommendedCourseVM>();

            // Accurate DB link ab: Skill -> CourseSkills -> InstituteCourses
            // (pehle course_name.Contains(skillName) text-search se galat/irrelevant
            // matches aa sakte the — ab sirf actual mapped courses hi milte hain)
            foreach (var skill in gapSkills)
            {
                var matchingCourse = await _db.CourseSkills
                    .Where(cs => cs.skill_id == skill.skill_id)
                    .Include(cs => cs.Course).ThenInclude(c => c!.Institute)
                    .Select(cs => cs.Course)
                    .FirstOrDefaultAsync(c => c != null);

                if (matchingCourse != null)
                {
                    recommendations.Add(new RecommendedCourseVM
                    {
                        CourseId = matchingCourse.course_id,
                        CourseName = matchingCourse.course_name,
                        InstituteName = matchingCourse.Institute?.name ?? "",
                        Duration = matchingCourse.duration ?? "",
                        Fee = matchingCourse.fee ?? "",
                        Mode = string.IsNullOrWhiteSpace(matchingCourse.Institute?.type) ? "Not specified" : matchingCourse.Institute!.type!,
                        Reason = $"Helps close your skill gap in {skill.skill_name}"
                    });
                }
            }

            // if no skill-gap-based matches, fall back to the student's interests
            // (still uses accurate CourseSkills-backed course list, not raw text contains)
            if (!recommendations.Any())
            {
                var student = await _db.StudentProfiles.FindAsync(studentId);
                if (!string.IsNullOrWhiteSpace(student?.interests))
                {
                    var interestWords = student.interests.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var word in interestWords.Take(3))
                    {
                        var matchedSkill = await _db.Skills
                            .FirstOrDefaultAsync(s => s.skill_name.ToLower() == word.ToLower());

                        if (matchedSkill == null) continue;

                        var course = await _db.CourseSkills
                            .Where(cs => cs.skill_id == matchedSkill.skill_id)
                            .Include(cs => cs.Course).ThenInclude(c => c!.Institute)
                            .Select(cs => cs.Course)
                            .FirstOrDefaultAsync(c => c != null);

                        if (course != null)
                        {
                            recommendations.Add(new RecommendedCourseVM
                            {
                                CourseId = course.course_id,
                                CourseName = course.course_name,
                                InstituteName = course.Institute?.name ?? "",
                                Duration = course.duration ?? "",
                                Fee = course.fee ?? "",
                                Mode = string.IsNullOrWhiteSpace(course.Institute?.type) ? "Not specified" : course.Institute!.type!,
                                Reason = $"Matches your interest in {word}"
                            });
                        }
                    }
                }
            }

            return new RecommendedCoursesVM
            {
                Courses = recommendations.Take(6).ToList()
            };
        }

        // ══════════════════════════════════════════════════════════
        // UNIVERSITY RECOMMENDATIONS — rule-based: match the
        // student's career goal against university program lists
        // ══════════════════════════════════════════════════════════
        public async Task<UniversityRecsVM> GetUniversityRecommendationsAsync(int studentId)
        {
            var student = await _db.StudentProfiles.FindAsync(studentId);
            if (student == null) return new UniversityRecsVM();

            var allUniversities = await _db.Universities.ToListAsync();
            var matched = new List<UniversityRecVM>();

            string? searchTerm = !string.IsNullOrWhiteSpace(student.goal)
                ? student.goal
                : student.program;

            foreach (var uni in allUniversities)
            {
                bool matches = !string.IsNullOrWhiteSpace(searchTerm) &&
                               !string.IsNullOrWhiteSpace(uni.programs) &&
                               uni.programs.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

                if (matches)
                {
                    matched.Add(new UniversityRecVM
                    {
                        UniversityId = uni.university_id,
                        Name = uni.name,
                        Location = uni.location ?? "",
                        Ranking = uni.ranking,
                        Type = uni.type ?? "",
                        FeeStructure = uni.fee_structure ?? "",
                        Reason = $"Offers programs related to {searchTerm}"
                    });
                }
            }

            // fall back: if nothing matched, just show top-ranked universities
            if (!matched.Any())
            {
                matched = allUniversities
                    .OrderBy(u => u.ranking ?? 99)
                    .Take(5)
                    .Select(u => new UniversityRecVM
                    {
                        UniversityId = u.university_id,
                        Name = u.name,
                        Location = u.location ?? "",
                        Ranking = u.ranking,
                        Type = u.type ?? "",
                        FeeStructure = u.fee_structure ?? "",
                        Reason = "Top-ranked university in your area"
                    })
                    .ToList();
            }

            return new UniversityRecsVM
            {
                Universities = matched.OrderBy(u => u.Ranking ?? 99).Take(6).ToList()
            };
        }

        // ══════════════════════════════════════════════════════════
        // MY PROGRESS — combined courses + roadmap progress view
        // ══════════════════════════════════════════════════════════
        public async Task<MyLearningProgressVM> GetMyProgressAsync(int studentId)
        {
            int coursesInProgress = await _db.StudentCourses
                .CountAsync(sc => sc.student_id == studentId && sc.status == "Enrolled");

            int coursesCompleted = await _db.StudentCourses
                .CountAsync(sc => sc.student_id == studentId && sc.status == "Completed");

            var roadmaps = await _db.Roadmaps
                .Where(r => r.student_id == studentId)
                .ToListAsync();

            var roadmapIds = roadmaps.Select(r => r.roadmap_id).ToList();

            int totalSteps = await _db.RoadmapSteps
                .CountAsync(s => roadmapIds.Contains(s.roadmap_id));

            int doneSteps = await _db.RoadmapProgresses
                .CountAsync(p => p.student_id == studentId && p.status == "Completed");

            int overallPct = totalSteps > 0
                ? (int)Math.Round((double)doneSteps / totalSteps * 100)
                : 0;

            return new MyLearningProgressVM
            {
                CoursesInProgress = coursesInProgress,
                CoursesCompleted = coursesCompleted,
                TotalRoadmaps = roadmaps.Count,
                RoadmapStepsTotal = totalSteps,
                RoadmapStepsDone = doneSteps,
                OverallProgressPercent = overallPct
            };
        }
    }
}