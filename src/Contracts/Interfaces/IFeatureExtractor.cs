using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IFeatureExtractor
{
    IntelligenceFeatureVector Extract(IReadOnlyList<CandleFeature> features);
}
