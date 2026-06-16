using Microsoft.AspNetCore.Components;
using RunningGoalTracker.Interfaces;
using RunningGoalTracker.Models;
using RunningGoalTracker.Models.enums;
using RunningGoalTracker.Services;

namespace RunningGoalTracker.Components.Pages
{
    public partial class GoalTracker
    {
        private const string GoalTrackerSettingsKey = "goalTrackerSettings";

        [Inject] private IStravaService StravaService { get; set; } = default!;
        [Inject] private GoalProgressService GoalService { get; set; } = default!;
        [Inject] private LocalStorageService StorageService { get; set; } = default!;
        [Inject] private StravaAuthService StravaAuthService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private bool hasLoadedSettings;
        private bool shouldSyncStravaAfterRender;
        private bool hasSyncedStravaThisVisit;
        private bool isLoadingStrava;

        private DateTime? lastStravaSync;

        private AppTheme appTheme = AppTheme.Light;
        private DistanceUnit distanceUnit = DistanceUnit.Miles;
        private MonthlyAllocationMode allocationMode = MonthlyAllocationMode.RedistributeRemaining;

        private RunningGoal goal = new()
        {
            GoalMiles = 1000,
            CurrentMiles = 0,
            ManualMiles = 0
        };

        private List<MonthlyRunTotal> monthlyRunTotals = new();

        private List<MonthlyAllocationSetting> monthlyAllocationSettings =
        [
            new() { MonthNumber = 1, Percent = 5 },
            new() { MonthNumber = 2, Percent = 6 },
            new() { MonthNumber = 3, Percent = 9 },
            new() { MonthNumber = 4, Percent = 10 },
            new() { MonthNumber = 5, Percent = 11 },
            new() { MonthNumber = 6, Percent = 8 },
            new() { MonthNumber = 7, Percent = 7 },
            new() { MonthNumber = 8, Percent = 7 },
            new() { MonthNumber = 9, Percent = 10 },
            new() { MonthNumber = 10, Percent = 11 },
            new() { MonthNumber = 11, Percent = 9 },
            new() { MonthNumber = 12, Percent = 7 }
        ];

        private bool IsDarkMode =>
            appTheme == AppTheme.Dark;

        private string ThemeClass =>
            IsDarkMode ? "theme-dark" : "theme-light";

        private string UnitLabel =>
            GoalService.GetUnitLabel(distanceUnit);

        private string DistanceUnitDisplay =>
            distanceUnit == DistanceUnit.Kilometers
                ? "Kilometers"
                : "Miles";

        private string AllocationModeText =>
            allocationMode switch
            {
                MonthlyAllocationMode.FillInOrder => "Fill In Order",
                MonthlyAllocationMode.RedistributeRemaining => "Redistribute Remaining",
                _ => string.Empty
            };

        private string StravaSyncText =>
            lastStravaSync == null
                ? "Strava not synced"
                : $"Synced {lastStravaSync.Value:g}";

        private List<MonthlyGoalAllocation> MonthlyAllocations =>
            GoalService.GetMonthlyAllocations(
                goal,
                allocationMode,
                monthlyAllocationSettings);

        private List<GoalScenario> GoalScenarios =>
        [
            GoalService.GetGoalScenario(goal, 1000),
            GoalService.GetGoalScenario(goal, 1200),
            GoalService.GetGoalScenario(goal, 1500)
        ];

        private decimal GoalCompletionPercent =>
            GoalService.GetRunningProgressPercent(goal);

        private decimal ProgressDifference =>
            GoalCompletionPercent - GoalService.GetYearProgressPercent();

        private decimal AheadBehindMiles =>
            GoalService.GetAheadBehindMiles(goal);

        private string PaceStatusText =>
            AheadBehindMiles switch
            {
                > 0 => $"Ahead by {FormatDistance(AheadBehindMiles)}",
                < 0 => $"Behind by {FormatDistance(Math.Abs(AheadBehindMiles))}",
                _ => "Exactly on pace"
            };

        private string PaceStatusClass =>
            AheadBehindMiles switch
            {
                > 0 => "status-ahead",
                < 0 => "status-behind",
                _ => "status-on-pace"
            };

        private decimal ProjectedAnnualMiles =>
            GoalService.GetProjectedAnnualMiles(goal);

        private DateTime? ProjectedFinishDate =>
            GoalService.GetProjectedFinishDate(goal);

        private string ProjectedFinishDateText =>
            ProjectedFinishDate == null
                ? "Not enough data"
                : ProjectedFinishDate.Value.ToString("MMM d");

        private decimal ProjectedGoalDifference =>
            ProjectedAnnualMiles - goal.GoalMiles;

        private string ProjectedGoalDifferenceText =>
            ProjectedGoalDifference >= 0
                ? $"+{FormatDistance(ProjectedGoalDifference)} above goal"
                : $"{FormatDistance(Math.Abs(ProjectedGoalDifference))} below goal";

        private decimal ProjectedGoalPercent =>
            goal.GoalMiles <= 0
                ? 0
                : ProjectedAnnualMiles / goal.GoalMiles * 100;

        private string ProjectedGoalWidth =>
            $"{Math.Min(ProjectedGoalPercent, 100):0.0}%";

        private string ProjectionClass =>
            ProjectedAnnualMiles >= goal.GoalMiles
                ? "status-ahead"
                : "status-behind";

