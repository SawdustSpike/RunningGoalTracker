namespace RunningGoalTracker.Models
{
    public class MonthlyGoalAllocation
    {
        public int MonthNumber { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public decimal AnnualPercent { get; set; }

        public decimal OriginalMiles { get; set; }

        public decimal RemainingMiles { get; set; }
    }
}