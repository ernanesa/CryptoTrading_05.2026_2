using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IVolatilityForecastService
{
    VolatilityForecast Forecast(string interval, IReadOnlyList<CandleFeature> features, IntelligenceFeatureVector vector);
}
