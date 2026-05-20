using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CryptoTrading.UnitTests;

public class BinanceTestnetTests
{
    private readonly ExchangeRuleValidator _validator = new();

    private class InMemoryFeatureStore : IFeatureStore
    {
        public List<ExchangeFilterInfo> Filters { get; set; } = new();
        public List<TestnetOrder> Orders { get; set; } = new();
        public List<TestnetAuditLog> Logs { get; set; } = new();

        public Task InitializeSchemaAsync() => Task.CompletedTask;
        public Task SaveCandlesAsync(IEnumerable<Candle> candles) => Task.CompletedTask;
        public Task SaveFeaturesAsync(IEnumerable<CandleFeature> features) => Task.CompletedTask;
        public Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval) => Task.FromResult<DateTime?>(null);
        public Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime) => Task.FromResult(Enumerable.Empty<MarketDataPoint>());
        public Task SaveWalletBalanceAsync(WalletBalance balance) => Task.CompletedTask;
        public Task<IEnumerable<WalletBalance>> GetWalletBalancesAsync() => Task.FromResult(Enumerable.Empty<WalletBalance>());
        public Task SavePaperTradeAsync(PaperTrade trade) => Task.CompletedTask;
        public Task<IEnumerable<PaperTrade>> GetPaperTradesAsync(string symbol, int limit = 100) => Task.FromResult(Enumerable.Empty<PaperTrade>());
        public Task SaveDecisionAuditAsync(DecisionAudit audit) => Task.CompletedTask;
        public Task<IEnumerable<DecisionAudit>> GetDecisionAuditsAsync(int limit = 100) => Task.FromResult(Enumerable.Empty<DecisionAudit>());
        public Task ClearPaperTradingDataAsync() => Task.CompletedTask;

        public Task SaveExchangeFilterInfoAsync(ExchangeFilterInfo filter)
        {
            Filters.RemoveAll(f => f.Symbol == filter.Symbol);
            Filters.Add(filter);
            return Task.CompletedTask;
        }

        public Task<ExchangeFilterInfo?> GetExchangeFilterInfoAsync(string symbol) => Task.FromResult(Filters.FirstOrDefault(f => f.Symbol == symbol));

        public Task SaveTestnetOrderAsync(TestnetOrder order)
        {
            Orders.RemoveAll(o => o.ClientOrderId == order.ClientOrderId);
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<TestnetOrder?> GetTestnetOrderAsync(string clientOrderId) => Task.FromResult(Orders.FirstOrDefault(o => o.ClientOrderId == clientOrderId));

        public Task<IEnumerable<TestnetOrder>> GetActiveTestnetOrdersAsync() => Task.FromResult<IEnumerable<TestnetOrder>>(Orders.Where(o => o.Status == "NEW" || o.Status == "PARTIALLY_FILLED"));

        public Task SaveTestnetAuditLogAsync(TestnetAuditLog log)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TestnetAuditLog>> GetTestnetAuditLogsAsync(int limit = 100) => Task.FromResult<IEnumerable<TestnetAuditLog>>(Logs.Take(limit));
    }

    [Fact]
    public void ValidateOrder_InvalidQty_ReturnsFalse()
    {
        var order = new TestnetOrder { Symbol = "BTCUSDT", Quantity = 0.00005m, Type = "LIMIT", Price = 50000m };
        var filters = new ExchangeFilterInfo { Symbol = "BTCUSDT", MinQty = 0.0001m, MaxQty = 1000m, StepSize = 0.0001m };

        var result = _validator.ValidateOrder(order, filters);

        Assert.False(result.IsValid);
        Assert.Contains("menor do que a quantidade mínima", result.Message);
    }

    [Fact]
    public void ValidateOrder_InvalidStepSize_ReturnsFalse()
    {
        var order = new TestnetOrder { Symbol = "BTCUSDT", Quantity = 0.10005m, Type = "LIMIT", Price = 50000m };
        var filters = new ExchangeFilterInfo { Symbol = "BTCUSDT", MinQty = 0.0001m, MaxQty = 1000m, StepSize = 0.01m }; // Requer múltiplo de 0.01

        var result = _validator.ValidateOrder(order, filters);

        Assert.False(result.IsValid);
        Assert.Contains("Step Size", result.Message);
    }

    [Fact]
    public void ValidateOrder_InvalidNotional_ReturnsFalse()
    {
        var order = new TestnetOrder { Symbol = "BTCUSDT", Quantity = 0.0001m, Type = "LIMIT", Price = 10000m }; // Notional = $1.00
        var filters = new ExchangeFilterInfo { Symbol = "BTCUSDT", MinQty = 0.0001m, MaxQty = 1000m, MinNotional = 5.0m };

        var result = _validator.ValidateOrder(order, filters);

        Assert.False(result.IsValid);
        Assert.Contains("Notional total", result.Message);
    }

    [Fact]
    public void ValidateOrder_ValidParams_ReturnsTrue()
    {
        var order = new TestnetOrder { Symbol = "BTCUSDT", Quantity = 0.1m, Type = "LIMIT", Price = 50000m };
        var filters = new ExchangeFilterInfo { Symbol = "BTCUSDT", MinQty = 0.0001m, MaxQty = 1000m, StepSize = 0.001m, TickSize = 0.01m, MinNotional = 5.0m };

        var result = _validator.ValidateOrder(order, filters);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteOrderAsync_DryRunEnabled_FillsMockOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance);
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_1", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order);

        Assert.Equal("FILLED", result.Status);
        Assert.StartsWith("MOCK_BINANCE_", result.BinanceOrderId);
        Assert.Contains(store.Logs, l => l.Action == "DRY_RUN_EXECUTION" && l.Status == "SUCCESS");
    }

    [Fact]
    public async Task ExecuteOrderAsync_ValidationFails_RejectsOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance);
        
        // Quantidade zero invalida a ordem
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_2", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0m };

        var result = await executor.ExecuteOrderAsync(order);

        Assert.Equal("REJECTED", result.Status);
        Assert.Contains(store.Logs, l => l.Action == "VALIDATION_FAILED" && l.Status == "FAILED");
    }

    [Fact]
    public async Task SynchronizeActiveOrders_SyncsPending_UpdatesToFilled()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var sync = new OrderStatusSynchronizer(store, config);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_3", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m, Status = "NEW" };
        await store.SaveTestnetOrderAsync(order);

        int updated = await sync.SynchronizeActiveOrdersAsync();

        Assert.Equal(1, updated);
        var syncedOrder = await store.GetTestnetOrderAsync("ORDER_3");
        Assert.Equal("FILLED", syncedOrder?.Status);
    }
}
