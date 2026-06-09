namespace RunningGoalTracker.Interfaces
{
    public interface IStravaService
    {
        Task<decimal> GetYearToDateMilesAsync();
    }
}