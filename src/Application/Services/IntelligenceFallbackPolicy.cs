using System;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class IntelligenceFallbackPolicy
{
    public IntelligenceSnapshot GetFallback(string symbol, string interval, DateTime occurredAt, string reason)
    {
        return new IntelligenceSnapshot
        {
            Symbol = symbol.ToUpperInvariant(),
            Interval = interval,
            SnapshotTime = occurredAt,
            MarketRegime = "Unknown",
            RegimeConfidence = 0.50m,
            AnomalyScore = 0m,
            VolatilityScore = 0m,
            SchemaVersion = "fallback/v1",
            ModelVersion = "fallback-heuristic-v1",
            FeatureVector = new IntelligenceFeatureVector(),
            VolatilityForecast = new VolatilityForecast { RiskBand = "Normal", ForecastScore = 30m },
            MetaLabel = new MetaLabelingResult { Label = "Neutral", QualityScore = 50m },
            EventRisk = new EventRiskSnapshot { Severity = "Normal", EventRiskScore = 30m },
            SentimentRisk = new SentimentRiskSnapshot { RiskBand = "Neutral", RiskScore = 30m },
            Insights = new List<string> { $"Fallback triggered: {reason}" },
            ShadowOutput = new ShadowModelOutput { Explanation = "Fallback shadow mode activated." }
        };
    }
}
