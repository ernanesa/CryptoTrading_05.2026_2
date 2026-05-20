using System.Diagnostics;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

var filter = ReadOption(args, "--filter") ?? "*";
var iterations = int.TryParse(ReadOption(args, "--iterations"), out var parsedIterations)
    ? Math.Max(1, parsedIterations)
    : 20;

var runner = new LocalBenchmarkRunner(iterations);
var results = runner.Run(filter);

Console.WriteLine("CryptoTrading local benchmark harness");
Console.WriteLine($"Filter: {filter} | Iterations: {iterations}");
Console.WriteLine();
Console.WriteLine("| Benchmark | Status | Total ms | Avg ms | Ops/s | Notes |");
Console.WriteLine("|---|---:|---:|---:|---:|---|");

foreach (var result in results)
{
    Console.WriteLine($"| {result.Name} | {result.Status} | {result.TotalMilliseconds:F2} | {result.AverageMilliseconds:F4} | {result.OperationsPerSecond:F2} | {result.Notes} |");
}

return results.Any(r => r.Status == "Failed") ? 1 : 0;

static string? ReadOption(string[] args, string name)
{
    var index = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (index < 0 || index + 1 >= args.Length)
    {
        return null;
    }

    return args[index + 1];
}

public sealed record BenchmarkResult(
    string Name,
    string Status,
    double TotalMilliseconds,
    double AverageMilliseconds,
    double OperationsPerSecond,
    string Notes);

public sealed class LocalBenchmarkRunner
{
    private readonly int _iterations;

    public LocalBenchmarkRunner(int iterations)
    {
        _iterations = iterations;
    }

    public List<BenchmarkResult> Run(string filter)
    {
        var results = new List<BenchmarkResult>();

        if (Matches(filter, "IndicatorService.CalculateFeatures"))
        {
            results.Add(RunTimed("IndicatorService.CalculateFeatures", BenchmarkIndicatorService));
        }

        if (Matches(filter, "AdaptiveStrategyOrchestrator.Decide"))
        {
            results.Add(RunTimed("AdaptiveStrategyOrchestrator.Decide", BenchmarkAdaptiveOrchestrator));
        }

        if (Matches(filter, "FeatureStore.GetMarketDataPointsAsync"))
        {
            results.Add(new BenchmarkResult(
                "FeatureStore.GetMarketDataPointsAsync",
                "RegisteredOnly",
                0,
                0,
                0,
                "Requires PostgreSQL fixture; keep as opt-in integration benchmark."));
        }

        if (Matches(filter, "Api.NativeAot.Publish"))
        {
            results.Add(new BenchmarkResult(
                "Api.NativeAot.Publish",
                "RegisteredOnly",
                0,
                0,
                0,
                "Run dotnet publish with PublishAot=true as a separate compatibility gate."));
        }

        return results.Count == 0
            ? new List<BenchmarkResult>
            {
                new("No matching benchmark", "Skipped", 0, 0, 0, "Adjust --filter, for example --filter *Adaptive*.")
            }
            : results;
    }

    private BenchmarkResult RunTimed(string name, Action benchmark)
    {
        benchmark();

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < _iterations; i++)
        {
            benchmark();
        }

        stopwatch.Stop();
        var total = stopwatch.Elapsed.TotalMilliseconds;
        var average = total / _iterations;
        var ops = total > 0 ? _iterations / stopwatch.Elapsed.TotalSeconds : 0;

