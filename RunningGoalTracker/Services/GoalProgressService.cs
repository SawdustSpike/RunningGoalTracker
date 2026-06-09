
using RunningGoalTracker.Models;
namespace RunningGoalTracker.Services
{
    public class GoalProgressService
    {
        public decimal GetYearProgressPercent()
        {
            var today = DateTime.Today;

            var start = new DateTime(today.Year, 1, 1);
            var end = new DateTime(today.Year, 12, 31);

            var totalDays = (end - start).Days + 1;
            var completedDays = (today - start).Days + 1;
            return (decimal)completedDays / totalDays * 100;
        }

        public decimal GetRunningProgressPercent(RunningGoal goal)
        {
            if (goal.GoalMiles <= 0) return 0;
            return GetTotalMiles(goal) / goal.GoalMiles * 100;
        }

        public decimal GetTotalMiles(RunningGoal goal)
        {
            return goal.CurrentMiles + goal.ManualMiles;
        }

        public decimal GetMilesRemaining(RunningGoal goal)
        {
            return Math.Max(0, goal.GoalMiles - GetTotalMiles(goal));
        }

        public decimal GetExpectedMilesToday(RunningGoal goal)
        {
            return goal.GoalMiles * GetYearProgressPercent() / 100;
        }
        public decimal GetAheadBehindMiles(RunningGoal goal)
        {
            return GetTotalMiles(goal)
                - GetExpectedMilesToday(goal);
        }

        public decimal GetMilesPerDayNeeded(RunningGoal goal)
        {
            var remainingMiles = GetMilesRemaining(goal);

            var daysRemaining =
                new DateTime(DateTime.Today.Year, 12, 31).DayOfYear
                - DateTime.Today.DayOfYear;

            if (daysRemaining <= 0) return 0;
            return remainingMiles / daysRemaining;

        }
        public List<MonthlyGoalAllocation> GetMonthlyAllocations(
    RunningGoal goal,
    MonthlyAllocationMode mode)
        {
            var monthlyPercents = new Dictionary<int, decimal>
    {
        { 1, 5 },
        { 2, 6 },
        { 3, 9 },
        { 4, 10 },
        { 5, 11 },
        { 6, 8 },
        { 7, 7 },
        { 8, 7 },
        { 9, 10 },
        { 10, 11 },
        { 11, 9 },
        { 12, 7 }
    };

            var today = DateTime.Today;
            var totalMilesRun = GetTotalMiles(goal);
            var remainingMiles = Math.Max(goal.GoalMiles - totalMilesRun, 0);

            var allocations = monthlyPercents
                .Select(x => new MonthlyGoalAllocation
                {
                    MonthNumber = x.Key,
                    MonthName = new DateTime(today.Year, x.Key, 1).ToString("MMMM"),
                    AnnualPercent = x.Value,
                    OriginalMiles = goal.GoalMiles * (x.Value / 100),
                    RemainingMiles = 0
                })
                .ToList();

            if (remainingMiles <= 0)
                return allocations;

            return mode switch
            {
                MonthlyAllocationMode.FillInOrder =>
                    GetFillInOrderAllocations(allocations, goal, totalMilesRun, today),

                MonthlyAllocationMode.RedistributeRemaining =>
                    GetRedistributedAllocations(allocations, remainingMiles, today),

                _ => allocations
            };
        }
        private List<MonthlyGoalAllocation> GetFillInOrderAllocations(
    List<MonthlyGoalAllocation> allocations,
    RunningGoal goal,
    decimal totalMilesRun,
    DateTime today)
        {
            var currentMonth = today.Month;

            // First, subtract completed miles from the original monthly goals in order.
            var milesToApply = totalMilesRun;

            foreach (var allocation in allocations)
            {
                var remainingForMonth =
                    Math.Max(allocation.OriginalMiles - milesToApply, 0);

                milesToApply =
                    Math.Max(milesToApply - allocation.OriginalMiles, 0);

                allocation.RemainingMiles = remainingForMonth;
            }

            // Then hide all past months.
            foreach (var allocation in allocations.Where(x => x.MonthNumber < currentMonth))
            {
                allocation.RemainingMiles = 0;
            }

            // If hiding past months caused the visible future values to no longer add up
            // to the actual remaining miles, redistribute from today forward.
            var visibleRemainingTotal = allocations
                .Where(x => x.MonthNumber >= currentMonth)
                .Sum(x => x.RemainingMiles);

            var actualRemainingMiles = Math.Max(goal.GoalMiles - totalMilesRun, 0);

            if (Math.Abs(visibleRemainingTotal - actualRemainingMiles) > 0.01m)
            {
                return GetRedistributedAllocations(
                    allocations,
                    actualRemainingMiles,
                    today);
            }

            return allocations;
        }
        private List<MonthlyGoalAllocation> GetRedistributedAllocations(
    List<MonthlyGoalAllocation> allocations,
    decimal remainingMiles,
    DateTime today)
        {
            var currentMonth = today.Month;

            var weightedMonths = allocations
                .Where(x => x.MonthNumber >= currentMonth)
                .Select(x =>
                {
                    var weight = x.AnnualPercent;

                    if (x.MonthNumber == currentMonth)
                    {
                        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

                        // Includes today.
                        var daysRemainingInMonth = daysInMonth - today.Day + 1;

                        var monthRemainingPercent =
                            (decimal)daysRemainingInMonth / daysInMonth;

                        weight *= monthRemainingPercent;
                    }

                    return new
                    {
                        Allocation = x,
                        Weight = weight
                    };
                })
                .Where(x => x.Weight > 0)
                .ToList();

            var totalWeight = weightedMonths.Sum(x => x.Weight);

            if (totalWeight <= 0)
                return allocations;

            foreach (var item in weightedMonths)
            {
                item.Allocation.RemainingMiles =
                    remainingMiles * (item.Weight / totalWeight);
            }

            foreach (var allocation in allocations.Where(x => x.MonthNumber < currentMonth))
            {
                allocation.RemainingMiles = 0;
            }

            return allocations;
        }
        public decimal ConvertFromMiles(
    decimal miles,
    DistanceUnit unit)
        {
            return unit == DistanceUnit.Kilometers
                ? miles * 1.60934m
                : miles;
        }

        public decimal ConvertToMiles(
            decimal value,
            DistanceUnit unit)
        {
            return unit == DistanceUnit.Kilometers
                ? value / 1.60934m
                : value;
        }

        public string GetUnitLabel(
            DistanceUnit unit)
        {
            return unit == DistanceUnit.Kilometers
                ? "km"
                : "mi";
        }
    }
}
