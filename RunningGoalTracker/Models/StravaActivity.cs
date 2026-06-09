using System.Text.Json.Serialization;

namespace RunningGoalTracker.Models
{
    public class StravaActivity
    {
        [JsonPropertyName("distance")]
        public double DistanceMeters { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }
}