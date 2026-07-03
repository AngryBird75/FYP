namespace AspiraHub.ViewModels.Career
{
    public class CareerExploreVM
    {
        public List<CareerCardVM> Careers { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? FilterDemand { get; set; }
    }

    public class CareerCardVM
    {
        public int CareerId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int? AvgSalary { get; set; }
        public string Scope { get; set; } = "";
        public string DemandLevel { get; set; } = "";
        public string JobMarketTrend { get; set; } = "";
        public bool IsSaved { get; set; }
        public int MatchPercent { get; set; }
    }
}