using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IMetaLabelingService
{
    MetaLabelingResult Label(IntelligenceFeatureVector vector, VolatilityForecast volatilityForecast, string regime);
}
