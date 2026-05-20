using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class RegimeDetectionService : IRegimeDetectionService
{
    public string Detect(IReadOnlyList<CandleFeature> features)
    {
        if (features.Count == 0)
        {
            return "Unknown";
        }

        var latest = features[^1];
        var volatilityScore = CalculateVolatilityScore(latest);

        if (volatilityScore >= 75m)
        {
            return "HighVolatility";
        }

        if (latest.Adx >= 20m && latest.Ema21 > latest.Ema50)
        {
            return "TrendingUp";
        }

        if (latest.Adx >= 20m && latest.Ema21 < latest.Ema50)
        {
            return "TrendingDown";
        }

        return "Sideways";
    }

    public decimal CalculateConfidence(IReadOnlyList<CandleFeature> features, string regime)
    {
        if (features.Count == 0 || regime == "Unknown")
        {
            return 0m;
        }

        var latest = features[^1];
        var trendStrength = Math.Min(latest.Adx / 50m, 1m);
        var emaDistance = latest.Ema50 != 0m
            ? Math.Min(Math.Abs(latest.Ema21 - latest.Ema50) / latest.Ema50 * 100m, 1m)
            : 0m;
        var volatilityStrength = CalculateVolatilityScore(latest) / 100m;

        var confidence = regime switch
        {
            "HighVolatility" => volatilityStrength,
            "TrendingUp" or "TrendingDown" => (trendStrength * 0.7m) + (emaDistance * 0.3m),
            "Sideways" => 1m - Math.Max(trendStrength, volatilityStrength),
            _ => 0m
        };

        return Math.Round(Math.Clamp(confidence * 100m, 0m, 100m), 2);
    }

    private static decimal CalculateVolatilityScore(CandleFeature feature)
    {
        var atrPressure = feature.BbMiddle > 0m
            ? Math.Min(feature.Atr14 / feature.BbMiddle * 1000m, 100m)
            : 0m;
        var spreadPressure = feature.BbMiddle > 0m
            ? Math.Min(feature.Spread / feature.BbMiddle * 1000m, 100m)
            : 0m;

        return Math.Max(atrPressure, spreadPressure);
    }
}
