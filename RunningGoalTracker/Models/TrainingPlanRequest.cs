public class TrainingPlanRequest
{
    public string Location { get; set; } = "";
    public decimal AnnualGoalMiles { get; set; }
    public decimal CurrentMiles { get; set; }
    public DateTime CurrentDate { get; set; } =DateTime.UtcNow;
}