using System.Data;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CryptoTrading.Infrastructure.Persistence;

/// <summary>
/// Implementação do repositório persistente de Features e Candles utilizando Npgsql e Dapper-first.
/// </summary>
public class FeatureStore : IFeatureStore
{
    private readonly string _connectionString;
    private readonly IMetricsService? _metrics;

    public FeatureStore(IConfiguration configuration, IMetricsService? metrics = null)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=cryptotrading;Username=postgres;Password=postgres";
        _metrics = metrics;
    }

    /// <summary>
    /// Construtor para testes unitários ou injeção direta de connection string.
    /// </summary>
    public FeatureStore(string connectionString, IMetricsService? metrics = null)
    {
        _connectionString = connectionString;
        _metrics = metrics;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task InitializeSchemaAsync()
    {
        const string ddl = @"
        CREATE TABLE IF NOT EXISTS candles (
            id BIGSERIAL PRIMARY KEY,
            symbol VARCHAR(20) NOT NULL,
            interval VARCHAR(10) NOT NULL,
            open_time TIMESTAMP WITH TIME ZONE NOT NULL,
            open NUMERIC(28, 8) NOT NULL,
            high NUMERIC(28, 8) NOT NULL,
            low NUMERIC(28, 8) NOT NULL,
            close NUMERIC(28, 8) NOT NULL,
            volume NUMERIC(28, 8) NOT NULL,
            taker_buy_volume NUMERIC(28, 8) NOT NULL DEFAULT 0,
            close_time TIMESTAMP WITH TIME ZONE NOT NULL,
            CONSTRAINT uq_candles UNIQUE (symbol, interval, open_time)
        );

        CREATE TABLE IF NOT EXISTS candle_features (
            candle_id BIGINT PRIMARY KEY REFERENCES candles(id) ON DELETE CASCADE,
            symbol VARCHAR(20) NOT NULL,
            open_time TIMESTAMP WITH TIME ZONE NOT NULL,
            ema_9 NUMERIC(28, 8) NOT NULL,
            ema_21 NUMERIC(28, 8) NOT NULL,
            ema_50 NUMERIC(28, 8) NOT NULL,
            ema_200 NUMERIC(28, 8) NOT NULL,
            rsi_14 NUMERIC(28, 8) NOT NULL,
            macd_value NUMERIC(28, 8) NOT NULL,
            macd_signal NUMERIC(28, 8) NOT NULL,
            macd_histogram NUMERIC(28, 8) NOT NULL,
            atr_14 NUMERIC(28, 8) NOT NULL,
            bb_upper NUMERIC(28, 8) NOT NULL,
            bb_middle NUMERIC(28, 8) NOT NULL,
            bb_lower NUMERIC(28, 8) NOT NULL,
            adx NUMERIC(28, 8) NOT NULL,
            returns NUMERIC(28, 8) NOT NULL DEFAULT 0,
            volume_z_score NUMERIC(28, 8) NOT NULL DEFAULT 0,
            spread NUMERIC(28, 8) NOT NULL DEFAULT 0,
            imbalance NUMERIC(28, 8) NOT NULL DEFAULT 0,
            calculated_at TIMESTAMP WITH TIME ZONE NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_candles_lookup ON candles (symbol, interval, open_time DESC);
        CREATE INDEX IF NOT EXISTS idx_features_lookup ON candle_features (symbol, open_time DESC);

        ALTER TABLE candles ADD COLUMN IF NOT EXISTS taker_buy_volume NUMERIC(28, 8) NOT NULL DEFAULT 0;
        ALTER TABLE candle_features ADD COLUMN IF NOT EXISTS returns NUMERIC(28, 8) NOT NULL DEFAULT 0;
        ALTER TABLE candle_features ADD COLUMN IF NOT EXISTS volume_z_score NUMERIC(28, 8) NOT NULL DEFAULT 0;
        ALTER TABLE candle_features ADD COLUMN IF NOT EXISTS spread NUMERIC(28, 8) NOT NULL DEFAULT 0;
        ALTER TABLE candle_features ADD COLUMN IF NOT EXISTS imbalance NUMERIC(28, 8) NOT NULL DEFAULT 0;

        CREATE TABLE IF NOT EXISTS paper_wallet (
            symbol VARCHAR(20) PRIMARY KEY,
            free NUMERIC(28, 8) NOT NULL,
            locked NUMERIC(28, 8) NOT NULL,
            updated_at TIMESTAMP WITH TIME ZONE NOT NULL
        );

        CREATE TABLE IF NOT EXISTS paper_trades (
            id BIGSERIAL PRIMARY KEY,
            symbol VARCHAR(20) NOT NULL,
            type VARCHAR(10) NOT NULL,
            price NUMERIC(28, 8) NOT NULL,
            quantity NUMERIC(28, 8) NOT NULL,
            fee NUMERIC(28, 8) NOT NULL,
            pnl NUMERIC(28, 8) NOT NULL,
            executed_at TIMESTAMP WITH TIME ZONE NOT NULL
        );

        CREATE TABLE IF NOT EXISTS decision_audits (
            id BIGSERIAL PRIMARY KEY,
            symbol VARCHAR(20) NOT NULL,
            strategy_name VARCHAR(50) NOT NULL,
            signal_type VARCHAR(10) NOT NULL,
            price NUMERIC(28, 8) NOT NULL,
            timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
            decision VARCHAR(20) NOT NULL,
            reason VARCHAR(255) NOT NULL
        );

        INSERT INTO paper_wallet (symbol, free, locked, updated_at)
        VALUES ('USDT', 10000.0, 0.0, NOW())
        ON CONFLICT (symbol) DO NOTHING;

        CREATE TABLE IF NOT EXISTS exchange_filter_info (
            symbol VARCHAR(20) PRIMARY KEY,
            tick_size NUMERIC(28, 8) NOT NULL,
            step_size NUMERIC(28, 8) NOT NULL,
            min_qty NUMERIC(28, 8) NOT NULL,
            max_qty NUMERIC(28, 8) NOT NULL,
            min_notional NUMERIC(28, 8) NOT NULL,
            price_precision INT NOT NULL,
            quantity_precision INT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS testnet_orders (
            id BIGSERIAL PRIMARY KEY,
            symbol VARCHAR(20) NOT NULL,
            client_order_id VARCHAR(50) UNIQUE NOT NULL,
            binance_order_id VARCHAR(50),
            side VARCHAR(10) NOT NULL,
            type VARCHAR(10) NOT NULL,
            price NUMERIC(28, 8) NOT NULL,
            quantity NUMERIC(28, 8) NOT NULL,
            status VARCHAR(20) NOT NULL,
            created_at TIMESTAMP WITH TIME ZONE NOT NULL,
            updated_at TIMESTAMP WITH TIME ZONE NOT NULL
        );

        CREATE TABLE IF NOT EXISTS testnet_audit_logs (
            id BIGSERIAL PRIMARY KEY,
            symbol VARCHAR(20) NOT NULL,
            action VARCHAR(50) NOT NULL,
            status VARCHAR(20) NOT NULL,
            details VARCHAR(255) NOT NULL,
            created_at TIMESTAMP WITH TIME ZONE NOT NULL
        );
        ";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(ddl);
    }

    public async Task SaveCandlesAsync(IEnumerable<Candle> candles)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        const string insertSql = @"
        INSERT INTO candles (symbol, interval, open_time, open, high, low, close, volume, taker_buy_volume, close_time)
        VALUES (@Symbol, @Interval, @OpenTime, @Open, @High, @Low, @Close, @Volume, @TakerBuyVolume, @CloseTime)
        ON CONFLICT (symbol, interval, open_time) DO UPDATE 
        SET open = EXCLUDED.open,
            high = EXCLUDED.high,
            low = EXCLUDED.low,
            close = EXCLUDED.close,
            volume = EXCLUDED.volume,
            taker_buy_volume = EXCLUDED.taker_buy_volume,
            close_time = EXCLUDED.close_time
        RETURNING id;";

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var candleList = candles.ToList();
            foreach (var candle in candleList)
            {
                // Salva e atualiza o ID do objeto para vincular com as features posteriormente
                candle.Id = await conn.QuerySingleAsync<long>(insertSql, candle, tx);
            }
            tx.Commit();
            
            sw.Stop();
            if (_metrics != null)
            {
                _metrics.SetDbLatency(sw.Elapsed.TotalMilliseconds);
                _metrics.IncrementCandles(candleList.Count);
            }
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task SaveFeaturesAsync(IEnumerable<CandleFeature> features)
    {
        // Se a coleção estiver vazia, não executa nada
        var featureList = features.ToList();
        if (!featureList.Any()) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        const string insertSql = @"
        INSERT INTO candle_features (candle_id, symbol, open_time, ema_9, ema_21, ema_50, ema_200, rsi_14, macd_value, macd_signal, macd_histogram, atr_14, bb_upper, bb_middle, bb_lower, adx, returns, volume_z_score, spread, imbalance, calculated_at)
        VALUES (@CandleId, @Symbol, @OpenTime, @Ema9, @Ema21, @Ema50, @Ema200, @Rsi14, @MacdValue, @MacdSignal, @MacdHistogram, @Atr14, @BbUpper, @BbMiddle, @BbLower, @Adx, @Returns, @VolumeZScore, @Spread, @Imbalance, @CalculatedAt)
        ON CONFLICT (candle_id) DO UPDATE
        SET ema_9 = EXCLUDED.ema_9,
            ema_21 = EXCLUDED.ema_21,
            ema_50 = EXCLUDED.ema_50,
            ema_200 = EXCLUDED.ema_200,
            rsi_14 = EXCLUDED.rsi_14,
            macd_value = EXCLUDED.macd_value,
            macd_signal = EXCLUDED.macd_signal,
            macd_histogram = EXCLUDED.macd_histogram,
            atr_14 = EXCLUDED.atr_14,
            bb_upper = EXCLUDED.bb_upper,
            bb_middle = EXCLUDED.bb_middle,
            bb_lower = EXCLUDED.bb_lower,
            adx = EXCLUDED.adx,
            returns = EXCLUDED.returns,
            volume_z_score = EXCLUDED.volume_z_score,
            spread = EXCLUDED.spread,
            imbalance = EXCLUDED.imbalance,
            calculated_at = EXCLUDED.calculated_at;";

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(insertSql, featureList, tx);
            tx.Commit();

            sw.Stop();
            _metrics?.SetDbLatency(sw.Elapsed.TotalMilliseconds);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval)
    {
        const string query = @"
        SELECT open_time 
        FROM candles 
        WHERE symbol = @Symbol AND interval = @Interval 
        ORDER BY open_time DESC 
        LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(query, new { Symbol = symbol, Interval = interval });
    }

    public async Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
    {
        const string query = @"
        SELECT 
            c.id AS Id, c.symbol AS Symbol, c.interval AS Interval, c.open_time AS OpenTime, c.open AS Open, c.high AS High, c.low AS Low, c.close AS Close, c.volume AS Volume, c.taker_buy_volume AS TakerBuyVolume, c.close_time AS CloseTime,
            f.candle_id AS CandleId, f.symbol AS Symbol, f.open_time AS OpenTime, f.ema_9 AS Ema9, f.ema_21 AS Ema21, f.ema_50 AS Ema50, f.ema_200 AS Ema200, f.rsi_14 AS Rsi14, f.macd_value AS MacdValue, f.macd_signal AS MacdSignal, f.macd_histogram AS MacdHistogram, f.atr_14 AS Atr14, f.bb_upper AS BbUpper, f.bb_middle AS BbMiddle, f.bb_lower AS BbLower, f.adx AS Adx, f.returns AS Returns, f.volume_z_score AS VolumeZScore, f.spread AS Spread, f.imbalance AS Imbalance, f.calculated_at AS CalculatedAt
        FROM candles c
        JOIN candle_features f ON c.id = f.candle_id
        WHERE c.symbol = @Symbol AND c.interval = @Interval AND c.open_time >= @StartTime AND c.open_time <= @EndTime
        ORDER BY c.open_time ASC;";

        using var conn = CreateConnection();
        var points = await conn.QueryAsync<Candle, CandleFeature, MarketDataPoint>(
            query,
            (candle, feature) => new MarketDataPoint { Candle = candle, Feature = feature },
            new { Symbol = symbol.ToUpper(), Interval = interval.ToLower(), StartTime = startTime, EndTime = endTime },
            splitOn: "CandleId"
        );

        return points;
    }

    public async Task SaveWalletBalanceAsync(WalletBalance balance)
    {
        const string sql = @"
        INSERT INTO paper_wallet (symbol, free, locked, updated_at) 
        VALUES (@Symbol, @Free, @Locked, @UpdatedAt) 
        ON CONFLICT (symbol) DO UPDATE 
        SET free = EXCLUDED.free, locked = EXCLUDED.locked, updated_at = EXCLUDED.updated_at;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, balance);
    }

    public async Task<IEnumerable<WalletBalance>> GetWalletBalancesAsync()
    {
        const string sql = "SELECT symbol, free, locked, updated_at AS UpdatedAt FROM paper_wallet;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<WalletBalance>(sql);
    }

    public async Task SavePaperTradeAsync(PaperTrade trade)
    {
        const string sql = @"
        INSERT INTO paper_trades (symbol, type, price, quantity, fee, pnl, executed_at) 
        VALUES (@Symbol, @Type, @Price, @Quantity, @Fee, @PnL, @ExecutedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, trade);
    }

    public async Task<IEnumerable<PaperTrade>> GetPaperTradesAsync(string symbol, int limit = 100)
    {
        const string sql = @"
        SELECT id, symbol, type, price, quantity, fee, pnl, executed_at AS ExecutedAt 
        FROM paper_trades 
        WHERE symbol = @Symbol 
        ORDER BY executed_at DESC 
        LIMIT @Limit;";

        using var conn = CreateConnection();
        return await conn.QueryAsync<PaperTrade>(sql, new { Symbol = symbol, Limit = limit });
    }

    public async Task SaveDecisionAuditAsync(DecisionAudit audit)
    {
        const string sql = @"
        INSERT INTO decision_audits (symbol, strategy_name, signal_type, price, timestamp, decision, reason) 
        VALUES (@Symbol, @StrategyName, @SignalType, @Price, @Timestamp, @Decision, @Reason);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, audit);
    }

    public async Task<IEnumerable<DecisionAudit>> GetDecisionAuditsAsync(int limit = 100)
    {
        const string sql = @"
        SELECT id, symbol, strategy_name AS StrategyName, signal_type AS SignalType, price, timestamp, decision, reason 
        FROM decision_audits 
        ORDER BY timestamp DESC 
        LIMIT @Limit;";

        using var conn = CreateConnection();
        return await conn.QueryAsync<DecisionAudit>(sql, new { Limit = limit });
    }

    public async Task ClearPaperTradingDataAsync()
    {
        const string sql = @"
        DELETE FROM paper_trades; 
        DELETE FROM decision_audits; 
        UPDATE paper_wallet SET free = 10000.0, locked = 0.0, updated_at = NOW();";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql);
    }

    public async Task SaveExchangeFilterInfoAsync(ExchangeFilterInfo filter)
    {
        const string sql = @"
        INSERT INTO exchange_filter_info (symbol, tick_size, step_size, min_qty, max_qty, min_notional, price_precision, quantity_precision) 
        VALUES (@Symbol, @TickSize, @StepSize, @MinQty, @MaxQty, @MinNotional, @PricePrecision, @QuantityPrecision) 
        ON CONFLICT (symbol) DO UPDATE 
        SET tick_size = EXCLUDED.tick_size, 
            step_size = EXCLUDED.step_size, 
            min_qty = EXCLUDED.min_qty, 
            max_qty = EXCLUDED.max_qty, 
            min_notional = EXCLUDED.min_notional, 
            price_precision = EXCLUDED.price_precision, 
            quantity_precision = EXCLUDED.quantity_precision;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, filter);
    }

    public async Task<ExchangeFilterInfo?> GetExchangeFilterInfoAsync(string symbol)
    {
        const string sql = @"
        SELECT symbol, tick_size AS TickSize, step_size AS StepSize, min_qty AS MinQty, max_qty AS MaxQty, min_notional AS MinNotional, price_precision AS PricePrecision, quantity_precision AS QuantityPrecision 
        FROM exchange_filter_info 
        WHERE symbol = @Symbol;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ExchangeFilterInfo>(sql, new { Symbol = symbol.ToUpper() });
    }

    public async Task SaveTestnetOrderAsync(TestnetOrder order)
    {
        const string sql = @"
        INSERT INTO testnet_orders (symbol, client_order_id, binance_order_id, side, type, price, quantity, status, created_at, updated_at) 
        VALUES (@Symbol, @ClientOrderId, @BinanceOrderId, @Side, @Type, @Price, @Quantity, @Status, @CreatedAt, @UpdatedAt) 
        ON CONFLICT (client_order_id) DO UPDATE 
        SET binance_order_id = EXCLUDED.binance_order_id, 
            status = EXCLUDED.status, 
            updated_at = EXCLUDED.updated_at;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, order);
    }

    public async Task<TestnetOrder?> GetTestnetOrderAsync(string clientOrderId)
    {
        const string sql = @"
        SELECT id, symbol, client_order_id AS ClientOrderId, binance_order_id AS BinanceOrderId, side, type, price, quantity, status, created_at AS CreatedAt, updated_at AS UpdatedAt 
        FROM testnet_orders 
        WHERE client_order_id = @ClientOrderId;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TestnetOrder>(sql, new { ClientOrderId = clientOrderId });
    }

    public async Task<IEnumerable<TestnetOrder>> GetActiveTestnetOrdersAsync()
    {
        const string sql = @"
        SELECT id, symbol, client_order_id AS ClientOrderId, binance_order_id AS BinanceOrderId, side, type, price, quantity, status, created_at AS CreatedAt, updated_at AS UpdatedAt 
        FROM testnet_orders 
        WHERE status IN ('NEW', 'PARTIALLY_FILLED');";

        using var conn = CreateConnection();
        return await conn.QueryAsync<TestnetOrder>(sql);
    }

    public async Task SaveTestnetAuditLogAsync(TestnetAuditLog log)
    {
        const string sql = @"
        INSERT INTO testnet_audit_logs (symbol, action, status, details, created_at) 
        VALUES (@Symbol, @Action, @Status, @Details, @CreatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, log);
    }

    public async Task<IEnumerable<TestnetAuditLog>> GetTestnetAuditLogsAsync(int limit = 100)
    {
        const string sql = @"
        SELECT id, symbol, action, status, details, created_at AS CreatedAt 
        FROM testnet_audit_logs 
        ORDER BY created_at DESC 
        LIMIT @Limit;";

        using var conn = CreateConnection();
        return await conn.QueryAsync<TestnetAuditLog>(sql, new { Limit = limit });
    }
}
