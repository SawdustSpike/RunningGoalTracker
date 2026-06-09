namespace RunningGoalTracker.Models
{
    public class GoalTrackerSettings
    {
        public RunningGoal Goal { get; set; } = new();
        public DistanceUnit DistanceUnit { get; set; }
        public MonthlyAllocationMode AllocationMode { get; set; }
    }
}