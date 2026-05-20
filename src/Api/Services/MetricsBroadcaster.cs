using CryptoTrading.Api.Hubs;
using CryptoTrading.Contracts.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CryptoTrading.Api.Services;

public class MetricsBroadcaster : BackgroundService
{
    private readonly IHubContext<MetricsHub> _hubContext;
    private readonly IMetricsService _metrics;
    private readonly ILogger<MetricsBroadcaster> _logger;

    public MetricsBroadcaster(
        IHubContext<MetricsHub> hubContext,
        IMetricsService metrics,
        ILogger<MetricsBroadcaster> logger)
    {
        _hubContext = hubContext;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando transmissor de métricas em tempo real...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = _metrics.GetSnapshot();
                await _hubContext.Clients.All.SendAsync("ReceiveMetricsSnapshot", snapshot, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao transmitir métricas.");
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}
