using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    public decimal CalculateScore(IReadOnlyList<CandleFeature> features)
    {
        if (features.Count == 0)
        {
            return 0m;
        }

        var latest = features[^1];
        var volumePressure = Math.Min(Math.Abs(latest.VolumeZScore) / 4m, 1m);
        var imbalancePressure = Math.Min(Math.Abs(latest.Imbalance), 1m);
        var spreadPressure = CalculateSpreadPressure(latest);
        var returnPressure = Math.Min(Math.Abs(latest.Returns) * 100m, 1m);

        var score = (volumePressure * 35m)
            + (imbalancePressure * 25m)
            + (spreadPressure * 20m)
            + (returnPressure * 20m);

        return Math.Round(Math.Clamp(score, 0m, 100m), 2);
    }

    private static decimal CalculateSpreadPressure(CandleFeature feature)
    {
        if (feature.BbMiddle <= 0m || feature.Spread <= 0m)
        {
            return 0m;
        }

        return Math.Min(feature.Spread / feature.BbMiddle, 1m);
    }
}
