using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using System.Linq;

namespace CryptoTrading.Worker;

public class StrategyRunnerWorker : BackgroundService
{
    private readonly ILogger<StrategyRunnerWorker> _logger;
    private readonly StrategyRegistry _registry;
    private readonly PaperTradeExecutor _executor;
    private readonly IFeatureStore _store;
    private readonly IConfiguration _configuration;

    public StrategyRunnerWorker(
        ILogger<StrategyRunnerWorker> logger,
        StrategyRegistry registry,
        PaperTradeExecutor executor,
        IFeatureStore store,
        IConfiguration configuration)
    {
        _logger = logger;
        _registry = registry;
        _executor = executor;
        _store = store;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[S1] Strategy Runner iniciado.");

        var symbols = _configuration.GetSection("MarketData:Symbols").Get<string[]>() ?? new[] { "BTCUSDT", "ETHUSDT" };
        var intervals = _configuration.GetSection("MarketData:Intervals").Get<string[]>() ?? new[] { "1m" };
        var pollingSeconds = 10; // Frequência do loop de decisão

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in symbols)
            {
                foreach (var interval in intervals)
                {
                    try
                    {
                        await RunStrategiesAsync(symbol, interval);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[S1] Erro ao rodar estratégia para {symbol}/{interval}", symbol, interval);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSeconds), stoppingToken);
        }
    }

    private async Task RunStrategiesAsync(string symbol, string interval)
    {
        var recentPoints = await _store.GetMarketDataPointsAsync(symbol, interval, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow);
        var currentPoint = recentPoints.OrderByDescending(x => x.Candle.OpenTime).FirstOrDefault();

        if (currentPoint == null) return;

        foreach (var strategy in _registry.GetAll())
        {
            var audit = await _executor.ProcessSignalAsync(strategy, currentPoint);
            if (audit.Decision != "REJECTED" && !audit.Reason.Contains("Hold"))
            {
                _logger.LogInformation("[S1] Strategy {strategy} executou com sucesso: {decision} - {reason}", strategy.Name, audit.Decision, audit.Reason);
            }
        }
    }
}
