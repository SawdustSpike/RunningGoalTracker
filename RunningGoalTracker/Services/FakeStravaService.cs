using RunningGoalTracker.Interfaces;

namespace RunningGoalTracker.Services
{
    public class FakeStravaService : IStravaService
    {
        public Task<decimal> GetYearToDateMilesAsync()
        {
            return Task.FromResult(377m);
        }
    }
}