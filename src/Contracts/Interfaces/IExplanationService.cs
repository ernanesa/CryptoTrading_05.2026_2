using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IExplanationService
{
    ExplanationSnapshot Explain(IntelligenceSnapshot snapshot);
}
