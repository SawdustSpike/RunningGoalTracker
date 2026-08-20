using Microsoft.Extensions.Options;
using RunningGoalTracker.Interfaces;
using RunningGoalTracker.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace RunningGoalTracker.Services
{
    public class ClaudeTrainingPlanRecommendationService
        : ITrainingPlanRecommendationService
    {
        private readonly ILogger<ClaudeTrainingPlanRecommendationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly AnthropicSettings _settings;
        private readonly UserClaudeCredentials _credentials;

        public ClaudeTrainingPlanRecommendationService(
            HttpClient httpClient,
            IOptions<AnthropicSettings> options,
            ILogger<ClaudeTrainingPlanRecommendationService> logger,
            UserClaudeCredentials credentials)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<TrainingPlanRecommendation> GeneratePlanAsync(
            TrainingPlanRequest request)
        {
            ValidateRequest(request);

            var remainingMiles =
                Math.Max(request.AnnualGoalMiles - request.CurrentMiles, 0);

            _logger.LogInformation(
                "Generating training plan for location {Location}, " +
                "remaining {RemainingMiles} miles",
                request.Location,
                remainingMiles);

            var prompt = BuildPrompt(request, remainingMiles);

            var apiRequest = new
            {
                model = _settings.Model,
                max_tokens = 1500,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                output_config = new
                {
                    format = new
                    {
                        type = "json_schema",
                        schema = GetJsonSchema()
                    }
                }
            };

            try
            {
                var response = await SendWithRetryAsync(apiRequest);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Claude API returned {StatusCode}: {ResponseBody}",
                        (int)response.StatusCode,
                        responseBody);

                    throw new InvalidOperationException(
                        $"Claude API returned {(int)response.StatusCode}: {responseBody}");
                }

                var recommendation =
                    ParseRecommendation(responseBody);

                ValidateRecommendation(
                    recommendation,
                    request,
                    remainingMiles);

                _logger.LogInformation(
                    "Training plan generated successfully for {Location}",
                    request.Location);

                return recommendation;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Plan generation failed for {Location}", request.Location);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during plan generation for {Location}", request.Location);
                throw;
            }
        }

        private static string BuildPrompt(
      TrainingPlanRequest request,
      decimal remainingMiles)
        {
            const string exampleJson = """
        {
          "regionDescription": "Urban area with moderate winters",
          "reasoningSummary": "Increasing mileage gradually through spring and summer",
          "remainingMiles": 500.5,
          "allocations": [
            {"monthNumber": 1, "monthName": "January", "percentOfRemaining": 0, "targetMiles": 0},
            {"monthNumber": 2, "monthName": "February", "percentOfRemaining": 0, "targetMiles": 0},
            {"monthNumber": 3, "monthName": "March", "percentOfRemaining": 5, "targetMiles": 25.0},
            {"monthNumber": 4, "monthName": "April", "percentOfRemaining": 10, "targetMiles": 50.0},
            {"monthNumber": 5, "monthName": "May", "percentOfRemaining": 12, "targetMiles": 60.0},
            {"monthNumber": 6, "monthName": "June", "percentOfRemaining": 12, "targetMiles": 60.0},
            {"monthNumber": 7, "monthName": "July", "percentOfRemaining": 11, "targetMiles": 55.0},
            {"monthNumber": 8, "monthName": "August", "percentOfRemaining": 11, "targetMiles": 55.0},
            {"monthNumber": 9, "monthName": "September", "percentOfRemaining": 12, "targetMiles": 60.0},
            {"monthNumber": 10, "monthName": "October", "percentOfRemaining": 12, "targetMiles": 60.0},
            {"monthNumber": 11, "monthName": "November", "percentOfRemaining": 7, "targetMiles": 35.0},
            {"monthNumber": 12, "monthName": "December", "percentOfRemaining": 0, "targetMiles": 0}
          ]
        }
        """;

            return $"""
        You are a running coach creating a personalized training plan.
        Consider these factors when allocating miles:
        - Seasonal weather variations affect outdoor running practicality
        - Runners need gradual increases to avoid injury
        - Current fitness level influences pacing

        User Information:
        - Location: {request.Location}
        - Current date: {request.CurrentDate:yyyy-MM-dd}
        - Annual goal: {request.AnnualGoalMiles:0.##} miles
        - Completed: {request.CurrentMiles:0.##} miles
        - Remaining: {remainingMiles:0.##} miles

        CRITICAL: You MUST return exactly 12 months (January through December).
        Past months must have targetMiles = 0 and percentOfRemaining = 0.

        Example of correct structure (with sample values):
        {exampleJson}

        Instructions:
        1. Recommend how the REMAINING mileage should be allocated across the current month and future months.
        2. Past months must not receive any additional mileage.
        3. Account for the number of days remaining in the current month.
        4. Account for regional seasonal running conditions for the supplied location, including typical temperature, precipitation, daylight, and seasonal practicality.
        5. Do not assume the runner has access to a treadmill.
        6. The targetMiles values must total exactly {remainingMiles:0.##} miles.
        7. percentOfRemaining values must total approximately 100%.
        8. Include ALL 12 MONTHS in the allocations array (January through December).
        9. Keep reasoningSummary concise and user-facing.
        10. Do not change the user's annual goal.

        Before responding, think through:
        1. Why this month's allocation makes sense given current conditions
        2. How seasonal weather will affect each future month
        3. How the allocation builds fitness safely
        """;
        }

        private static TrainingPlanRecommendation ParseRecommendation(
            string responseBody)
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var content =
                document.RootElement.GetProperty("content");

            var textBlock = content
                .EnumerateArray()
                .FirstOrDefault(x =>
                    x.TryGetProperty("type", out var type) &&
                    type.GetString() == "text");

            if (textBlock.ValueKind == JsonValueKind.Undefined)
            {
                throw new InvalidOperationException(
                    "Claude returned no text response.");
            }

            var json =
                textBlock.GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException(
                    "Claude returned an empty recommendation.");
            }

            var recommendation =
                JsonSerializer.Deserialize<TrainingPlanRecommendation>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return recommendation
                ?? throw new InvalidOperationException(
                    "Claude recommendation could not be parsed.");
        }

        private static void ValidateRequest(
            TrainingPlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                throw new ArgumentException(
                    "Location is required.");
            }

            if (request.AnnualGoalMiles <= 0)
            {
                throw new ArgumentException(
                    "Annual mileage goal must be greater than zero.");
            }

            if (request.CurrentMiles < 0)
            {
                throw new ArgumentException(
                    "Current mileage cannot be negative.");
            }
        }

        private static void ValidateRecommendation(
            TrainingPlanRecommendation recommendation,
            TrainingPlanRequest request,
            decimal remainingMiles)
        {
            if (recommendation.Allocations.Count != 12)
            {
                throw new InvalidOperationException(
                    "Claude must return exactly 12 monthly allocations.");
            }

            if (recommendation.Allocations
                .Select(x => x.MonthNumber)
                .Distinct()
                .Count() != 12)
            {
                throw new InvalidOperationException(
                    "Claude returned duplicate or missing months.");
            }

            if (recommendation.Allocations.Any(
                x => x.MonthNumber < 1 || x.MonthNumber > 12))
            {
                throw new InvalidOperationException(
                    "Claude returned an invalid month.");
            }

            if (recommendation.Allocations.Any(
                x => x.TargetMiles < 0 ||
                     x.PercentOfRemaining < 0))
            {
                throw new InvalidOperationException(
                    "Claude returned a negative allocation.");
            }

            var pastMonthAllocations =
                recommendation.Allocations.Where(
                    x => x.MonthNumber < request.CurrentDate.Month);

            if (pastMonthAllocations.Any(
                x => x.TargetMiles != 0))
            {
                throw new InvalidOperationException(
                    "Claude allocated mileage to a past month.");
            }

            var totalMiles =
                recommendation.Allocations.Sum(
                    x => x.TargetMiles);

            if (Math.Abs(totalMiles - remainingMiles) > 0.5m)
            {
                throw new InvalidOperationException(
                    $"Claude allocated {totalMiles:0.0} miles " +
                    $"but {remainingMiles:0.0} miles remain.");
            }

            var totalPercent =
                recommendation.Allocations.Sum(
                    x => x.PercentOfRemaining);

            if (remainingMiles > 0 &&
                Math.Abs(totalPercent - 100) > 0.5m)
            {
                throw new InvalidOperationException(
                    $"Claude allocation percentages total " +
                    $"{totalPercent:0.0}% instead of 100%.");
            }
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(
            object apiRequest)
        {
            const int maxRetries = 3;
            int delayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var message = new HttpRequestMessage(
                        HttpMethod.Post,
                        "https://api.anthropic.com/v1/messages");

                    if (!_credentials.HasApiKey)
                    {
                        throw new InvalidOperationException(
                            "Connect an Anthropic API key before generating a plan.");
                    }

                    message.Headers.Add(
                        "x-api-key",
                        _credentials.ApiKey);

                    message.Headers.Add(
                        "anthropic-version",
                        "2023-06-01"); // Update this as Anthropic releases new API versions

                    message.Content =
                        JsonContent.Create(apiRequest);

                    var response = await _httpClient.SendAsync(message);

                    // Don't retry on client errors (4xx)
                    var statusCode = (int)response.StatusCode;

                    // Don't retry normal client errors.
                    // 429 is intentionally excluded because it should be retried.
                    if (statusCode >= 400 &&
                        statusCode < 500 &&
                        statusCode != 429)
                    {
                        return response;
                    }

                    if (response.IsSuccessStatusCode || attempt == maxRetries)
                    {
                        return response;
                    }

                    if (statusCode == 429 &&
                        response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
                    {
                        delayMs = Math.Max(
                            delayMs,
                            (int)retryAfter.TotalMilliseconds);
                    }

                    _logger.LogWarning(
                        "Claude API returned {StatusCode} on attempt {Attempt}/{MaxRetries}. " +
                        "Retrying in {DelayMs}ms",
                        statusCode,
                        attempt,
                        maxRetries,
                        delayMs);

                    response.Dispose();

                    await Task.Delay(delayMs);

                    delayMs *= 2;

                    // Return on success or final attempt
                    if (response.IsSuccessStatusCode || attempt == maxRetries)
                    {
                        return response;
                    }

                    // Retry on server errors (5xx) or rate limits (429)
                    _logger.LogWarning(
                        "Claude API returned {StatusCode} on attempt {Attempt}/{MaxRetries}. " +
                        "Retrying in {DelayMs}ms",
                        (int)response.StatusCode,
                        attempt,
                        maxRetries,
                        delayMs);

                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(
                        ex,
                        "Network error on attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms",
                        attempt,
                        maxRetries,
                        delayMs);

                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
            }

            throw new InvalidOperationException(
                $"Claude API request failed after {maxRetries} retries.");
        }
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.anthropic.com/v1/models");

            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        private static object GetJsonSchema()
        {
            return new
            {
                type = "object",
                properties = new
                {
                    regionDescription = new { type = "string" },
                    reasoningSummary = new { type = "string" },
                    remainingMiles = new { type = "number" },
                    allocations = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                monthNumber = new { type = "integer" },
                                monthName = new { type = "string" },
                                percentOfRemaining = new { type = "number" },
                                targetMiles = new { type = "number" }
                            },
                            required = new[]
                            {
                                "monthNumber",
                                "monthName",
                                "percentOfRemaining",
                                "targetMiles"
                            },
                            additionalProperties = false
                        }
                    }
                },
                required = new[]
                {
                    "regionDescription",
                    "reasoningSummary",
                    "remainingMiles",
                    "allocations"
                },
                additionalProperties = false
            };
        }
    }
}