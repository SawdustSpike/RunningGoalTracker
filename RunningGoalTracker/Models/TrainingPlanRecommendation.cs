namespace RunningGoalTracker.Models
{
    public class TrainingPlanRecommendation
    {
        public string RegionDescription { get; set; } = string.Empty;

        public string ReasoningSummary { get; set; } = string.Empty;

        public decimal RemainingMiles { get; set; }

        public List<RecommendedMonthlyAllocation> Allocations { get; set; } = new();
    }
}