        private List<string> AchievementBadges
        {
            get
            {
                var badges = new List<string>();

                if (AheadBehindMiles > 0)
                {
                    badges.Add("🏅 Ahead of Pace");
                }

                if (ProjectedAnnualMiles >= goal.GoalMiles)
                {
                    badges.Add("🚀 Projected to Beat Goal");
                }

                if (GoalCompletionPercent >= 50)
                {
                    badges.Add("🔥 Halfway There");
                }

                if (GoalService.GetMilesPerDayNeeded(goal) <= 3)
                {
                    badges.Add("😎 Comfortable Daily Pace");
                }

                return badges;
            }
        }

        private decimal DisplayedGoalDistance
        {
            get => ConvertMilesForDisplay(goal.GoalMiles);
            set => goal.GoalMiles = ConvertDisplayValueToMiles(value);
        }

        private decimal DisplayedCurrentDistance
        {
            get => ConvertMilesForDisplay(goal.CurrentMiles);
            set => goal.CurrentMiles = ConvertDisplayValueToMiles(value);
        }

        private decimal DisplayedManualDistance
        {
            get => ConvertMilesForDisplay(goal.ManualMiles);
            set => goal.ManualMiles = ConvertDisplayValueToMiles(value);
        }

        protected override Task OnParametersSetAsync()
        {
            if (!hasSyncedStravaThisVisit)
            {
                shouldSyncStravaAfterRender = true;
            }

            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !hasLoadedSettings)
            {
                hasLoadedSettings = true;

                await LoadSettings();

                StateHasChanged();
            }

            if (shouldSyncStravaAfterRender && !hasSyncedStravaThisVisit)
            {
                shouldSyncStravaAfterRender = false;
                hasSyncedStravaThisVisit = true;

                await LoadStravaMiles();

                StateHasChanged();
            }
        }

        private decimal ConvertMilesForDisplay(decimal miles)
        {
            return Math.Round(
                GoalService.ConvertFromMiles(miles, distanceUnit),
                2);
        }

        private decimal ConvertDisplayValueToMiles(decimal value)
        {
            return GoalService.ConvertToMiles(value, distanceUnit);
        }

        private string FormatDistance(decimal miles)
        {
            var convertedDistance =
                GoalService.ConvertFromMiles(miles, distanceUnit);

            return $"{convertedDistance:0.0} {UnitLabel}";
        }

        private async Task SetTheme(bool useDarkMode)
        {
            appTheme = useDarkMode
                ? AppTheme.Dark
                : AppTheme.Light;

            await SaveSettings();
        }

        private async Task SetDistanceUnit(bool useKilometers)
        {
            distanceUnit = useKilometers
                ? DistanceUnit.Kilometers
                : DistanceUnit.Miles;

            await SaveSettings();
        }

        private async Task SetDisplayedGoalDistance(decimal value)
        {
            DisplayedGoalDistance = value;
            await SaveSettings();
        }

        private async Task SetDisplayedCurrentDistance(decimal value)
        {
            DisplayedCurrentDistance = value;
            await SaveSettings();
        }

        private async Task SetDisplayedManualDistance(decimal value)
        {
            DisplayedManualDistance = value;
            await SaveSettings();
        }

        private async Task SetAllocationMode(bool isRedistributeMode)
        {
            allocationMode = isRedistributeMode
                ? MonthlyAllocationMode.RedistributeRemaining
                : MonthlyAllocationMode.FillInOrder;

            await SaveSettings();
        }

        private async Task SetMonthlyAllocationSettings(
            List<MonthlyAllocationSetting> settings)
        {
            monthlyAllocationSettings = settings;
            await SaveSettings();
        }

        private async Task SaveSettings()
        {
            var settings = new GoalTrackerSettings
            {
                Goal = goal,
                DistanceUnit = distanceUnit,
                AllocationMode = allocationMode,
                AppTheme = appTheme,
                MonthlyAllocations = monthlyAllocationSettings
            };

            await StorageService.SaveAsync(
                GoalTrackerSettingsKey,
                settings);
        }

        private async Task LoadSettings()
        {
            var settings =
                await StorageService.LoadAsync<GoalTrackerSettings>(
                    GoalTrackerSettingsKey);

            if (settings == null)
            {
                return;
            }

            goal = settings.Goal;
            distanceUnit = settings.DistanceUnit;
            allocationMode = settings.AllocationMode;
            appTheme = settings.AppTheme;

            if (settings.MonthlyAllocations.Any())
            {
                monthlyAllocationSettings = settings.MonthlyAllocations;
            }
        }

        private async Task ResetValues()
        {
            goal = new RunningGoal
            {
                GoalMiles = 1000,
                CurrentMiles = 0,
                ManualMiles = 0
            };

            distanceUnit = DistanceUnit.Miles;
            allocationMode = MonthlyAllocationMode.RedistributeRemaining;
            appTheme = AppTheme.Light;
            monthlyRunTotals = new();

            await SaveSettings();
        }

        private async Task LoadStravaMiles()
        {
            isLoadingStrava = true;

            try
            {
                var syncedMiles =
                    await StravaService.GetYearToDateMilesAsync();

                var syncedMonthlyRuns =
                    await StravaService.GetMonthlyRunTotalsAsync();

                if (syncedMiles > 0)
                {
                    goal.CurrentMiles = syncedMiles;
                    monthlyRunTotals = syncedMonthlyRuns;
                    lastStravaSync = DateTime.Now;

                    await SaveSettings();
                }
            }
            finally
            {
                isLoadingStrava = false;
            }
        }

        private void ConnectToStrava()
        {
            Navigation.NavigateTo(
                StravaAuthService.GetAuthorizationUrl(),
                forceLoad: true);
        }
        private async Task ToggleTheme()
        {
            appTheme = IsDarkMode
                ? AppTheme.Light
                : AppTheme.Dark;

            await SaveSettings();
        }
    }
}