        return new BenchmarkResult(name, "Passed", total, average, ops, "In-process Stopwatch harness.");
    }

    private static void BenchmarkIndicatorService()
    {
        var service = new IndicatorService();
        var candles = SampleDataFactory.CreateCandles(300);
        var features = service.CalculateFeatures(candles);

        if (features.Count != candles.Count)
        {
            throw new InvalidOperationException("Indicator benchmark produced an unexpected feature count.");
        }
    }

    private static void BenchmarkAdaptiveOrchestrator()
    {
        var intelligence = ServiceFactory.CreateIntelligenceService()
            .CreateSnapshot("BTCUSDT", "1m", SampleDataFactory.CreateFeatures());
        var orchestrator = ServiceFactory.CreateAdaptiveOrchestrator();

        var decision = orchestrator.Decide(new AdaptiveOrchestrationRequest
        {
            Symbol = "BTCUSDT",
            Interval = "1m",
            Intelligence = intelligence,
            StrategyNames = new List<string>
            {
                "EMA Trend Following",
                "ATR Breakout",
                "RSI Mean Reversion",
                "Bollinger Mean Reversion"
            },
            CurrentStrategyName = "RSI Mean Reversion",
            PersistentAdvantageCycles = 3,
            LastSwitchAt = DateTime.UtcNow.AddHours(-2),
            PortfolioValue = 10000m,
            RiskStatus = RiskStatus.Normal,
            DataQualityPassed = true
        });

        if (string.IsNullOrWhiteSpace(decision.ActiveStrategyName))
        {
            throw new InvalidOperationException("Adaptive benchmark produced an invalid decision.");
        }
    }

    private static bool Matches(string filter, string benchmarkName)
    {
        if (filter == "*" || string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var normalized = filter.Replace("*", string.Empty, StringComparison.Ordinal);
        return benchmarkName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Indicator", StringComparison.OrdinalIgnoreCase) && benchmarkName.Contains("Indicator", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Adaptive", StringComparison.OrdinalIgnoreCase) && benchmarkName.Contains("Adaptive", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("FeatureStore", StringComparison.OrdinalIgnoreCase) && benchmarkName.Contains("FeatureStore", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Aot", StringComparison.OrdinalIgnoreCase) && benchmarkName.Contains("Aot", StringComparison.OrdinalIgnoreCase);
    }
}

public static class ServiceFactory
{
    public static IntelligenceSnapshotService CreateIntelligenceService()
    {
        return new IntelligenceSnapshotService(
            new RegimeDetectionService(),
            new AnomalyDetectionService(),
            new FeatureExtractor(),
            new VolatilityForecastService(),
            new MetaLabelingService(),
            new EventRiskClassifier(),
            new SentimentRiskService(),
            new ModelRegistry(),
            new RagContextProvider(),
            new ExplanationService());
    }

    public static AdaptiveStrategyOrchestrator CreateAdaptiveOrchestrator()
    {
        var performanceTracker = new StrategyPerformanceTracker();

        return new AdaptiveStrategyOrchestrator(
            new MarketRegimeService(),
            new AssetRankingService(),
            new StrategyScoringService(performanceTracker),
            new AdaptivePortfolioAllocator(),
            new DynamicPositionSizingService(),
            new DynamicExitEngine(),
            new ExecutionCostModel(),
            new StrategyHealthMonitor(),
            new TradeAttributionService(),
            new WalkForwardEvaluator(),
            new MultiArmedBanditAllocator(),
            new MarketHealthScore());
    }
}

public static class SampleDataFactory
{
    public static List<Candle> CreateCandles(int count)
    {
        var candles = new List<Candle>(count);
        var start = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var price = 68000m;

        for (var i = 0; i < count; i++)
        {
            var drift = (i % 7) - 3;
            var open = price;
            var close = open + drift + 12m;
            var high = Math.Max(open, close) + 18m;
            var low = Math.Min(open, close) - 18m;
            var volume = 1000m + (i % 20) * 25m;

            candles.Add(new Candle
            {
                Id = i + 1,
                Symbol = "BTCUSDT",
                Interval = "1m",
                OpenTime = start.AddMinutes(i),
                CloseTime = start.AddMinutes(i).AddSeconds(59),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                TakerBuyVolume = volume * 0.54m
            });

            price = close;
        }

        return candles;
    }

    public static List<CandleFeature> CreateFeatures()
    {
        var start = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

        return Enumerable.Range(0, 80)
            .Select(i => new CandleFeature
            {
                CandleId = i + 1,
                Symbol = "BTCUSDT",
                OpenTime = start.AddMinutes(i),
                Ema9 = 110m,
                Ema21 = 108m,
                Ema50 = 100m,
                Ema200 = 90m,
                Rsi14 = 58m,
                MacdHistogram = 1.5m,
                Atr14 = 15m,
                BbMiddle = 1000m,
                BbUpper = 1040m,
                BbLower = 960m,
                Adx = 32m,
                Returns = 0.004m,
                VolumeZScore = 0.8m,
                Spread = 2m,
                Imbalance = 0.1m,
                CalculatedAt = start.AddMinutes(i)
            })
            .ToList();
    }
}
