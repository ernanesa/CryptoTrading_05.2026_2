using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class SentimentRiskService : ISentimentRiskService
{
    public SentimentRiskSnapshot Evaluate(
        IntelligenceFeatureVector vector,
        EventRiskSnapshot eventRisk)
    {
        var sentimentScore = Math.Clamp(
            50m
            + ((vector.MomentumScore - 50m) * 0.45m)
            + ((vector.TrendScore - 50m) * 0.35m)
            - (eventRisk.EventRiskScore * 0.20m),
            0m,
            100m);

        var riskScore = Math.Clamp(
            (eventRisk.EventRiskScore * 0.55m)
            + (vector.LiquidityStressScore * 0.25m)
            + (vector.VolumePressureScore * 0.20m),
            0m,
            100m);

        return new SentimentRiskSnapshot
        {
            SentimentScore = Math.Round(sentimentScore, 2),
            RiskScore = Math.Round(riskScore, 2),
            RiskBand = riskScore switch
            {
                >= 75m => "RiskOff",
                >= 45m => "Cautious",
                _ => "Neutral"
            },
            Sources = new List<string>
            {
                "FeatureStore momentum/trend proxy",
                "EventRiskClassifier market context"
            }
        };
    }
}
