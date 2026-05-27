using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class ShadowModelRunner
{
    public ShadowModelOutput Run(IntelligenceFeatureVector featureVector, VolatilityForecast volatility, decimal anomalyScore)
    {
        var riskPressure = (featureVector.LiquidityStressScore * 0.25m)
            + (volatility.ForecastScore * 0.35m)
            + (anomalyScore * 0.25m)
            + (Math.Abs(featureVector.MomentumScore - 50m) * 0.15m);

        var score = Math.Round(Math.Clamp(riskPressure, 0m, 100m), 2);
        var confidence = Math.Round(Math.Clamp(100m - Math.Abs(score - 50m), 0m, 100m), 2);

        return new ShadowModelOutput
        {
            Score = score,
            Confidence = confidence,
            Label = score >= 70m ? "RiskOff" : score >= 45m ? "Caution" : "Supportive",
            DriftStatus = anomalyScore >= 80m || volatility.ForecastScore >= 85m ? "Watch" : "Stable",
            Explanation = "Shadow model scored market context only; execution remains gated by RiskEngine/RiskDecision."
        };
    }
}
