using CryptoTrading.Application.Services;
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
}
