using RunningGoalTracker.Components;
using RunningGoalTracker.Interfaces;
using RunningGoalTracker.Models;
using RunningGoalTracker.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<GoalProgressService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<IStravaService, StravaService>();
builder.Services.Configure<StravaSettings>(
    builder.Configuration.GetSection("Strava"));
builder.Services.AddScoped<StravaAuthService>();
builder.Services.AddHttpClient<StravaApiService>();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
