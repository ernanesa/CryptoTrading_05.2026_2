using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface ISentimentRiskService
{
    SentimentRiskSnapshot Evaluate(IntelligenceFeatureVector vector, EventRiskSnapshot eventRisk);
}
