using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class PaperPnLTests
{
    private readonly PaperPnLService _pnlService = new();

    [Fact]
    public void CalculateRealizedPnL_ShouldSumSellPnLs()
    {
        var trades = new List<PaperTrade>
        {
            new PaperTrade { Type = "BUY", Price = 50000m, Quantity = 1m, PnL = 0m },
            new PaperTrade { Type = "SELL", Price = 51000m, Quantity = 0.5m, PnL = 500m },
            new PaperTrade { Type = "SELL", Price = 49000m, Quantity = 0.5m, PnL = -500m }
        };

        var realized = _pnlService.CalculateRealizedPnL(trades);

        Assert.Equal(0m, realized);
    }

    [Fact]
    public void CalculateUnrealizedPnL_Long_ShouldComputeBasedOnCurrentPrice()
    {
        var position = new Position
        {
            Type = PositionType.Long,
            EntryPrice = 50000m,
            Quantity = 2.0m,
            State = PositionState.Open
        };

        var unrealized = _pnlService.CalculateUnrealizedPnL(position, 55000m);

        Assert.Equal(10000m, unrealized);
    }
}
