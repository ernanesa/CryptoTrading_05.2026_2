using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Polly;
using Polly.Retry;

namespace CryptoTrading.Infrastructure.Adapters;

/// <summary>
/// Adaptador de mercado conectando-se diretamente à API da Binance Spot usando Binance.Net e Polly v8 para resiliência de rede.
/// </summary>
public class BinanceMarketDataAdapter : IMarketDataAdapter
{
    private readonly ResiliencePipeline _resiliencePipeline;

    public BinanceMarketDataAdapter()
    {
        // Criação de uma pipeline de resiliência Polly v8 moderna com Retry Exponencial e Jitter
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 4,
                Delay = TimeSpan.FromSeconds(1)
            })
            .Build();
    }

    public async Task<IEnumerable<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
    {
        var klineInterval = MapInterval(interval);

        return await _resiliencePipeline.ExecuteAsync(async token =>
        {
            using var restClient = new BinanceRestClient();
            var limit = 1000; // Máximo suportado em uma chamada única
            
            var result = await restClient.SpotApi.ExchangeData.GetKlinesAsync(
                symbol: symbol.ToUpper(),
                interval: klineInterval,
                startTime: startTime,
                endTime: endTime,
                limit: limit,
                ct: token
            );

            if (!result.Success)
            {
                throw new Exception($"Erro ao buscar candles na Binance: {result.Error?.Message}");
            }

            return result.Data.Select(k => new Candle
            {
                Symbol = symbol.ToUpper(),
                Interval = interval.ToLower(),
                OpenTime = k.OpenTime,
                Open = k.OpenPrice,
                High = k.HighPrice,
                Low = k.LowPrice,
                Close = k.ClosePrice,
                Volume = k.Volume,
                CloseTime = k.CloseTime
            });
        });
    }

    private static KlineInterval MapInterval(string interval)
    {
        return interval.ToLower().Trim() switch
        {
            "1m" => KlineInterval.OneMinute,
            "3m" => KlineInterval.ThreeMinutes,
            "5m" => KlineInterval.FiveMinutes,
            "15m" => KlineInterval.FifteenMinutes,
            "30m" => KlineInterval.ThirtyMinutes,
            "1h" => KlineInterval.OneHour,
            "2h" => KlineInterval.TwoHour,
            "4h" => KlineInterval.FourHour,
            "6h" => KlineInterval.SixHour,
            "8h" => KlineInterval.EightHour,
            "12h" => KlineInterval.TwelveHour,
            "1d" => KlineInterval.OneDay,
            "3d" => KlineInterval.ThreeDay,
            "1w" => KlineInterval.OneWeek,
            "1M" => KlineInterval.OneMonth,
            _ => throw new ArgumentException($"Intervalo '{interval}' não é suportado pelo adaptador da Binance Spot.")
        };
    }
}
