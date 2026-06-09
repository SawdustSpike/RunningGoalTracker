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