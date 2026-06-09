using Microsoft.Extensions.Options;
using RunningGoalTracker.Models;
using System.Web;

namespace RunningGoalTracker.Services
{
    public class StravaAuthService
    {
        private readonly StravaSettings _settings;

        public StravaAuthService(IOptions<StravaSettings> options)
        {
            _settings = options.Value;
        }

        public string GetAuthorizationUrl()
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            query["client_id"] = _settings.ClientId;
            query["redirect_uri"] = _settings.RedirectUri;
            query["response_type"] = "code";
            query["approval_prompt"] = "auto";
            query["scope"] = "read,activity:read_all";

            return $"https://www.strava.com/oauth/authorize?{query}";
        }
    }
}