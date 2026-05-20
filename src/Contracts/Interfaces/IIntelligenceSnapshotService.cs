using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IIntelligenceSnapshotService
{
    IntelligenceSnapshot CreateSnapshot(string symbol, string interval, IReadOnlyList<CandleFeature> features);
}
