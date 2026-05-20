using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IAnomalyDetectionService
{
    decimal CalculateScore(IReadOnlyList<CandleFeature> features);
}
