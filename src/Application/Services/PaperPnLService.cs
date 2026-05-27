using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class PaperPnLService
{
    public decimal CalculateRealizedPnL(IEnumerable<PaperTrade> trades)
    {
        return trades.Where(t => t.Type.Equals("SELL", StringComparison.OrdinalIgnoreCase)).Sum(t => t.PnL);
    }

    public decimal CalculateUnrealizedPnL(Position? position, decimal currentPrice)
    {
        if (position == null || position.IsClosed) return 0m;

        if (position.Type == PositionType.Long)
        {
            return (currentPrice - position.EntryPrice) * position.Quantity;
        }
        else
        {
            return (position.EntryPrice - currentPrice) * position.Quantity;
        }
    }
}
