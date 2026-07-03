namespace AspiraHub.Models
{
   
    public static class RoadmapStatus
    {
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";  
        public const string Completed = "Completed";

        public static readonly string[] All = { Pending, InProgress, Completed };
    }
}