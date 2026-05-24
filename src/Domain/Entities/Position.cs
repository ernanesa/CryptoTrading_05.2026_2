using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class Position
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public PositionType Type { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal Quantity { get; set; }
    public DateTime EntryTime { get; set; }
    public decimal? ExitPrice { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal FeesPaid { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public bool IsClosed { get; set; }

    public void Close(decimal exitPrice, DateTime exitTime, decimal exitFee)
    {
        ExitPrice = exitPrice;
        ExitTime = exitTime;
        FeesPaid += exitFee;
        IsClosed = true;

        if (Type == PositionType.Long)
        {
            RealizedPnL = (exitPrice - EntryPrice) * Quantity - FeesPaid;
        }
        else // Short
        {
            RealizedPnL = (EntryPrice - exitPrice) * Quantity - FeesPaid;
        }
    }
}
