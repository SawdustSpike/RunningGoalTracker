using RunningGoalTracker.Models.enums;

namespace RunningGoalTracker.Models
{
    public class GoalTrackerSettings
    {
        public RunningGoal Goal { get; set; } = new();
        public DistanceUnit DistanceUnit { get; set; }
        public MonthlyAllocationMode AllocationMode { get; set; }
        public AppTheme AppTheme { get; set; } = AppTheme.Light;
        public List<MonthlyAllocationSetting> MonthlyAllocations { get; set; } = new();
    }
}