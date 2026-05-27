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

    [Fact]
    public void ReportExporter_ToMarkdown_IncludesAdvancedMetricsAndRegimeBreakdown()
    {
        var report = new BacktestReport
        {
            StrategyName = "Test Strategy",
            Symbol = "BTCUSDT",
            Interval = "1h",
            StartTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            InitialCapital = 10000m,
            FinalCapital = 10150m,
            TotalPnL = 150m,
            TotalPnLPercent = 1.5m,
            ExposureTimePercent = 62.5m,
            AvgHoldingTimeHours = 3.25,
            MaxConsecutiveLosses = 2,
            FeeImpactPercent = 0.18m,
            SlippageImpactPercent = 0.04m,
            RegimeBreakdown = new Dictionary<string, RegimePerformance>
            {
                ["Trending"] = new()
                {
                    Regime = "Trending",
                    Trades = 3,
                    WinRate = 0.6667m,
                    PnL = 150m,
                    AvgReturn = 0.5m
                }
            }
        };

        var markdown = ReportExporter.ToMarkdown(report);

        Assert.Contains("## Advanced Metrics", markdown);
        Assert.Contains("- **Exposure Time:** 62.50%", markdown);
        Assert.Contains("- **Average Holding Time:** 3.25 hours", markdown);
        Assert.Contains("- **Max Consecutive Losses:** 2", markdown);
        Assert.Contains("- **Fee Impact:** 0.18%", markdown);
        Assert.Contains("- **Slippage Impact:** 0.04%", markdown);
        Assert.Contains("## Regime Performance", markdown);
        Assert.Contains("| Trending | 3 | 66.67% | 150.00 | 0.50% |", markdown);
    }

    [Fact]
    public void PerformanceAnalyzer_PopulatesAdvancedMetricsDeterministically()
    {
        var report = new BacktestReport
        {
            StrategyName = "Deterministic Strategy",
            Symbol = "BTCUSDT",
            Interval = "1h",
            StartTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            InitialCapital = 10000m,
            FinalCapital = 10025m,
            Trades =
            [
                new Position
                {
                    Symbol = "BTCUSDT",
                    Type = PositionType.Long,
                    EntryTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExitTime = new DateTime(2026, 5, 1, 2, 0, 0, DateTimeKind.Utc),
                    RealizedPnL = 100m,
                    FeesPaid = 10m,
                    Regime = "Trending"
                },
                new Position
                {
                    Symbol = "BTCUSDT",
                    Type = PositionType.Long,
                    EntryTime = new DateTime(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc),
                    ExitTime = new DateTime(2026, 5, 1, 4, 0, 0, DateTimeKind.Utc),
                    RealizedPnL = -50m,
                    FeesPaid = 5m,
                    Regime = "Sideways"
                },
                new Position
                {
                    Symbol = "BTCUSDT",
                    Type = PositionType.Long,
                    EntryTime = new DateTime(2026, 5, 1, 5, 0, 0, DateTimeKind.Utc),
                    ExitTime = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                    RealizedPnL = -25m,
                    FeesPaid = 5m,
                    Regime = "Trending"
                }
            ]
        };

        new PerformanceAnalyzer().PopulateMetrics(report);

        Assert.Equal(3, report.TotalTrades);
        Assert.Equal(1, report.WinningTrades);
        Assert.Equal(2, report.LosingTrades);
        Assert.Equal(25m, report.TotalPnL);
        Assert.Equal(0.25m, report.TotalPnLPercent);
        Assert.Equal(20m, report.TotalFees);
        Assert.Equal(60m, report.ExposureTimePercent);
        Assert.Equal(2d, report.AvgHoldingTimeHours);
        Assert.Equal(2, report.MaxConsecutiveLosses);
        Assert.Equal(0.2m, report.FeeImpactPercent);
        Assert.Equal(0.05m, report.SlippageImpactPercent);
        Assert.Equal(75m, report.MaxDrawdown);
        Assert.Equal(1.3333m, Math.Round(report.ProfitFactor, 4));
        Assert.Equal(8.3333m, Math.Round(report.Expectancy, 4));

        var trending = report.RegimeBreakdown["Trending"];
        Assert.Equal(2, trending.Trades);
        Assert.Equal(0.5m, trending.WinRate);
        Assert.Equal(75m, trending.PnL);
        Assert.Equal(0.375m, trending.AvgReturn);

        var sideways = report.RegimeBreakdown["Sideways"];
        Assert.Equal(1, sideways.Trades);
        Assert.Equal(0m, sideways.WinRate);
        Assert.Equal(-50m, sideways.PnL);
        Assert.Equal(-0.5m, sideways.AvgReturn);
    }
}
