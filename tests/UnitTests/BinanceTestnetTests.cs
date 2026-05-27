using CryptoTrading.Domain.Enums;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CryptoTrading.UnitTests;

public class BinanceTestnetTests
{
    private readonly ExchangeRuleValidator _validator = new();
    private class MockRiskEngine : IRiskEngine
    {
        public bool ShouldApprove { get; set; } = true;
        public RiskValidationResult ValidateSignal(TradeSignal signal, decimal price, decimal spread, IEnumerable<WalletBalance> balances, IEnumerable<PaperTrade> recentTrades, CryptoTrading.Domain.Enums.RiskStatus currentStatus)
        {
            return new RiskValidationResult { IsApproved = ShouldApprove, Reason = ShouldApprove ? "OK" : "Blocked by risk" };
        }
    }

    private class InMemoryFeatureStore : IFeatureStore
    {
        public List<ExchangeFilterInfo> Filters { get; set; } = new();
        public List<TestnetOrder> Orders { get; set; } = new();
        public List<TestnetAuditLog> Logs { get; set; } = new();
        public List<PaperTrade> Trades { get; set; } = new();
        public List<PaperOrder> _orders { get; set; } = new();
        public List<PaperOrderEvent> OrderEvents { get; set; } = new();
        private readonly List<WalletBalance> _balances = new();
        public List<DecisionAudit> Audits { get; set; } = new();

