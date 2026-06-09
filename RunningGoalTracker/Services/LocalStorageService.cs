using Microsoft.JSInterop;
using System.Text.Json;

namespace RunningGoalTracker.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SaveAsync<T>(
            string key,
            T value)
        {
            var json = JsonSerializer.Serialize(value);

            await _js.InvokeVoidAsync(
                "localStorage.setItem",
                key,
                json);
        }

        public async Task<T?> LoadAsync<T>(
            string key)
        {
            var json = await _js.InvokeAsync<string>(
                "localStorage.getItem",
                key);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
    }
}