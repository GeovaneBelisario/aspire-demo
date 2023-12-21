namespace Aspire.Demo.Web;

public class BackendApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync()
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast") ?? [];
    }

    public async Task EnqueueAsync(WeatherForecast weatherForecast)
    {
        await httpClient.PostAsJsonAsync("/enqueue", weatherForecast);
    }
}

public record WeatherForecast(string Location, DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}