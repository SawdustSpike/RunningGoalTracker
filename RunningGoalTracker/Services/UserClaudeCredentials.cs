namespace RunningGoalTracker.Services
{
    public class UserClaudeCredentials
    {
        public string? ApiKey { get; private set; }

        public bool HasApiKey =>
            !string.IsNullOrWhiteSpace(ApiKey);

        public void SetApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException(
                    "Anthropic API key is required.");

            ApiKey = apiKey.Trim();
        }

        public void Clear()
        {
            ApiKey = null;
        }
    }
}