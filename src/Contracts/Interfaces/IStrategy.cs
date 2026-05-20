using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IStrategy
{
    string Name { get; }
    TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history);
}
