using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.UnitTests;

public class AdaptiveOrchestrationTests
{
    private readonly IntelligenceSnapshotService _intelligence = new(
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

    private readonly AdaptiveStrategyOrchestrator _orchestrator;

    public AdaptiveOrchestrationTests()
    {
        var performanceTracker = new StrategyPerformanceTracker();

        _orchestrator = new AdaptiveStrategyOrchestrator(
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

    [Fact]
    public void Decide_TrendingRegime_SelectsTrendStrategyAndExplainsScores()
    {
        var intelligence = _intelligence.CreateSnapshot("BTCUSDT", "1m",
            CreateFeatures(ema21: 130m, ema50: 100m, adx: 42m, volumeZScore: 0.5m));

        var decision = _orchestrator.Decide(CreateRequest(intelligence));

        Assert.Equal("TrendingUp", decision.MarketRegime);
        Assert.Contains(decision.CandidateStrategyName, new[] { "EMA Trend Following", "ATR Breakout" });
        Assert.NotEmpty(decision.StrategyScores);
        Assert.All(decision.StrategyScores, score =>
        {
            Assert.InRange(score.RegimeFitScore, 0m, 100m);
            Assert.InRange(score.ExpectancyScore, 0m, 100m);
            Assert.InRange(score.ProfitFactorScore, 0m, 100m);
            Assert.InRange(score.DrawdownScore, 0m, 100m);
            Assert.InRange(score.ExecutionCostScore, 0m, 100m);
            Assert.InRange(score.SignalQualityScore, 0m, 100m);
            Assert.InRange(score.StabilityScore, 0m, 100m);
        });
        Assert.NotEmpty(decision.AssetScores);
        Assert.NotEmpty(decision.Reasons);
        Assert.True(decision.PositionSize > 0m);
    }

    [Fact]
    public void Decide_DataQualityBlocked_PreventsStrategySwitchAndPositionSizing()
    {
        var intelligence = _intelligence.CreateSnapshot("ETHUSDT", "1m",
            CreateFeatures(ema21: 130m, ema50: 100m, adx: 42m, volumeZScore: 0.5m));
        var request = CreateRequest(intelligence);
        request.DataQualityPassed = false;

        var decision = _orchestrator.Decide(request);

        Assert.False(decision.ShouldSwitchStrategy);
        Assert.Equal(0m, decision.MarketHealthScore);
        Assert.Equal(0m, decision.PositionSize);
        Assert.Contains("DataQualityGate: BLOCKED", decision.Reasons[2]);
    }

    [Fact]
    public void Decide_HaltedRiskStatus_ForcesZeroPositionSize()
    {
        var intelligence = _intelligence.CreateSnapshot("SOLUSDT", "1m",
            CreateFeatures(ema21: 130m, ema50: 100m, adx: 42m, volumeZScore: 0.5m));
        var request = CreateRequest(intelligence);
        request.RiskStatus = RiskStatus.Halted;

        var decision = _orchestrator.Decide(request);

        Assert.False(decision.ShouldSwitchStrategy);
        Assert.Equal(0m, decision.PositionSize);
    }

    [Fact]
    public void FeedbackStateProjector_HeldDecision_PersistsIncrementedAdvantageCycles()
    {
        var projector = new AdaptiveFeedbackStateProjector();
        var request = new AdaptiveOrchestrationRequest
        {
            PersistentAdvantageCycles = 1
        };
        var previous = new StrategyState
        {
            StrategyName = "RSI Mean Reversion",
            Symbol = "BTCUSDT",
            CooldownUntil = DateTime.UtcNow.AddMinutes(-10),
            AdvantageCycles = 1
        };
        var decision = new AdaptiveOrchestrationDecision
        {
            Symbol = "BTCUSDT",
            ActiveStrategyName = "RSI Mean Reversion",
            CandidateStrategyName = "EMA Trend Following",
            ShouldSwitchStrategy = false,
            StrategyScore = 70m,
            StrategyHealth = new StrategyHealthSnapshot { IsPaused = false },
            StrategyScores = new List<StrategyScoreSnapshot>
            {
                new() { StrategyName = "EMA Trend Following", Score = 80m },
                new() { StrategyName = "RSI Mean Reversion", Score = 70m }
            }
        };

        var projected = projector.Project(request, decision, previous, DateTime.UtcNow);

        Assert.Equal("RSI Mean Reversion", projected.StrategyName);
        Assert.Equal(2, projected.AdvantageCycles);
        Assert.Equal(previous.CooldownUntil, projected.CooldownUntil);
        Assert.Equal(70m, projected.LastScore);
    }

    [Fact]
    public void FeedbackStateProjector_SwitchDecision_ResetsAdvantageCyclesAndStoresSwitchTime()
    {
        var projector = new AdaptiveFeedbackStateProjector();
        var now = DateTime.UtcNow;
        var decision = new AdaptiveOrchestrationDecision
        {
            Symbol = "BTCUSDT",
            ActiveStrategyName = "EMA Trend Following",
            CandidateStrategyName = "EMA Trend Following",
            ShouldSwitchStrategy = true,
            StrategyScore = 83m,
            StrategyHealth = new StrategyHealthSnapshot { IsPaused = true }
        };

        var projected = projector.Project(new AdaptiveOrchestrationRequest(), decision, null, now);

        Assert.Equal("EMA Trend Following", projected.StrategyName);
        Assert.Equal(0, projected.AdvantageCycles);
        Assert.Equal(now, projected.CooldownUntil);
        Assert.True(projected.IsPaused);
        Assert.Equal(83m, projected.LastScore);
    }

    [Fact]
    public async Task MetricsAggregator_PersistsMetricWhenEvidenceMeetsMinimumWindow()
    {
        var now = DateTime.UtcNow;
        var store = new AggregatorFeatureStore
        {
            Trades =
            {
                new() { Symbol = "BTCUSDT", Type = "SELL", PnL = 10m, ExecutedAt = now.AddMinutes(1) },
                new() { Symbol = "BTCUSDT", Type = "SELL", PnL = -5m, ExecutedAt = now.AddMinutes(2) },
                new() { Symbol = "BTCUSDT", Type = "SELL", PnL = 15m, ExecutedAt = now.AddMinutes(3) }
            },
            Audits =
            {
                new() { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", Decision = "APPROVED", Timestamp = now.AddMinutes(1) },
                new() { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", Decision = "APPROVED", Timestamp = now.AddMinutes(2) },
                new() { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", Decision = "APPROVED", Timestamp = now.AddMinutes(3) },
                new() { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", Decision = "REJECTED", Timestamp = now.AddMinutes(4) }
            }
        };
        var backtests = new AggregatorBacktestRepository
        {
            Reports =
            {
                new()
                {
                    StrategyName = "EMA Trend Following",
                    Symbol = "BTCUSDT",
                    Interval = "1m",
                    TotalTrades = 2,
                    WinningTrades = 1,
                    LosingTrades = 1,
                    ProfitFactor = 2m,
                    MaxDrawdownPercent = 4m,
                    MaxConsecutiveLosses = 1,
                    SlippageImpactPercent = 0.03m
                }
            }
        };
        var aggregator = new AdaptiveMetricsAggregator(store, backtests);

        var breakdown = await aggregator.AggregateAndPersistAsync(
            "EMA Trend Following",
            "btcusdt",
            "1m",
            "TrendingUp",
            new AdaptiveMetricsAggregationOptions { MinimumEvidenceSamples = 5 });

        Assert.True(breakdown.HasMinimumEvidence);
        Assert.Equal(6, breakdown.EvidenceSamples);
        Assert.Equal(2, breakdown.BacktestSamples);
        Assert.Equal(3, breakdown.PaperTradeSamples);
        Assert.Equal(0.6000m, breakdown.Metric.WinRate);
        Assert.Equal(1, breakdown.Metric.RiskRejections);
        Assert.Single(store.SavedMetrics);
    }

    [Fact]
    public async Task MetricsAggregator_DoesNotPersistWhenMinimumWindowIsMissing()
    {
        var store = new AggregatorFeatureStore
        {
            Audits =
            {
                new()
                {
                    Symbol = "BTCUSDT",
                    StrategyName = "ATR Breakout",
                    Decision = "REJECTED",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        var aggregator = new AdaptiveMetricsAggregator(store, new AggregatorBacktestRepository());

        var breakdown = await aggregator.AggregateAndPersistAsync(
            "ATR Breakout",
            "BTCUSDT",
            "1m",
            "HighVolatility",
            new AdaptiveMetricsAggregationOptions { MinimumEvidenceSamples = 3 });

        Assert.False(breakdown.HasMinimumEvidence);
        Assert.Equal(1, breakdown.EvidenceSamples);
        Assert.Empty(store.SavedMetrics);
    }

    private static AdaptiveOrchestrationRequest CreateRequest(IntelligenceSnapshot intelligence)
    {
        return new AdaptiveOrchestrationRequest
        {
            Symbol = intelligence.Symbol,
            Interval = intelligence.Interval,
            Intelligence = intelligence,
            StrategyNames = new List<string>
            {
                "EMA Trend Following",
                "ATR Breakout",
                "RSI Mean Reversion",
                "Bollinger Mean Reversion"
            },
            CurrentStrategyName = "RSI Mean Reversion",
            LastSwitchAt = DateTime.UtcNow.AddHours(-2),
            PersistentAdvantageCycles = 3,
            PortfolioValue = 10000m,
            RiskStatus = RiskStatus.Normal,
            DataQualityPassed = true
        };
    }

    private static List<CandleFeature> CreateFeatures(
        decimal ema21,
        decimal ema50,
        decimal adx,
        decimal volumeZScore)
    {
        var start = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);

        return Enumerable.Range(0, 30)
            .Select(i => new CandleFeature
            {
                CandleId = i + 1,
                Symbol = "BTCUSDT",
                OpenTime = start.AddMinutes(i),
                Ema21 = ema21,
                Ema50 = ema50,
                Ema9 = ema21 + 2m,
                Adx = adx,
                Atr14 = 15m,
                BbMiddle = 1000m,
                Spread = 2m,
                VolumeZScore = i == 29 ? volumeZScore : 0.2m,
                Imbalance = 0.1m,
                Returns = i == 29 ? 0.006m : 0.001m,
                MacdHistogram = 1.5m,
                CalculatedAt = start.AddMinutes(i)
            })
            .ToList();
    }

    private sealed class AggregatorBacktestRepository : IBacktestRepository
    {
        public List<BacktestReport> Reports { get; set; } = new();

        public Task SaveReportAsync(BacktestReport report)
        {
            Reports.Add(report);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<BacktestReport>> GetReportsAsync(int limit = 50) =>
            Task.FromResult<IEnumerable<BacktestReport>>(Reports.Take(limit));

        public Task<BacktestReport?> GetLatestReportAsync(string strategyName, string symbol) =>
            Task.FromResult(Reports.LastOrDefault(r =>
                r.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase)
                && r.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class AggregatorFeatureStore : IFeatureStore
    {
        public List<PaperTrade> Trades { get; set; } = new();
        public List<DecisionAudit> Audits { get; set; } = new();
        public List<StrategyPerformanceMetric> SavedMetrics { get; set; } = new();

        public Task SaveCandlesAsync(IEnumerable<Candle> candles) => Task.CompletedTask;
        public Task SaveFeaturesAsync(IEnumerable<CandleFeature> features) => Task.CompletedTask;
        public Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval) => Task.FromResult<DateTime?>(null);
        public Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime) => Task.FromResult(Enumerable.Empty<MarketDataPoint>());
        public Task SaveWalletBalanceAsync(WalletBalance balance) => Task.CompletedTask;
        public Task<IEnumerable<WalletBalance>> GetWalletBalancesAsync() => Task.FromResult(Enumerable.Empty<WalletBalance>());
        public Task SavePaperTradeAsync(PaperTrade trade) { Trades.Add(trade); return Task.CompletedTask; }
        public Task<IEnumerable<PaperTrade>> GetPaperTradesAsync(string symbol, int limit = 100) => Task.FromResult<IEnumerable<PaperTrade>>(Trades.Where(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)).OrderByDescending(t => t.ExecutedAt).Take(limit));
        public Task SavePaperPositionAsync(Position position) => Task.CompletedTask;
        public Task<Position?> GetActivePaperPositionAsync(string symbol) => Task.FromResult<Position?>(null);
        public Task SavePaperOrderAsync(PaperOrder order) => Task.CompletedTask;
        public Task<IEnumerable<PaperOrder>> GetActivePaperOrdersAsync(string symbol) => Task.FromResult(Enumerable.Empty<PaperOrder>());
        public Task SavePaperOrderEventAsync(PaperOrderEvent orderEvent) => Task.CompletedTask;
        public Task<IEnumerable<PaperOrderEvent>> GetPaperOrderEventsAsync(long paperOrderId) => Task.FromResult(Enumerable.Empty<PaperOrderEvent>());
        public Task SaveDecisionAuditAsync(DecisionAudit audit) { Audits.Add(audit); return Task.CompletedTask; }
        public Task<IEnumerable<DecisionAudit>> GetDecisionAuditsAsync(int limit = 100) => Task.FromResult<IEnumerable<DecisionAudit>>(Audits.OrderByDescending(a => a.Timestamp).Take(limit));
        public Task ClearPaperTradingDataAsync() { Trades.Clear(); Audits.Clear(); return Task.CompletedTask; }
        public Task SaveExchangeFilterInfoAsync(ExchangeFilterInfo filter) => Task.CompletedTask;
        public Task<ExchangeFilterInfo?> GetExchangeFilterInfoAsync(string symbol) => Task.FromResult<ExchangeFilterInfo?>(null);
        public Task SaveTestnetOrderAsync(TestnetOrder order) => Task.CompletedTask;
        public Task<TestnetOrder?> GetTestnetOrderAsync(string clientOrderId) => Task.FromResult<TestnetOrder?>(null);
        public Task<IEnumerable<TestnetOrder>> GetActiveTestnetOrdersAsync() => Task.FromResult(Enumerable.Empty<TestnetOrder>());
        public Task SaveTestnetAuditLogAsync(TestnetAuditLog log) => Task.CompletedTask;
        public Task<IEnumerable<TestnetAuditLog>> GetTestnetAuditLogsAsync(int limit = 100) => Task.FromResult(Enumerable.Empty<TestnetAuditLog>());
        public Task SaveStrategyPerformanceMetricAsync(StrategyPerformanceMetric metric) { SavedMetrics.Add(metric); return Task.CompletedTask; }
        public Task<StrategyPerformanceMetric?> GetStrategyPerformanceMetricAsync(string strategyName, string symbol, string timeframe, string regime) => Task.FromResult(SavedMetrics.LastOrDefault(m => m.StrategyName.Equals(strategyName, StringComparison.OrdinalIgnoreCase) && m.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) && m.Timeframe.Equals(timeframe, StringComparison.OrdinalIgnoreCase) && m.Regime.Equals(regime, StringComparison.OrdinalIgnoreCase)));
        public Task SaveStrategyStateAsync(StrategyState state) => Task.CompletedTask;
        public Task<StrategyState?> GetStrategyStateAsync(string strategyName, string symbol) => Task.FromResult<StrategyState?>(null);
    }
}
