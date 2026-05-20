using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class MetaLabelingService : IMetaLabelingService
{
    public MetaLabelingResult Label(
        IntelligenceFeatureVector vector,
        VolatilityForecast volatilityForecast,
        string regime)
    {
        var directionalEdge = Math.Clamp(
            (vector.TrendScore * 0.35m)
            + (vector.MomentumScore * 0.35m)
            + ((100m - volatilityForecast.ForecastScore) * 0.20m)
            + ((100m - vector.LiquidityStressScore) * 0.10m),
            0m,
            100m);

        var label = directionalEdge switch
        {
            >= 65m when regime is "TrendingUp" => "LongContext",
            >= 65m when regime is "TrendingDown" => "ShortContext",
            <= 35m => "Avoid",
            _ => "Neutral"
        };

        return new MetaLabelingResult
        {
            Label = label,
            Probability = Math.Round(directionalEdge, 2),
            QualityScore = Math.Round(Math.Clamp((directionalEdge + volatilityForecast.Confidence) / 2m, 0m, 100m), 2),
            IsTradeContextFavorable = label is "LongContext" or "ShortContext"
                && volatilityForecast.RiskBand != "Elevated"
        };
    }
}
