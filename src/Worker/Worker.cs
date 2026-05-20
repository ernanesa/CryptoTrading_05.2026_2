using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Services;

namespace CryptoTrading.Worker;

/// <summary>
/// Worker de ingestão de dados de mercado: coleta candles da Binance, valida pelo DataQualityGate,
/// calcula indicadores técnicos e persiste tudo no PostgreSQL.
/// </summary>
public class Worker(
    ILogger<Worker> logger,
    IMarketDataAdapter marketDataAdapter,
    IFeatureStore featureStore,
    IConfiguration configuration) : BackgroundService
{
    private readonly DataQualityGate _qualityGate = new();
    private readonly IndicatorService _indicatorService = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[M1] Worker de ingestão de dados de mercado iniciado às {time}", DateTimeOffset.Now);

        // Inicializar schema do banco de dados no startup
        await featureStore.InitializeSchemaAsync();
        logger.LogInformation("[M1] Schema do PostgreSQL inicializado com sucesso.");

        var symbols = configuration.GetSection("MarketData:Symbols").Get<string[]>() ?? ["BTCUSDT", "ETHUSDT"];
        var intervals = configuration.GetSection("MarketData:Intervals").Get<string[]>() ?? ["1m"];
        var intervalSeconds = configuration.GetValue("MarketData:PollingIntervalSeconds", 60);

        logger.LogInformation("[M1] Monitorando {count} pares nos intervalos: {intervals}",
            symbols.Length, string.Join(", ", intervals));

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in symbols)
            {
                foreach (var interval in intervals)
                {
                    try
                    {
                        await IngestCandlesAsync(symbol, interval, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[M1] Erro na ingestão de {symbol}/{interval}", symbol, interval);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task IngestCandlesAsync(string symbol, string interval, CancellationToken ct)
    {
        // 1. Obter o último timestamp persistido para calcular o delta de dados faltantes
        var lastTime = await featureStore.GetLastCandleTimeAsync(symbol, interval);
        var startTime = lastTime?.AddMinutes(1) ?? DateTime.UtcNow.AddHours(-24);
        var endTime = DateTime.UtcNow;

        if (startTime >= endTime)
        {
            logger.LogDebug("[M1] {symbol}/{interval} já está atualizado.", symbol, interval);
            return;
        }

        // 2. Buscar candles brutos da Binance com resiliência Polly integrada
        logger.LogInformation("[M1] Buscando candles {symbol}/{interval} de {start:u} a {end:u}...",
            symbol, interval, startTime, endTime);

        var rawCandles = (await marketDataAdapter.GetHistoricalCandlesAsync(symbol, interval, startTime, endTime)).ToList();

        if (rawCandles.Count == 0)
        {
            logger.LogWarning("[M1] Nenhum candle retornado para {symbol}/{interval}.", symbol, interval);
            return;
        }

        // 3. Filtrar pelo DataQualityGate
        var validCandles = new List<Domain.Entities.Candle>();
        var rejectedCount = 0;

        foreach (var candle in rawCandles)
        {
            if (_qualityGate.Validate(candle, out var errors))
            {
                validCandles.Add(candle);
            }
            else
            {
                rejectedCount++;
                logger.LogWarning("[M1] Candle rejeitado ({symbol} {time}): {errors}",
                    symbol, candle.OpenTime, string.Join("; ", errors));
            }
        }

        logger.LogInformation("[M1] {symbol}/{interval}: {valid} candles válidos, {rejected} rejeitados pelo DataQualityGate.",
            symbol, interval, validCandles.Count, rejectedCount);

        if (validCandles.Count == 0)
        {
            return;
        }

        // 4. Persistir candles validados
        await featureStore.SaveCandlesAsync(validCandles);
        logger.LogInformation("[M1] {count} candles persistidos no PostgreSQL para {symbol}/{interval}.",
            validCandles.Count, symbol, interval);

        // 5. Calcular indicadores técnicos (features)
        var features = _indicatorService.CalculateFeatures(validCandles);

        if (features.Count > 0)
        {
            await featureStore.SaveFeaturesAsync(features);
            logger.LogInformation("[M1] {count} features calculadas e persistidas para {symbol}/{interval}.",
                features.Count, symbol, interval);
        }
    }
}
