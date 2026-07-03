namespace AspiraHub.ViewModels.Career
{
    public class CareerCompareVM
    {
        public List<CareerOption> AvailableCareers { get; set; } = new();
        public CareerCompareItemVM? CareerA { get; set; }
        public CareerCompareItemVM? CareerB { get; set; }
    }

    public class CareerOption
    {
        public int CareerId { get; set; }
        public string Title { get; set; } = "";
    }

    public class CareerCompareItemVM
    {
        public int CareerId { get; set; }
        public string Title { get; set; } = "";
        public int? AvgSalary { get; set; }
        public string DemandLevel { get; set; } = "";
        public string Scope { get; set; } = "";
        public int MatchPercent { get; set; }
        public List<string> TopSkills { get; set; } = new();
    }
}