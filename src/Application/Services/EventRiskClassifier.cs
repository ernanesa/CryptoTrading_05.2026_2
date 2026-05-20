using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class EventRiskClassifier : IEventRiskClassifier
{
    public EventRiskSnapshot Classify(
        IntelligenceFeatureVector vector,
        VolatilityForecast volatilityForecast)
    {
        var score = Math.Clamp(
            (volatilityForecast.ForecastScore * 0.45m)
            + (vector.VolumePressureScore * 0.30m)
            + (vector.LiquidityStressScore * 0.25m),
            0m,
            100m);

        var tags = new List<string>();
        if (volatilityForecast.RiskBand == "Elevated")
        {
            tags.Add("volatility-expansion");
        }

        if (vector.VolumePressureScore >= 70m)
        {
            tags.Add("volume-dislocation");
        }

        if (vector.LiquidityStressScore >= 60m)
        {
            tags.Add("liquidity-stress");
        }

        if (tags.Count == 0)
        {
            tags.Add("no-material-event");
        }

        return new EventRiskSnapshot
        {
            EventRiskScore = Math.Round(score, 2),
            Severity = score switch
            {
                >= 75m => "High",
                >= 45m => "Medium",
                _ => "Low"
            },
            EventTags = tags
        };
    }
}
