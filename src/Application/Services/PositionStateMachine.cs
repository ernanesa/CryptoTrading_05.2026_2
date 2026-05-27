using System;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public static class PositionStateMachine
{
    public static void Open(Position position, decimal entryPrice, decimal quantity, DateTime timestamp)
    {
        if (position.State != PositionState.Open)
        {
            throw new InvalidOperationException($"Cannot open a position when state is {position.State}");
        }
        position.EntryPrice = entryPrice;
        position.Quantity = quantity;
        position.EntryTime = timestamp;
        position.State = PositionState.Open;
    }

    public static void PartiallyClose(Position position, decimal exitPrice, decimal closeQuantity, decimal exitFee, DateTime timestamp)
    {
        if (position.State != PositionState.Open && position.State != PositionState.PartiallyClosed)
        {
            throw new InvalidOperationException($"Cannot partially close a position when state is {position.State}");
        }
        if (closeQuantity <= 0)
        {
            throw new ArgumentException("Close quantity must be greater than zero", nameof(closeQuantity));
        }

        if (closeQuantity >= position.Quantity)
        {
            Close(position, exitPrice, timestamp, exitFee);
            return;
        }

        position.FeesPaid += exitFee;
        decimal pnl = 0;
        if (position.Type == PositionType.Long)
        {
            pnl = (exitPrice - position.EntryPrice) * closeQuantity - exitFee;
        }
        else
        {
            pnl = (position.EntryPrice - exitPrice) * closeQuantity - exitFee;
        }

        position.RealizedPnL += pnl;
        position.Quantity -= closeQuantity;
        position.State = PositionState.PartiallyClosed;
    }

    public static void Close(Position position, decimal exitPrice, DateTime timestamp, decimal exitFee)
    {
        if (position.State == PositionState.Closed)
        {
            throw new InvalidOperationException("Position is already closed");
        }

        position.ExitPrice = exitPrice;
        position.ExitTime = timestamp;
        position.FeesPaid += exitFee;
        position.State = PositionState.Closed;

        if (position.Type == PositionType.Long)
        {
            position.RealizedPnL += (exitPrice - position.EntryPrice) * position.Quantity - exitFee;
        }
        else
        {
            position.RealizedPnL += (position.EntryPrice - exitPrice) * position.Quantity - exitFee;
        }

        position.Quantity = 0m;
        position.UnrealizedPnL = 0m;
    }
}
