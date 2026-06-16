namespace RunningGoalTracker.Models
{
    public class GoalScenario
    {
        public decimal GoalMiles { get; set; }
        public decimal CompletionPercent { get; set; }
        public decimal MilesRemaining { get; set; }
        public decimal MilesPerDayNeeded { get; set; }
        public decimal ProjectedAnnualMiles { get; set; }
        public DateTime? ProjectedFinishDate { get; set; }
    }
}