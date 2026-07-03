using AspiraHub.Data;
using AspiraHub.Models;
using AspiraHub.ViewModels.Career;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Services
{
    public class CareerService : ICareerService
    {
        private readonly AppDbContext _db;

        // Maps text importance levels to numeric weights for scoring
        private static readonly Dictionary<string, int> ImportanceWeight = new()
        {
            { "Critical", 4 },
            { "High",     3 },
            { "Medium",   2 },
            { "Low",      1 }
        };

        public CareerService(AppDbContext db)
        {
            _db = db;
        }

        // ══════════════════════════════════════════════════════════
        // EXPLORE — list all careers with match % for the student
        // ══════════════════════════════════════════════════════════
        public async Task<CareerExploreVM> ExploreCareersAsync(
            int studentId, string? search, string? demandFilter)
        {
            var query = _db.Careers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.title.Contains(search));

            if (!string.IsNullOrWhiteSpace(demandFilter))
                query = query.Where(c => c.demand_level == demandFilter);

            var careers = await query.ToListAsync();

            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .Select(ss => ss.skill_id)
                .ToListAsync();

            // SavedItems uses user_id, not student_id — caller must pass the user_id
            // separately when checking saved state; for the explore list we look up
            // via the student's linked user_id
            var studentProfile = await _db.StudentProfiles.FindAsync(studentId);
            int userId = studentProfile?.user_id ?? 0;

            var savedIds = await _db.SavedItems
                .Where(s => s.user_id == userId && s.item_type == "Career")
                .Select(s => s.item_id)
                .ToListAsync();

            var cards = new List<CareerCardVM>();

            foreach (var c in careers)
            {
                int matchPct = await ComputeMatchPercentage(c.career_id, studentSkillIds);

                cards.Add(new CareerCardVM
                {
                    CareerId = c.career_id,
                    Title = c.title,
                    Description = c.description ?? "",
                    AvgSalary = c.average_salary,
                    Scope = c.scope ?? "",
                    DemandLevel = c.demand_level ?? "Medium",
                    JobMarketTrend = c.job_market_trend ?? "Stable",
                    IsSaved = savedIds.Contains(c.career_id),
                    MatchPercent = matchPct
                });
            }

            cards = cards.OrderByDescending(x => x.MatchPercent).ToList();

            return new CareerExploreVM
            {
                Careers = cards,
                SearchTerm = search,
                FilterDemand = demandFilter
            };
        }

        // ══════════════════════════════════════════════════════════
        // SKILL GAP ANALYSIS — compares student skills vs career
        // requirements, weighted by importance level
        // ══════════════════════════════════════════════════════════
        public async Task<SkillGapVM> AnalyzeSkillGapAsync(int studentId, int careerId)
        {
            var career = await _db.Careers.FindAsync(careerId);
            if (career == null) return new SkillGapVM();

            var requiredSkills = await _db.CareerSkills
                .Where(cs => cs.career_id == careerId)
                .Include(cs => cs.Skill)
                .ToListAsync();

            // sort by importance weight, highest first
            requiredSkills = requiredSkills
                .OrderByDescending(r => GetWeight(r.importance_level))
                .ToList();

            var studentSkills = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .ToListAsync();

            var studentSkillIds = studentSkills.Select(s => s.skill_id).ToHashSet();

            var have = new List<SkillStatusVM>();
            var missing = new List<SkillStatusVM>();

            // clear previous analysis records for this student+career
            var oldRecords = await _db.SkillGapAnalyses
                .Where(g => g.student_id == studentId && g.career_id == careerId)
                .ToListAsync();
            _db.SkillGapAnalyses.RemoveRange(oldRecords);

            foreach (var reqSkill in requiredSkills)
            {
                if (studentSkillIds.Contains(reqSkill.skill_id))
                {
                    var matched = studentSkills.First(s => s.skill_id == reqSkill.skill_id);

                    have.Add(new SkillStatusVM
                    {
                        SkillName = reqSkill.Skill.skill_name,
                        ImportanceLevel = reqSkill.importance_level ?? "Medium",
                        CurrentLevel = matched.proficiency_level
                    });

                    _db.SkillGapAnalyses.Add(new SkillGapAnalysis
                    {
                        student_id = studentId,
                        career_id = careerId,
                        skill_id = reqSkill.skill_id,
                        required_level = reqSkill.importance_level,
                        current_level = matched.proficiency_level,
                        gap_level = "None",
                        analyzed_at = DateTime.Now
                    });
                }
                else
                {
                    missing.Add(new SkillStatusVM
                    {
                        SkillName = reqSkill.Skill.skill_name,
                        ImportanceLevel = reqSkill.importance_level ?? "Medium"
                    });

                    _db.SkillGapAnalyses.Add(new SkillGapAnalysis
                    {
                        student_id = studentId,
                        career_id = careerId,
                        skill_id = reqSkill.skill_id,
                        required_level = reqSkill.importance_level,
                        current_level = "None",
                        gap_level = reqSkill.importance_level ?? "Medium",
                        analyzed_at = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();

            int matchPct = await ComputeMatchPercentage(careerId, studentSkillIds.ToList());

            // simple rule: suggest courses for the top 3 missing skills,
            // ranked by importance weight
            var suggestions = missing
                .OrderByDescending(m => GetWeight(m.ImportanceLevel))
                .Take(3)
                .Select(m => $"Take a course in {m.SkillName}")
                .ToList();

            return new SkillGapVM
            {
                CareerId = career.career_id,
                CareerTitle = career.title,
                MatchPercent = matchPct,
                HaveSkills = have,
                MissingSkills = missing,
                SuggestedCourses = suggestions
            };
        }

        // ══════════════════════════════════════════════════════════
        // COMPARE — list of careers for the dropdown
        // ══════════════════════════════════════════════════════════
        public async Task<CareerCompareVM> GetCompareOptionsAsync()
        {
            var careers = await _db.Careers
                .OrderBy(c => c.title)
                .Select(c => new CareerOption { CareerId = c.career_id, Title = c.title })
                .ToListAsync();

            return new CareerCompareVM { AvailableCareers = careers };
        }

        public async Task<CareerCompareVM> CompareCareersAsync(
            int studentId, int careerIdA, int careerIdB)
        {
            var options = await GetCompareOptionsAsync();

            var careerA = await BuildCompareItem(studentId, careerIdA);
            var careerB = await BuildCompareItem(studentId, careerIdB);

            _db.CareerComparisons.Add(new CareerComparison
            {
                student_id = studentId,
                career_id_1 = careerIdA,
                career_id_2 = careerIdB,
                compared_at = DateTime.Now
            });
            await _db.SaveChangesAsync();

            options.CareerA = careerA;
            options.CareerB = careerB;
            return options;
        }

        private async Task<CareerCompareItemVM> BuildCompareItem(int studentId, int careerId)
        {
            var career = await _db.Careers.FindAsync(careerId);
            if (career == null) return new CareerCompareItemVM();

            var skills = await _db.CareerSkills
                .Where(cs => cs.career_id == careerId)
                .Include(cs => cs.Skill)
                .ToListAsync();

            skills = skills
                .OrderByDescending(s => GetWeight(s.importance_level))
                .Take(5)
                .ToList();

            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .Select(ss => ss.skill_id)
                .ToListAsync();

            int matchPct = await ComputeMatchPercentage(careerId, studentSkillIds);

            return new CareerCompareItemVM
            {
                CareerId = career.career_id,
                Title = career.title,
                AvgSalary = career.average_salary,
                DemandLevel = career.demand_level ?? "Medium",
                Scope = career.scope ?? "",
                MatchPercent = matchPct,
                TopSkills = skills.Select(s => s.Skill.skill_name).ToList()
            };
        }

        // ══════════════════════════════════════════════════════════
        // SAVE / UNSAVE
        // ══════════════════════════════════════════════════════════
        public async Task<bool> SaveCareerAsync(int userId, int careerId)
        {
            bool exists = await _db.SavedItems.AnyAsync(s =>
                s.user_id == userId && s.item_type == "Career" && s.item_id == careerId);

            if (exists) return true;

            _db.SavedItems.Add(new SavedItem
            {
                user_id = userId,
                item_type = "Career",
                item_id = careerId,
                saved_at = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnsaveCareerAsync(int userId, int careerId)
        {
            var item = await _db.SavedItems.FirstOrDefaultAsync(s =>
                s.user_id == userId && s.item_type == "Career" && s.item_id == careerId);

            if (item == null) return false;

            _db.SavedItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<CareerCardVM>> GetSavedCareersAsync(int userId, int studentId)
        {
            var savedIds = await _db.SavedItems
                .Where(s => s.user_id == userId && s.item_type == "Career")
                .Select(s => s.item_id)
                .ToListAsync();

            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .Select(ss => ss.skill_id)
                .ToListAsync();

            var careers = await _db.Careers
                .Where(c => savedIds.Contains(c.career_id))
                .ToListAsync();

            var result = new List<CareerCardVM>();
            foreach (var c in careers)
            {
                int matchPct = await ComputeMatchPercentage(c.career_id, studentSkillIds);
                result.Add(new CareerCardVM
                {
                    CareerId = c.career_id,
                    Title = c.title,
                    Description = c.description ?? "",
                    AvgSalary = c.average_salary,
                    Scope = c.scope ?? "",
                    DemandLevel = c.demand_level ?? "Medium",
                    JobMarketTrend = c.job_market_trend ?? "Stable",
                    IsSaved = true,
                    MatchPercent = matchPct
                });
            }
            return result;
        }

        // ══════════════════════════════════════════════════════════
        // RECOMMENDATIONS — scored using skills, interests, and
        // demand level (rule-based, not a real AI model)
        // ══════════════════════════════════════════════════════════
        public async Task<List<CareerCardVM>> GetRecommendedCareersAsync(int studentId, int take = 5)
        {
            var student = await _db.StudentProfiles.FindAsync(studentId);
            if (student == null) return new List<CareerCardVM>();

            var studentSkillIds = await _db.StudentSkills
                .Where(ss => ss.student_id == studentId)
                .Select(ss => ss.skill_id)
                .ToListAsync();

            var allCareers = await _db.Careers.ToListAsync();
            var scored = new List<(Career career, int score)>();

            foreach (var c in allCareers)
            {
                int score = 0;

                int matchPct = await ComputeMatchPercentage(c.career_id, studentSkillIds);
                score += matchPct; // up to +100

                if (!string.IsNullOrWhiteSpace(student.interests))
                {
                    var interestWords = student.interests
                        .Split(',', StringSplitOptions.TrimEntries);

                    foreach (var word in interestWords)
                    {
                        if (word.Length < 3) continue;
                        if ((c.title + " " + c.description)
                            .Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 15;
                        }
                    }
                }

                if (c.demand_level == "High") score += 10;
                else if (c.demand_level == "Medium") score += 5;

                scored.Add((c, score));
            }

            var top = scored
                .OrderByDescending(x => x.score)
                .Take(take)
                .ToList();

            return top.Select(x => new CareerCardVM
            {
                CareerId = x.career.career_id,
                Title = x.career.title,
                Description = x.career.description ?? "",
                AvgSalary = x.career.average_salary,
                Scope = x.career.scope ?? "",
                DemandLevel = x.career.demand_level ?? "Medium",
                JobMarketTrend = x.career.job_market_trend ?? "Stable",
                MatchPercent = Math.Min(100, x.score)
            }).ToList();
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        private async Task<int> ComputeMatchPercentage(int careerId, List<int> studentSkillIds)
        {
            var requiredSkills = await _db.CareerSkills
                .Where(cs => cs.career_id == careerId)
                .ToListAsync();

            if (!requiredSkills.Any()) return 0;

            int totalWeight = requiredSkills.Sum(r => GetWeight(r.importance_level));
            int matchedWeight = requiredSkills
                .Where(r => studentSkillIds.Contains(r.skill_id))
                .Sum(r => GetWeight(r.importance_level));

            if (totalWeight == 0) return 0;

            return (int)Math.Round((double)matchedWeight / totalWeight * 100);
        }

        private static int GetWeight(string? importanceLevel)
        {
            if (importanceLevel != null && ImportanceWeight.TryGetValue(importanceLevel, out int w))
                return w;
            return 2; // default to Medium weight
        }
    }
}