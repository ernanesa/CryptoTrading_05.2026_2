using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class IntelligenceSnapshotService : IIntelligenceSnapshotService
{
    private readonly IRegimeDetectionService _regimeDetection;
    private readonly IAnomalyDetectionService _anomalyDetection;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IVolatilityForecastService _volatilityForecast;
    private readonly IMetaLabelingService _metaLabeling;
    private readonly IEventRiskClassifier _eventRiskClassifier;
    private readonly ISentimentRiskService _sentimentRisk;
    private readonly IModelRegistry _modelRegistry;
    private readonly IRagContextProvider _ragContext;
    private readonly IExplanationService _explanation;

    public IntelligenceSnapshotService(
        IRegimeDetectionService regimeDetection,
        IAnomalyDetectionService anomalyDetection,
        IFeatureExtractor featureExtractor,
        IVolatilityForecastService volatilityForecast,
        IMetaLabelingService metaLabeling,
        IEventRiskClassifier eventRiskClassifier,
        ISentimentRiskService sentimentRisk,
        IModelRegistry modelRegistry,
        IRagContextProvider ragContext,
        IExplanationService explanation)
    {
        _regimeDetection = regimeDetection;
        _anomalyDetection = anomalyDetection;
        _featureExtractor = featureExtractor;
        _volatilityForecast = volatilityForecast;
        _metaLabeling = metaLabeling;
        _eventRiskClassifier = eventRiskClassifier;
        _sentimentRisk = sentimentRisk;
        _modelRegistry = modelRegistry;
        _ragContext = ragContext;
        _explanation = explanation;
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
        var featureVector = _featureExtractor.Extract(features);
        var volatilityForecast = _volatilityForecast.Forecast(interval, features, featureVector);
        var metaLabel = _metaLabeling.Label(featureVector, volatilityForecast, regime);
        var eventRisk = _eventRiskClassifier.Classify(featureVector, volatilityForecast);
        var sentimentRisk = _sentimentRisk.Evaluate(featureVector, eventRisk);

        var snapshot = new IntelligenceSnapshot
        {
            Symbol = symbol.ToUpperInvariant(),
            Interval = interval,
            SnapshotTime = latest.OpenTime,
            MarketRegime = regime,
            RegimeConfidence = _regimeDetection.CalculateConfidence(features, regime),
            AnomalyScore = anomalyScore,
            VolatilityScore = volatilityForecast.ForecastScore,
            FeatureVector = featureVector,
            VolatilityForecast = volatilityForecast,
            MetaLabel = metaLabel,
            EventRisk = eventRisk,
            SentimentRisk = sentimentRisk,
            RagContext = _ragContext.BuildContext(symbol, interval, regime),
            RegisteredModels = _modelRegistry.GetRegisteredModels().ToList(),
            HasAnomaly = anomalyScore >= 70m,
            Insights = BuildInsights(regime, anomalyScore, volatilityForecast, metaLabel, sentimentRisk, eventRisk, latest)
        };

        snapshot.Explanation = _explanation.Explain(snapshot);
        return snapshot;
    }

    private static List<string> BuildInsights(
        string regime,
        decimal anomalyScore,
        VolatilityForecast volatilityForecast,
        MetaLabelingResult metaLabel,
        SentimentRiskSnapshot sentimentRisk,
        EventRiskSnapshot eventRisk,
        CandleFeature latest)
    {
        var insights = new List<string>
        {
            $"Regime detected as {regime} from FeatureStore indicators.",
            $"Anomaly score {anomalyScore:F2}/100 using volume, imbalance, spread and returns.",
            $"Volatility forecast is {volatilityForecast.RiskBand} for {volatilityForecast.HorizonMinutes} minutes.",
            $"Meta-label is {metaLabel.Label}; sentiment risk is {sentimentRisk.RiskBand}; event severity is {eventRisk.Severity}."
        };

        if (volatilityForecast.ForecastScore >= 75m)
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
