using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class FeatureExtractor : IFeatureExtractor
{
    public IntelligenceFeatureVector Extract(IReadOnlyList<CandleFeature> features)
    {
        if (features.Count == 0)
        {
            throw new ArgumentException("At least one feature is required.", nameof(features));
        }

        var latest = features[^1];
        var emaDistance = latest.Ema50 != 0m
            ? Math.Abs(latest.Ema21 - latest.Ema50) / latest.Ema50 * 100m
            : 0m;
        var atrPercent = latest.BbMiddle != 0m
            ? latest.Atr14 / latest.BbMiddle * 100m
            : 0m;
        var spreadPercent = latest.BbMiddle != 0m
            ? latest.Spread / latest.BbMiddle * 100m
            : 0m;

        return new IntelligenceFeatureVector
        {
            OpenTime = latest.OpenTime,
            MomentumScore = ClampScore(50m + (latest.Returns * 1000m) + (latest.MacdHistogram * 2m)),
            TrendScore = ClampScore((Math.Min(latest.Adx, 50m) * 1.4m) + Math.Min(emaDistance * 15m, 30m)),
            VolumePressureScore = ClampScore(Math.Abs(latest.VolumeZScore) * 25m),
            LiquidityStressScore = ClampScore((spreadPercent * 50m) + (Math.Abs(latest.Imbalance) * 50m)),
            NormalizedReturn = Math.Round(latest.Returns * 100m, 4),
            AtrPercent = Math.Round(atrPercent, 4)
        };
    }

    private static decimal ClampScore(decimal score)
    {
        return Math.Round(Math.Clamp(score, 0m, 100m), 2);
    }
}
