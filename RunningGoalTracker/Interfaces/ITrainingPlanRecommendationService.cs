using RunningGoalTracker.Models;

public interface ITrainingPlanRecommendationService
{
    Task<TrainingPlanRecommendation> GeneratePlanAsync(
        TrainingPlanRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}