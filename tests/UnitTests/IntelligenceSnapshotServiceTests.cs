using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.UnitTests;

public class IntelligenceSnapshotServiceTests
{
    private readonly IntelligenceSnapshotService _service = new(
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

    [Fact]
    public void CreateSnapshot_TrendingFeatures_ReturnsVersionedSnapshot()
    {
        var features = CreateFeatures(ema21: 110m, ema50: 100m, adx: 32m, volumeZScore: 0.8m);

        var snapshot = _service.CreateSnapshot("btcusdt", "1m", features);

        Assert.Equal("BTCUSDT", snapshot.Symbol);
        Assert.Equal("1m", snapshot.Interval);
        Assert.Equal("intelligence-snapshot/v1", snapshot.SchemaVersion);
        Assert.Equal("heuristic-m6-v1", snapshot.ModelVersion);
        Assert.Equal("score-v1", snapshot.ScoreVersion);
        Assert.Equal("FeatureStore.CandleFeature", snapshot.ScoreSource);
        Assert.Equal("feature-vector/v1", snapshot.FeatureVector.Version);
        Assert.Equal("volatility-heuristic-m6-v1", snapshot.VolatilityForecast.ModelVersion);
        Assert.Equal("meta-label-heuristic-m6-v1", snapshot.MetaLabel.ModelVersion);
        Assert.Equal("sentiment-risk-heuristic-m6-v1", snapshot.SentimentRisk.ModelVersion);
        Assert.Equal("event-risk-heuristic-m6-v1", snapshot.EventRisk.ModelVersion);
        Assert.Equal("rag-context-provider-m6-v1", snapshot.RagContext.ProviderVersion);
        Assert.Equal("explanation-heuristic-m6-v1", snapshot.Explanation.ModelVersion);
        Assert.NotEmpty(snapshot.RegisteredModels);
        Assert.Equal("TrendingUp", snapshot.MarketRegime);
        Assert.True(snapshot.RegimeConfidence > 0m);
        Assert.True(snapshot.VolatilityForecast.HorizonMinutes > 0);
        Assert.NotEmpty(snapshot.Insights);
    }

    [Fact]
    public void CreateSnapshot_UnusualVolumeAndImbalance_FlagsAnomaly()
    {
        var features = CreateFeatures(ema21: 100m, ema50: 100m, adx: 12m, volumeZScore: 4m, imbalance: 1m, returns: 0.03m);

        var snapshot = _service.CreateSnapshot("ETHUSDT", "5m", features);

        Assert.True(snapshot.HasAnomaly);
        Assert.True(snapshot.AnomalyScore >= 70m);
    }

    [Fact]
    public void CreateSnapshot_HighStressFeatures_ProducesElevatedVolatilityForecast()
    {
        var features = CreateFeatures(
            ema21: 100m,
            ema50: 100m,
            adx: 12m,
            volumeZScore: 4m,
            imbalance: 1m,
            returns: 0.03m,
            atr14: 90m,
            spread: 35m);

        var snapshot = _service.CreateSnapshot("SOLUSDT", "5m", features);

        Assert.Equal("Elevated", snapshot.VolatilityForecast.RiskBand);
        Assert.True(snapshot.VolatilityForecast.ForecastScore >= 75m);
        Assert.Equal(15, snapshot.VolatilityForecast.HorizonMinutes);
    }

    [Fact]
    public void CreateSnapshot_ElevatedContext_KeepsIntelligenceAsRiskContextOnly()
    {
        var features = CreateFeatures(
            ema21: 125m,
            ema50: 100m,
            adx: 42m,
            volumeZScore: 4m,
            imbalance: 1m,
            returns: 0.04m,
            atr14: 90m,
            spread: 35m);

        var snapshot = _service.CreateSnapshot("BNBUSDT", "1m", features);

        Assert.False(snapshot.MetaLabel.IsTradeContextFavorable);
        Assert.Equal("RiskOff", snapshot.SentimentRisk.RiskBand);
        Assert.Equal("High", snapshot.EventRisk.Severity);
        Assert.Contains("RiskEngine", snapshot.Explanation.Factors.Last());
    }

    [Fact]
    public void CreateSnapshot_EmptyFeatures_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _service.CreateSnapshot("BTCUSDT", "1m", Array.Empty<CandleFeature>()));

        Assert.Equal("features", ex.ParamName);
    }

    private static List<CandleFeature> CreateFeatures(
        decimal ema21,
        decimal ema50,
        decimal adx,
        decimal volumeZScore,
        decimal imbalance = 0.1m,
        decimal returns = 0.002m,
        decimal atr14 = 15m,
        decimal spread = 2m)
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
                Adx = adx,
                Atr14 = i == 29 ? atr14 : 15m,
                BbMiddle = 1000m,
                Spread = i == 29 ? spread : 2m,
                VolumeZScore = i == 29 ? volumeZScore : 0.2m,
                Imbalance = i == 29 ? imbalance : 0.1m,
                Returns = i == 29 ? returns : 0.001m,
                CalculatedAt = start.AddMinutes(i)
            })
            .ToList();
    }
}
