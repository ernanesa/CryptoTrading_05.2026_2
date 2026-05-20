using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IRegimeDetectionService
{
    string Detect(IReadOnlyList<CandleFeature> features);
    decimal CalculateConfidence(IReadOnlyList<CandleFeature> features, string regime);
}
