using Microsoft.Extensions.Options;
using RunningGoalTracker.Models;

namespace RunningGoalTracker.Services
{
    public class StravaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly StravaSettings _settings;

        public StravaApiService(
            HttpClient httpClient,
            IOptions<StravaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<StravaTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var values = new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code"
            };

            return await PostTokenRequestAsync(
                values,
                "token exchange");
        }
        public async Task<StravaTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
        {
            var values = new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            };

            return await PostTokenRequestAsync(
                values,
                "token refresh");
        }
        public async Task<List<StravaActivity>> GetActivitiesAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            var allActivities = new List<StravaActivity>();
            var page = 1;

            while (true)
            {
                var activities = await _httpClient
                    .GetFromJsonAsync<List<StravaActivity>>(
                        $"https://www.strava.com/api/v3/athlete/activities?page={page}&per_page=200");

                if (activities == null || activities.Count == 0)
                    break;

                allActivities.AddRange(activities);
                page++;
            }

            return allActivities;
        }
        private async Task<StravaTokenResponse?> PostTokenRequestAsync(
    Dictionary<string, string> values,
    string errorContext)
        {
            var response = await _httpClient.PostAsync(
                "https://www.strava.com/oauth/token",
                new FormUrlEncodedContent(values));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Strava {errorContext} failed:");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine(errorBody);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<StravaTokenResponse>();
        }
    }
}