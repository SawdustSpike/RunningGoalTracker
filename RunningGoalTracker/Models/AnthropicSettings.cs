using System.ComponentModel.DataAnnotations;

namespace RunningGoalTracker.Models
{
    public class AnthropicSettings
    {
        public string? ApiKey { get; set; }
        [Required]
        public string Model { get; set; } = string.Empty;

     
    }
}