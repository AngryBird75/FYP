namespace AspiraHub.DTOs
{
    // Matches AspiraHub.ViewModels.Learning.UniversityRecsVM's University
    // item (Name, Location, Ranking (int?), Type, FeeStructure, Reason),
    // plus website/latitude/longitude pulled straight from the
    // Universities table so the client can offer a "Visit website" link
    // and a "View on map" action for each recommendation.
    public class UniversityDto
    {
        public string name { get; set; } = "";
        public string location { get; set; } = "";
        public int? ranking { get; set; }
        public string type { get; set; } = "";
        public string feeStructure { get; set; } = "";
        public string reason { get; set; } = "";
        public string website { get; set; } = "";
        public double? latitude { get; set; }
        public double? longitude { get; set; }
    }
}
