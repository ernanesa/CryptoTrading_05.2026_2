using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class TradeSignal
{
    public string Symbol { get; set; } = string.Empty;
    public TradeSignalType Type { get; set; } = TradeSignalType.Hold;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
}
