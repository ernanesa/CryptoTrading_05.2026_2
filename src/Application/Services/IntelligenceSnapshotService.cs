using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class IntelligenceSnapshotService : IIntelligenceSnapshotService
{
    private readonly IRegimeDetectionService _regimeDetection;
    private readonly IAnomalyDetectionService _anomalyDetection;

    public IntelligenceSnapshotService(
        IRegimeDetectionService regimeDetection,
        IAnomalyDetectionService anomalyDetection)
    {
        _regimeDetection = regimeDetection;
        _anomalyDetection = anomalyDetection;
    }

    public IntelligenceSnapshot CreateSnapshot(string symbol, string interval, IReadOnlyList<CandleFeature> features)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required.", nameof(symbol));
        }

        if (string.IsNullOrWhiteSpace(interval))
        {
            throw new ArgumentException("Interval is required.", nameof(interval));
        }

        if (features.Count == 0)
        {
            throw new ArgumentException("At least one feature is required.", nameof(features));
        }

        var latest = features[^1];
        var regime = _regimeDetection.Detect(features);
        var anomalyScore = _anomalyDetection.CalculateScore(features);
        var volatilityScore = CalculateVolatilityScore(latest);

        return new IntelligenceSnapshot
        {
            Symbol = symbol.ToUpperInvariant(),
            Interval = interval,
            SnapshotTime = latest.OpenTime,
            MarketRegime = regime,
            RegimeConfidence = _regimeDetection.CalculateConfidence(features, regime),
            AnomalyScore = anomalyScore,
            VolatilityScore = volatilityScore,
            HasAnomaly = anomalyScore >= 70m,
            Insights = BuildInsights(regime, anomalyScore, volatilityScore, latest)
        };
    }

    private static decimal CalculateVolatilityScore(CandleFeature feature)
    {
        if (feature.BbMiddle <= 0m)
        {
            return 0m;
        }

        var atrPressure = Math.Min(feature.Atr14 / feature.BbMiddle * 1000m, 100m);
        var spreadPressure = Math.Min(feature.Spread / feature.BbMiddle * 1000m, 100m);

        return Math.Round(Math.Max(atrPressure, spreadPressure), 2);
    }

    private static List<string> BuildInsights(
        string regime,
        decimal anomalyScore,
        decimal volatilityScore,
        CandleFeature latest)
    {
        var insights = new List<string>
        {
            $"Regime detected as {regime} from FeatureStore indicators.",
            $"Anomaly score {anomalyScore:F2}/100 using volume, imbalance, spread and returns."
        };

        if (volatilityScore >= 75m)
        {
            insights.Add("Volatility context is elevated; downstream decisioning must remain gated by RiskEngine.");
        }

        if (Math.Abs(latest.VolumeZScore) >= 2m)
        {
            insights.Add($"Volume z-score is unusual at {latest.VolumeZScore:F2}.");
        }

        return insights;
    }
}
