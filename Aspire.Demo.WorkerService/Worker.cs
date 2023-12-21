namespace Aspire.Demo.WorkerService;

using Aspire.Demo.Architecture.Messaging;
using Aspire.Demo.Architecture.Messaging.Models;

public class Worker : BackgroundService
{
    private readonly IMessageReceiver _busControl;
    private readonly ILogger<Worker> _logger;

    public Worker(IMessageReceiver busControl, ILogger<Worker> logger)
    {
        _busControl = busControl;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _busControl.ReceiveAsync<WeatherForecastRequest>(Queue.WeatherForecast, async x =>
        {         
            Task.Run(() => { Receive(x); }, stoppingToken);
        });
    }

    private void Receive(WeatherForecastRequest weatherForecast)
    {        
        _logger.LogInformation($"Location: {weatherForecast.Location}, date: {weatherForecast.Date}, {weatherForecast.TemperatureC}°C, {weatherForecast.TemperatureF}°F, Summary: {weatherForecast.Summary}");        
    }
}
