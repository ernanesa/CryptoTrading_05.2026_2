using System.Data;
using System.Reflection;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Dapper;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith("CryptoTrading.Infrastructure.Persistence.Migrations.Scripts."))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception("Falha na migração do banco de dados (DbUp).", result.Error);
        }

        await Task.CompletedTask;
    }

    public async Task SaveCandlesAsync(IEnumerable<Candle> candles)
    {
        var candleList = candles.ToList();
        if (!candleList.Any()) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        const string insertSql = @"
        INSERT INTO candles (symbol, interval, open_time, open, high, low, close, volume, taker_buy_volume, close_time)
        SELECT * FROM UNNEST(@Symbols, @Intervals, @OpenTimes, @Opens, @Highs, @Lows, @Closes, @Volumes, @TakerBuyVolumes, @CloseTimes)
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
            var parameters = new
            {
                Symbols = candleList.Select(c => c.Symbol).ToArray(),
                Intervals = candleList.Select(c => c.Interval).ToArray(),
                OpenTimes = candleList.Select(c => c.OpenTime).ToArray(),
                Opens = candleList.Select(c => c.Open).ToArray(),
                Highs = candleList.Select(c => c.High).ToArray(),
                Lows = candleList.Select(c => c.Low).ToArray(),
                Closes = candleList.Select(c => c.Close).ToArray(),
                Volumes = candleList.Select(c => c.Volume).ToArray(),
                TakerBuyVolumes = candleList.Select(c => c.TakerBuyVolume).ToArray(),
                CloseTimes = candleList.Select(c => c.CloseTime).ToArray()
            };

            var returnedIds = (await conn.QueryAsync<long>(insertSql, parameters, tx)).ToList();
            
            // Atribuir IDs gerados/atualizados aos objetos de volta, assumindo que a ordem é preservada no UNNEST
            for (int i = 0; i < candleList.Count; i++)
            {
                candleList[i].Id = returnedIds[i];
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

    public async Task SavePaperPositionAsync(Position position)
    {
        using var conn = CreateConnection();
        // Se a posição for nova, Id provavelmente será 0. Não podemos fazer ON CONFLICT em (id) se o id não vier setado.
        // Se usarmos id BIGSERIAL e mandarmos DEFAULT no insert, o ON CONFLICT (id) só funciona para atualizações se o id estiver correto.
        // Wait, a melhor abordagem é se Position.Id == 0 faz INSERT RETURNING id. Se Id > 0 faz UPDATE.
        
        
        if (position.Id == 0)
        {
            const string insertSql = @"
                INSERT INTO paper_positions (symbol, type, entry_price, quantity, entry_time, exit_price, exit_time, realized_pnl, fees_paid, stop_loss_price, take_profit_price, is_closed) 
                VALUES (@Symbol, @TypeStr, @EntryPrice, @Quantity, @EntryTime, @ExitPrice, @ExitTime, @RealizedPnL, @FeesPaid, @StopLossPrice, @TakeProfitPrice, @IsClosed)
                RETURNING id;
            ";
            var typeStr = position.Type.ToString();
            position.Id = await conn.ExecuteScalarAsync<long>(insertSql, new { position.Symbol, TypeStr = typeStr, position.EntryPrice, position.Quantity, position.EntryTime, position.ExitPrice, position.ExitTime, position.RealizedPnL, position.FeesPaid, position.StopLossPrice, position.TakeProfitPrice, position.IsClosed });
        }
        else
        {
            const string updateSql = @"
                UPDATE paper_positions
                SET exit_price = @ExitPrice, exit_time = @ExitTime, realized_pnl = @RealizedPnL, fees_paid = @FeesPaid, stop_loss_price = @StopLossPrice, take_profit_price = @TakeProfitPrice, is_closed = @IsClosed
                WHERE id = @Id;
            ";
            await conn.ExecuteAsync(updateSql, position);
        }
    }

    public async Task<Position?> GetActivePaperPositionAsync(string symbol)
    {
        const string sql = @"
        SELECT id, symbol, type AS TypeStr, entry_price AS EntryPrice, quantity AS Quantity, entry_time AS EntryTime, exit_price AS ExitPrice, exit_time AS ExitTime, realized_pnl AS RealizedPnL, fees_paid AS FeesPaid, stop_loss_price AS StopLossPrice, take_profit_price AS TakeProfitPrice, is_closed AS IsClosed 
        FROM paper_positions 
        WHERE symbol = @Symbol AND is_closed = FALSE
        ORDER BY entry_time DESC 
        LIMIT 1;";

        using var conn = CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Symbol = symbol });
        if (result == null) return null;
        
        return new Position
        {
            Id = result.id,
            Symbol = result.symbol,
            Type = Enum.Parse<CryptoTrading.Domain.Enums.PositionType>((string)result.typestr),
            EntryPrice = result.entryprice,
            Quantity = result.quantity,
            EntryTime = result.entrytime,
            ExitPrice = result.exitprice,
            ExitTime = result.exittime,
            RealizedPnL = result.realizedpnl,
            FeesPaid = result.feespaid,
            StopLossPrice = result.stoplossprice,
            TakeProfitPrice = result.takeprofitprice,
            IsClosed = result.isclosed
        };
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
