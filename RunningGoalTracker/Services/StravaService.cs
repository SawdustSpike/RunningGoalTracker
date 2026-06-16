using RunningGoalTracker.Interfaces;
using RunningGoalTracker.Models;

namespace RunningGoalTracker.Services
{
    public class StravaService : IStravaService
    {
        private readonly StravaApiService _stravaApiService;
        private readonly LocalStorageService _storageService;

        public StravaService(
            StravaApiService stravaApiService,
            LocalStorageService storageService)
        {
            _stravaApiService = stravaApiService;
            _storageService = storageService;
        }
        public async Task<List<MonthlyRunTotal>> GetMonthlyRunTotalsAsync()
        {
            var token = await _storageService
                .LoadAsync<StravaTokenResponse>("stravaToken");

            if (token == null)
                return GetEmptyMonthlyRunTotals();

            var currentUnixTime =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (token.ExpiresAt <= currentUnixTime)
            {
                var refreshedToken =
                    await _stravaApiService.RefreshAccessTokenAsync(
                        token.RefreshToken);

                if (refreshedToken == null)
                    return GetEmptyMonthlyRunTotals();

                token = refreshedToken;

                await _storageService.SaveAsync(
                    "stravaToken",
                    token);
            }

            var activities =
                await _stravaApiService.GetActivitiesAsync(token.AccessToken);

            if (activities == null)
                return GetEmptyMonthlyRunTotals();

            var currentYear = DateTime.Today.Year;

            var runMilesByMonth = activities
                .Where(a => a.Type == "Run")
                .Where(a => a.StartDate.Year == currentYear)
                .GroupBy(a => a.StartDate.Month)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(a => (decimal)a.DistanceMeters / 1609.34m));

            return Enumerable.Range(1, 12)
                .Select(monthNumber => new MonthlyRunTotal
                {
                    MonthNumber = monthNumber,
                    MonthName = new DateTime(currentYear, monthNumber, 1).ToString("MMMM"),
                    Miles = runMilesByMonth.TryGetValue(monthNumber, out var miles)
                        ? miles
                        : 0
                })
                .ToList();
        }

        private List<MonthlyRunTotal> GetEmptyMonthlyRunTotals()
        {
            var currentYear = DateTime.Today.Year;

            return Enumerable.Range(1, 12)
                .Select(monthNumber => new MonthlyRunTotal
                {
                    MonthNumber = monthNumber,
                    MonthName = new DateTime(currentYear, monthNumber, 1).ToString("MMMM"),
                    Miles = 0
                })
                .ToList();
        }
        public async Task<decimal> GetYearToDateMilesAsync()
        {
            var token = await _storageService
    .LoadAsync<StravaTokenResponse>("stravaToken");

            if (token == null)
                return 0;

            var currentUnixTime =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (token.ExpiresAt <= currentUnixTime)
            {
                var refreshedToken =
                    await _stravaApiService.RefreshAccessTokenAsync(
                        token.RefreshToken);

                if (refreshedToken == null)
                    return 0;

                token = refreshedToken;

                await _storageService.SaveAsync(
                    "stravaToken",
                    token);
            }

            if (token == null)
                return 0;

            var activities = await _stravaApiService
                .GetActivitiesAsync(token.AccessToken);

            if (activities == null)
                return 0;

            return activities
                .Where(a => a.Type == "Run")
                .Where(a => a.StartDate.Year == DateTime.Now.Year)
                .Sum(a => (decimal)a.DistanceMeters / 1609.34m);
        }
    }
}