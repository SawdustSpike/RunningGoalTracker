using RunningGoalTracker.Models;

namespace RunningGoalTracker.Interfaces
{
    public interface IStravaService
    {
        Task<decimal> GetYearToDateMilesAsync();
        Task<List<MonthlyRunTotal>> GetMonthlyRunTotalsAsync();
    }
}