namespace AspiraHub.ViewModels.Job
{
    public class JobBrowseVM
    {
        public List<JobCardVM> Jobs { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? FilterJobType { get; set; }
    }

    public class JobCardVM
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string JobType { get; set; } = "";
        public string? Salary { get; set; }
        public string? Experience { get; set; }
        public DateTime? Deadline { get; set; }
        public int MatchScore { get; set; }
        public bool IsApplied { get; set; }
        public bool IsSaved { get; set; }
    }

    public class MyApplicationVM
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedAt { get; set; }
    }

    public class ApplyJobVM
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? CoverLetter { get; set; }
    }
}