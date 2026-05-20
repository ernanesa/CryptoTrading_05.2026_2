using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IEventRiskClassifier
{
    EventRiskSnapshot Classify(IntelligenceFeatureVector vector, VolatilityForecast volatilityForecast);
}
