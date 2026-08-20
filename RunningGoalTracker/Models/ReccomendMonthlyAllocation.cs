namespace RunningGoalTracker.Models
{
    public class RecommendedMonthlyAllocation
    {
        public int MonthNumber { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public decimal PercentOfRemaining { get; set; }

        public decimal TargetMiles { get; set; }
    }
}