        public Task SaveCandlesAsync(IEnumerable<Candle> candles) => Task.CompletedTask;
        public Task SaveFeaturesAsync(IEnumerable<CandleFeature> features) => Task.CompletedTask;
        public Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval) => Task.FromResult<DateTime?>(null);
        public Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime) => Task.FromResult(Enumerable.Empty<MarketDataPoint>());
        public Task SaveWalletBalanceAsync(WalletBalance balance) => Task.CompletedTask;
        public Task<IEnumerable<WalletBalance>> GetWalletBalancesAsync() => Task.FromResult(Enumerable.Empty<WalletBalance>());
        public Task SavePaperTradeAsync(PaperTrade trade) { Trades.Add(trade); return Task.CompletedTask; }
        public Task<IEnumerable<PaperTrade>> GetPaperTradesAsync(string symbol, int limit = 100) => Task.FromResult(Trades.Where(t => t.Symbol == symbol).Take(limit));

        public Task SavePaperPositionAsync(Position position) => Task.CompletedTask;
        public Task<Position?> GetActivePaperPositionAsync(string symbol) => Task.FromResult<Position?>(null);

        public Task SaveDecisionAuditAsync(DecisionAudit audit) { Audits.Add(audit); return Task.CompletedTask; }
        public Task<IEnumerable<DecisionAudit>> GetDecisionAuditsAsync(int limit = 100) => Task.FromResult(Enumerable.Empty<DecisionAudit>());
        public Task SavePaperOrderAsync(PaperOrder order)
        {
            if (order.Id == 0) order.Id = _orders.Count + 1;
            var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existing != null) _orders.Remove(existing);
            _orders.Add(order);
            return Task.CompletedTask;
        }
        public Task<IEnumerable<PaperOrder>> GetActivePaperOrdersAsync(string symbol)
        {
            var active = _orders.Where(o => o.Symbol == symbol && (o.Status == OrderStatus.New || o.Status == OrderStatus.Open || o.Status == OrderStatus.PartiallyFilled));
            return Task.FromResult(active);
        }
        public Task SavePaperOrderEventAsync(PaperOrderEvent orderEvent)
        {
            if (orderEvent.Id == 0) orderEvent.Id = OrderEvents.Count + 1;
            OrderEvents.Add(orderEvent);
            return Task.CompletedTask;
        }
        public Task<IEnumerable<PaperOrderEvent>> GetPaperOrderEventsAsync(long paperOrderId) => Task.FromResult<IEnumerable<PaperOrderEvent>>(OrderEvents.Where(e => e.PaperOrderId == paperOrderId));

        public Task SaveStrategyPerformanceMetricAsync(CryptoTrading.Domain.Entities.StrategyPerformanceMetric metric) => Task.CompletedTask; public Task<CryptoTrading.Domain.Entities.StrategyPerformanceMetric?> GetStrategyPerformanceMetricAsync(string strategyName, string symbol, string timeframe, string regime) => Task.FromResult<CryptoTrading.Domain.Entities.StrategyPerformanceMetric?>(null); public Task SaveStrategyStateAsync(CryptoTrading.Domain.Entities.StrategyState state) => Task.CompletedTask; public Task<CryptoTrading.Domain.Entities.StrategyState?> GetStrategyStateAsync(string strategyName, string symbol) => Task.FromResult<CryptoTrading.Domain.Entities.StrategyState?>(null); public Task ClearPaperTradingDataAsync() => Task.CompletedTask;

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

        public Task<IEnumerable<TestnetOrder>> GetActiveTestnetOrdersAsync() => Task.FromResult<IEnumerable<TestnetOrder>>(Orders.Where(o => o.Status is "New" or "PartiallyFilled" or "NEW" or "PARTIALLY_FILLED"));

        public Task SaveTestnetAuditLogAsync(TestnetAuditLog log)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TestnetAuditLog>> GetTestnetAuditLogsAsync(int limit = 100) => Task.FromResult<IEnumerable<TestnetAuditLog>>(Logs.Take(limit));

        public List<PaperLedgerEntry> LedgerEntries { get; set; } = new();
        public Task SavePaperLedgerEntryAsync(PaperLedgerEntry entry) { LedgerEntries.Add(entry); return Task.CompletedTask; }
        public Task<IEnumerable<PaperLedgerEntry>> GetPaperLedgerEntriesAsync(string asset, int limit = 100) => Task.FromResult<IEnumerable<PaperLedgerEntry>>(LedgerEntries.Where(e => e.Asset.Equals(asset, StringComparison.OrdinalIgnoreCase)).Take(limit));
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

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "APPROVED", "Manual approval", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 50000m, 0.1m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_1", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Filled.ToString(), result.Status);
        Assert.Equal("DRY_RUN_SIMULATED_FILL", result.OriginalExchangeStatus);
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

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "APPROVED", "Manual approval", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 50000m, 0m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        // Quantidade zero invalida a ordem
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_2", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        Assert.Contains(store.Logs, l => l.Action == "VALIDATION_FAILED" && l.Status == "FAILED");
    }

    [Fact]
    public async Task ExecuteOrderAsync_RealMode_InvalidCredentials_ThrowsAndLogsSecretMasked()
    {
        var store = new InMemoryFeatureStore();
        await store.SaveExchangeFilterInfoAsync(new ExchangeFilterInfo
        {
            Symbol = "BTCUSDT",
            TickSize = 0.01m,
            StepSize = 0.0001m,
            MinQty = 0.0001m,
            MaxQty = 1000m,
            MinNotional = 5.0m,
            PricePrecision = 2,
            QuantityPrecision = 4
        });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "true" },
            { "Binance:Testnet:ApiKey", "placeholder_key" },
            { "Binance:Testnet:ApiSecret", "secret_to_redact" }
        }).Build();

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "APPROVED", "Manual approval", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 50000m, 0.1m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_REAL", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        
        var failureLog = store.Logs.FirstOrDefault(l => l.Action == "BINANCE_TESTNET_FAILED");
        Assert.NotNull(failureLog);
        Assert.Contains("invalidas", failureLog.Details);
        Assert.DoesNotContain("secret_to_redact", failureLog.Details);
    }

    [Fact]
    public async Task ExecuteOrderAsync_RiskDecisionMissing_RejectsOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var redactor = new SecretRedactor();
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_RISK_MISSING", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, null);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        Assert.Contains(store.Logs, l => l.Action == "RISK_DECISION_MISSING" && l.Status == "FAILED");
    }

    [Fact]
    public async Task ExecuteOrderAsync_RiskDecisionRejected_RejectsOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "REJECTED", "Risk limit reached", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 50000m, 0.1m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_RISK_REJ", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        Assert.Contains(store.Logs, l => l.Action == "RISK_DECISION_REJECTED" && l.Status == "FAILED");
    }

    [Fact]
    public async Task ExecuteOrderAsync_RiskDecisionExpired_RejectsOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "APPROVED", "Manual approval", DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow.AddMinutes(-10), "BUY", 50000m, 0.1m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_RISK_EXP", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        Assert.Contains(store.Logs, l => l.Action == "RISK_DECISION_EXPIRED" && l.Status == "FAILED");
    }

    [Fact]
    public async Task ExecuteOrderAsync_RiskDecisionMismatch_RejectsOrder()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("ETHUSDT", "APPROVED", "Manual approval", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 3500m, 1.0m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_RISK_MISMATCH", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Rejected.ToString(), result.Status);
        Assert.Contains(store.Logs, l => l.Action == "RISK_DECISION_MISMATCH" && l.Status == "FAILED");
    }
    
    [Fact(Skip = "Opt-in: Remova o Skip e coloque chaves reais da Testnet para rodar o teste na exchange real")]
    public async Task ExecuteOrderAsync_RealMode_WithValidCredentials_ShouldSucceed()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "true" },
            { "Binance:Testnet:ApiKey", "YOUR_REAL_TESTNET_KEY" },
            { "Binance:Testnet:ApiSecret", "YOUR_REAL_TESTNET_SECRET" }
        }).Build();

        var redactor = new SecretRedactor();
        var risk = new RiskDecision("BTCUSDT", "APPROVED", "Manual approval", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), "BUY", 40000m, 0.01m);
        var executor = new BinanceTestnetExecutor(store, _validator, config, NullLogger<BinanceTestnetExecutor>.Instance, new MockRiskEngine(), redactor);
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_REAL_OPT_IN", Side = "BUY", Type = "LIMIT", Price = 40000m, Quantity = 0.01m };

        var result = await executor.ExecuteOrderAsync(order, risk);

        Assert.Equal(TestnetOrderStatus.Filled.ToString(), result.Status);
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
        
        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_3", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m, Status = TestnetOrderStatus.New.ToString() };
        await store.SaveTestnetOrderAsync(order);

        int updated = await sync.SynchronizeActiveOrdersAsync();

        Assert.Equal(1, updated);
        var syncedOrder = await store.GetTestnetOrderAsync("ORDER_3");
        Assert.Equal(TestnetOrderStatus.Filled.ToString(), syncedOrder?.Status);
        Assert.Equal("DRY_RUN_SIMULATED_FILL", syncedOrder?.OriginalExchangeStatus);
    }

    [Fact]
    public async Task SynchronizeActiveOrders_RealModeMissingBinanceId_DoesNotAssumeFilled()
    {
        var store = new InMemoryFeatureStore();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "true" }
        }).Build();

        var sync = new OrderStatusSynchronizer(store, config);

        var order = new TestnetOrder { Symbol = "BTCUSDT", ClientOrderId = "ORDER_REAL_SYNC", Side = "BUY", Type = "LIMIT", Price = 50000m, Quantity = 0.1m, Status = TestnetOrderStatus.New.ToString() };
        await store.SaveTestnetOrderAsync(order);

        int updated = await sync.SynchronizeActiveOrdersAsync();

        Assert.Equal(0, updated);
        var syncedOrder = await store.GetTestnetOrderAsync("ORDER_REAL_SYNC");
        Assert.Equal(TestnetOrderStatus.New.ToString(), syncedOrder?.Status);
        Assert.Contains(store.Logs, l => l.Action == "SYNC_ORDER_BINANCE_SKIPPED" && l.Status == "FAILED");
    }
}
