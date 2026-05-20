using CryptoTrading.Application.Services;
using CryptoTrading.Application.Strategies;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.UnitTests;

public class BacktestEngineTests
{
    private readonly BacktestEngine _engine = new();
    private readonly IFeeModel _feeModel = new BinanceSpotFeeModel();
    private readonly ISlippageModel _slippageModel = new PercentageSlippageModel();

    private static List<MarketDataPoint> CreateMockData(int count = 100)
    {
        var points = new List<MarketDataPoint>();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Simular um preço que sobe e desce ciclicamente para gerar cruzamentos
        for (var i = 0; i < count; i++)
        {
            var radians = (i * 10.0 * Math.PI) / 180.0;
            var close = 50000m + (decimal)(Math.Sin(radians) * 5000.0);
            var open = close - 10m;
            var high = Math.Max(open, close) + 50m;
            var low = Math.Min(open, close) - 50m;
            var volume = 1000m;

            var candle = new Candle
            {
                Id = i + 1,
                Symbol = "BTCUSDT",
                Interval = "1m",
                OpenTime = baseTime.AddMinutes(i),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                TakerBuyVolume = volume * 0.5m,
                CloseTime = baseTime.AddMinutes(i).AddSeconds(59)
            };

            // Criar features correspondentes simuladas
            var feature = new CandleFeature
            {
                CandleId = candle.Id,
                Symbol = candle.Symbol,
                OpenTime = candle.OpenTime,
                // Cruzamento artificial da EMA
                Ema9 = i > 10 ? close * 0.995m : 0m,
                Ema21 = i > 10 ? close * 1.002m : 0m,
                // RSI oscilante
                Rsi14 = 30m + (decimal)(Math.Sin(radians) * 35.0) + 15m, // oscila entre 10 e 80
                // Bollinger Bands
                BbMiddle = close,
                BbLower = close - 200m,
                BbUpper = close + 200m,
                Atr14 = 100m,
                Adx = 25m,
                Returns = 0.01m,
                VolumeZScore = 0.5m,
                Spread = 100m,
                Imbalance = 0m,
                CalculatedAt = DateTime.UtcNow
            };

            points.Add(new MarketDataPoint { Candle = candle, Feature = feature });
        }

        return points;
    }

    [Fact]
    public void Run_EmaStrategy_ExecutesAndGeneratesReport()
    {
        var data = CreateMockData(150);
        var strategy = new EmaTrendFollowingStrategy();

        var report = _engine.Run(strategy, data, 10000m, _feeModel, _slippageModel);

        Assert.Equal(strategy.Name, report.StrategyName);
        Assert.Equal("BTCUSDT", report.Symbol);
        Assert.True(report.FinalCapital > 0m);
        Assert.NotNull(report.Trades);
    }

    [Fact]
    public void Run_RsiStrategy_ExecutesAndGeneratesReport()
    {
        var data = CreateMockData(150);
        var strategy = new RsiMeanReversionStrategy();

        var report = _engine.Run(strategy, data, 10000m, _feeModel, _slippageModel);

        Assert.Equal(strategy.Name, report.StrategyName);
        Assert.True(report.FinalCapital > 0m);
        Assert.NotNull(report.Trades);
    }

    [Fact]
    public void Run_BollingerStrategy_ExecutesAndGeneratesReport()
    {
        var data = CreateMockData(150);
        var strategy = new BollingerMeanReversionStrategy();

        var report = _engine.Run(strategy, data, 10000m, _feeModel, _slippageModel);

        Assert.Equal(strategy.Name, report.StrategyName);
        Assert.True(report.FinalCapital > 0m);
        Assert.NotNull(report.Trades);
    }

    [Fact]
    public void Run_AtrStrategy_ExecutesAndGeneratesReport()
    {
        var data = CreateMockData(150);
        var strategy = new AtrBreakoutStrategy();

        var report = _engine.Run(strategy, data, 10000m, _feeModel, _slippageModel);

        Assert.Equal(strategy.Name, report.StrategyName);
        Assert.True(report.FinalCapital > 0m);
        Assert.NotNull(report.Trades);
    }
}
