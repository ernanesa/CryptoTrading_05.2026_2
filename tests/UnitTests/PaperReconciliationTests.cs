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

public class PaperReconciliationTests
{
    [Fact]
    public async Task ReconcileAsync_PerfectMatch_ShouldReturnIsPerfectTrue()
    {
        var store = new PaperTradingTests.InMemoryFeatureStore();
        store.Balances.Clear();
        store.Balances.Add(new WalletBalance { Symbol = "USDT", Free = 5000m, Locked = 0m });
        store.Balances.Add(new WalletBalance { Symbol = "BTC", Free = 0.1m, Locked = 0m });

        // Add 1 buy trade
        store.Trades.Add(new PaperTrade
        {
            Symbol = "BTCUSDT",
            Type = "BUY",
            Price = 50000m,
            Quantity = 0.1m,
            Fee = 5m,
            ExecutedAt = DateTime.UtcNow
        });

        store.Positions.Add(new Position
        {
            Symbol = "BTCUSDT",
            Type = PositionType.Long,
            EntryPrice = 50000m,
            Quantity = 0.1m,
            FeesPaid = 5m,
            State = PositionState.Open,
            EntryTime = DateTime.UtcNow
        });

        var reconciliationService = new PaperReconciliationService(store);
        var result = await reconciliationService.ReconcileAsync("BTCUSDT");

        Assert.True(result.AssetMatches);
        Assert.True(result.IsPerfect);
    }
}
