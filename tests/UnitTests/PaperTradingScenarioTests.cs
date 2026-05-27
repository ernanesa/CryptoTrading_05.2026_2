using Xunit;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Application.Strategies;

namespace CryptoTrading.UnitTests;

public class PaperTradingScenarioTests
{
    [Fact]
    public async Task PaperTradeExecutor_ReconcileOrders_ShouldFillPartial_And_CalculatePnL()
    {
        // Arrange
        var store = new PaperTradingTests.InMemoryFeatureStore();
        await store.SaveWalletBalanceAsync(new WalletBalance { Symbol = "USDT", Free = 1000m });
        var riskEngine = new RiskEngine();
        var executor = new PaperTradeExecutor(store, riskEngine);
        
        var strategy = new EmaTrendFollowingStrategy(); // using any mock strategy
        
        // Simulating the order creation logic by passing a BUY signal (mock point)
        var buyPoint = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", Interval = "1m", Close = 50000m, OpenTime = DateTime.UtcNow, Volume = 2m },
            Feature = new CandleFeature { Spread = 10m }
        };
        
        // Create an order directly or through ProcessSignalAsync
        // Since we patched ProcessSignalAsync to create OrderType.Market orders
        // it should create a BUY order and try to reconcile. Volume = 2m -> availableLiquidity = 0.02m
        // Price = 50000, Wallet = 1000 USDT -> Quantity ~ 0.0196 BTC. So it will fully fill.
        
        // Act
        // This will create order and fill immediately because liquidity (0.02) > qty (0.0196)
        var signal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Buy, Timestamp = DateTime.UtcNow };
        // ProcessSignalAsync expects strategy.GenerateSignal to return this. We can't mock strategy here easily if we don't implement IStrategy.
        // Let's just create an order in the store and call ProcessSignalAsync to trigger reconciliation loop.
        
        var order = new PaperOrder
        {
            Symbol = "BTCUSDT",
            Side = "BUY",
            Type = OrderType.Market,
            Price = 50000m,
            Quantity = 0.05m, // requires 2.5 loops to fill if Volume=2m (liquidity=0.02)
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow
        };
        await store.SavePaperOrderAsync(order);
        
        // Run loop 1
        var dummyStrategy = new EmaTrendFollowingStrategy(); // Will just return hold if not much history
        await executor.ProcessSignalAsync(dummyStrategy, buyPoint);
        
        var activeOrders = await store.GetActivePaperOrdersAsync("BTCUSDT");
        
        // Assert
        Assert.Single(activeOrders);
        var updatedOrder = activeOrders.First();
        Assert.Equal(OrderStatus.PartiallyFilled, updatedOrder.Status);
        Assert.Equal(0.02m, updatedOrder.FilledQuantity);
        Assert.Equal(0.03m, updatedOrder.RemainingQuantity);
        Assert.Equal(["Accepted", "PartiallyFilled"], store.OrderEvents.Select(e => e.EventType));
        
        var position = await store.GetActivePaperPositionAsync("BTCUSDT");
        Assert.NotNull(position);
        Assert.Equal(0.02m, position!.Quantity);
        Assert.Equal(PositionState.Open, position.State);
        
        // Run loop 2
        await executor.ProcessSignalAsync(dummyStrategy, buyPoint);
        var activeOrders2 = await store.GetActivePaperOrdersAsync("BTCUSDT");
        var updatedOrder2 = activeOrders2.First();
        Assert.Equal(0.04m, updatedOrder2.FilledQuantity);
        Assert.Equal("PartiallyFilled", store.OrderEvents.Last().EventType);
        
        // Run loop 3
        await executor.ProcessSignalAsync(dummyStrategy, buyPoint);
        var activeOrders3 = await store.GetActivePaperOrdersAsync("BTCUSDT");
        Assert.Empty(activeOrders3); // Fully filled
        Assert.Equal("Filled", store.OrderEvents.Last().EventType);
        
        var position3 = await store.GetActivePaperPositionAsync("BTCUSDT");
        Assert.Equal(0.05m, position3!.Quantity);
    }

    [Fact]
    public async Task PaperTradeExecutor_ReconcileOrders_ShouldMoveNewLimitOrderToOpen_WhenNotFilled()
    {
        var store = new PaperTradingTests.InMemoryFeatureStore();
        var executor = new PaperTradeExecutor(store, new RiskEngine());
        var point = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", Interval = "1m", OpenTime = DateTime.UtcNow, High = 50100m, Low = 49900m, Close = 50000m, Volume = 1m },
            Feature = new CandleFeature { Spread = 10m }
        };

        await store.SavePaperOrderAsync(new PaperOrder
        {
            Symbol = "BTCUSDT",
            Side = "BUY",
            Type = OrderType.Limit,
            Price = 49000m,
            Quantity = 0.01m,
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow
        });

        await executor.ProcessSignalAsync(new EmaTrendFollowingStrategy(), point);

        var order = Assert.Single(await store.GetActivePaperOrdersAsync("BTCUSDT"));
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(0m, order.FilledQuantity);
        Assert.NotNull(order.UpdatedAt);
        Assert.Empty(store.Trades);
    }

    [Fact]
    public async Task PaperTradeExecutor_ReconcileOrders_ShouldRecordIncrementalSellPnL()
    {
        var store = new PaperTradingTests.InMemoryFeatureStore();
        await store.SaveWalletBalanceAsync(new WalletBalance { Symbol = "BTC", Free = 0.02m, Locked = 0m, UpdatedAt = DateTime.UtcNow });
        await store.SavePaperPositionAsync(new Position
        {
            Symbol = "BTCUSDT",
            Type = PositionType.Long,
            EntryPrice = 50000m,
            Quantity = 0.02m,
            EntryTime = DateTime.UtcNow.AddMinutes(-5),
            State = PositionState.Open
        });

        await store.SavePaperOrderAsync(new PaperOrder
        {
            Symbol = "BTCUSDT",
            Side = "SELL",
            Type = OrderType.Market,
            Price = 51000m,
            Quantity = 0.02m,
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow
        });

        var executor = new PaperTradeExecutor(store, new RiskEngine());
        var point = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", Interval = "1m", OpenTime = DateTime.UtcNow, Close = 51000m, Volume = 10m },
            Feature = new CandleFeature { Spread = 10m }
        };

        await executor.ProcessSignalAsync(new EmaTrendFollowingStrategy(), point);

        var trade = Assert.Single(store.Trades);
        Assert.Equal("SELL", trade.Type);
        Assert.Equal(18.8801m, trade.PnL);

        var position = store.Positions.Single();
        Assert.True(position.IsClosed);
        Assert.Equal(0m, position.Quantity);
        Assert.Equal(0m, store.Balances.Single(b => b.Symbol == "BTC").Free);
    }
}
