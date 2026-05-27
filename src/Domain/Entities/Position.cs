using System;
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
    public decimal UnrealizedPnL { get; set; }
    public decimal FeesPaid { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public PositionState State { get; set; } = PositionState.Open;
    public bool IsClosed => State == PositionState.Closed;
    /// <summary>Market regime active when trade was entered (set by BacktestEngine). Used for regime-performance breakdown.</summary>
    public string Regime { get; set; } = string.Empty;


    public void UpdateUnrealizedPnL(decimal currentPrice)
    {
        if (IsClosed) return;
        
        if (Type == PositionType.Long)
        {
            UnrealizedPnL = (currentPrice - EntryPrice) * Quantity;
        }
        else
        {
            UnrealizedPnL = (EntryPrice - currentPrice) * Quantity;
        }
    }

    public void Close(decimal exitPrice, DateTime exitTime, decimal exitFee)
    {
        ExitPrice = exitPrice;
        ExitTime = exitTime;
        FeesPaid += exitFee;
        State = PositionState.Closed;

        if (Type == PositionType.Long)
        {
            RealizedPnL = (exitPrice - EntryPrice) * Quantity - FeesPaid;
        }
        else // Short
        {
            RealizedPnL = (EntryPrice - exitPrice) * Quantity - FeesPaid;
        }
        UnrealizedPnL = 0;
    }
    
    public void PartiallyClose(decimal exitPrice, decimal closeQuantity, decimal exitFee)
    {
        if (closeQuantity >= Quantity)
        {
            Close(exitPrice, DateTime.UtcNow, exitFee);
            return;
        }
        
        FeesPaid += exitFee;
        decimal pnl = 0;
        if (Type == PositionType.Long)
        {
            pnl = (exitPrice - EntryPrice) * closeQuantity - exitFee;
        }
        else
        {
            pnl = (EntryPrice - exitPrice) * closeQuantity - exitFee;
        }
        RealizedPnL += pnl;
        Quantity -= closeQuantity;
        State = PositionState.PartiallyClosed;
    }
}
