using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class VolatilityForecastService : IVolatilityForecastService
{
    public VolatilityForecast Forecast(
        string interval,
        IReadOnlyList<CandleFeature> features,
        IntelligenceFeatureVector vector)
    {
        if (features.Count == 0)
        {
            throw new ArgumentException("At least one feature is required.", nameof(features));
        }

        var window = features.TakeLast(Math.Min(features.Count, 20)).ToList();
        var averageAtrPercent = window.Average(CalculateAtrPercent);
        var averageStress = window.Average(CalculateStressScore);
        var forecastScore = Math.Clamp(
            (averageAtrPercent * 12m)
            + (vector.VolumePressureScore * 0.25m)
            + (vector.LiquidityStressScore * 0.25m)
            + (averageStress * 0.25m),
            0m,
            100m);

        return new VolatilityForecast
        {
            HorizonMinutes = EstimateHorizonMinutes(interval),
            ForecastScore = Math.Round(forecastScore, 2),
            ExpectedAtrPercent = Math.Round(Math.Max(vector.AtrPercent, averageAtrPercent), 4),
            Confidence = CalculateConfidence(window.Count, vector),
            RiskBand = ClassifyRiskBand(forecastScore)
        };
    }

    private static decimal CalculateAtrPercent(CandleFeature feature)
    {
        return feature.BbMiddle > 0m ? feature.Atr14 / feature.BbMiddle * 100m : 0m;
    }

    private static decimal CalculateStressScore(CandleFeature feature)
    {
        var spreadPercent = feature.BbMiddle > 0m ? feature.Spread / feature.BbMiddle * 100m : 0m;
        return Math.Clamp((spreadPercent * 50m) + (Math.Abs(feature.Imbalance) * 50m), 0m, 100m);
    }

    private static decimal CalculateConfidence(int sampleSize, IntelligenceFeatureVector vector)
    {
        var sampleConfidence = Math.Min(sampleSize / 20m, 1m) * 70m;
        var sourceConfidence = vector.Source == "FeatureStore.CandleFeature" ? 30m : 10m;
        return Math.Round(Math.Clamp(sampleConfidence + sourceConfidence, 0m, 100m), 2);
    }

    private static string ClassifyRiskBand(decimal forecastScore)
    {
        if (forecastScore >= 75m)
        {
            return "Elevated";
        }

        if (forecastScore >= 45m)
        {
            return "Watch";
        }

        return "Normal";
    }

    private static int EstimateHorizonMinutes(string interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
        {
            return 60;
        }

        var unit = interval[^1];
        var valueText = interval[..^1];
        if (!int.TryParse(valueText, out var value) || value <= 0)
        {
            return 60;
        }

        var baseMinutes = unit switch
        {
            'm' => value,
            'h' => value * 60,
            'd' => value * 1440,
            _ => 60
        };

        return Math.Clamp(baseMinutes * 3, 1, 10080);
    }
}
