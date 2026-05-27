using CryptoTrading.Application.Services;
using CryptoTrading.Application.Strategies;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.UnitTests;

public class WalkForwardEngineTests
{
    [Fact]
    public void WalkForwardEngine_Executes_Multiple_Windows()
    {
        var backtestEngine = new BacktestEngine();
        var wfEngine = new WalkForwardEngine(backtestEngine);
        var strategy = new EmaTrendFollowingStrategy();
        var data = CreateMockData(60); // 60 days
        
        var reports = wfEngine.RunWalkForward(
            strategy, 
            data, 
            trainWindowDays: 10, 
            testWindowDays: 5, 
            initialCapital: 10000m, 
            feeModel: new BinanceSpotFeeModel(), 
            slippageModel: new PercentageSlippageModel()
        );

        Assert.NotEmpty(reports);
        Assert.True(reports.Count > 1);
        Assert.All(reports, r => Assert.Contains("_WF_", r.StrategyName));
    }

    private static List<MarketDataPoint> CreateMockData(int days)
    {
        var points = new List<MarketDataPoint>();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        for (var i = 0; i < days * 24; i++) // hourly candles
        {
            var close = 50000m + (decimal)(Math.Sin(i * 0.1) * 1000);
            var candle = new Candle
            {
                Id = i + 1, Symbol = "BTCUSDT", Interval = "1h", OpenTime = baseTime.AddHours(i),
                Open = close, High = close + 10, Low = close - 10, Close = close, Volume = 100
            };
            var feature = new CandleFeature
            {
                CandleId = candle.Id, Symbol = candle.Symbol, OpenTime = candle.OpenTime,
                Ema9 = close * 0.99m, Ema21 = close * 1.01m, Rsi14 = 50m
            };
            points.Add(new MarketDataPoint { Candle = candle, Feature = feature });
        }
        return points;
    }
}
