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

    public FeatureStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=cryptotrading;Username=postgres;Password=postgres";
    }

    /// <summary>
    /// Construtor para testes unitários ou injeção direta de connection string.
    /// </summary>
    public FeatureStore(string connectionString)
    {
        _connectionString = connectionString;
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
        ";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(ddl);
    }

    public async Task SaveCandlesAsync(IEnumerable<Candle> candles)
    {
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
            foreach (var candle in candles)
            {
                // Salva e atualiza o ID do objeto para vincular com as features posteriormente
                candle.Id = await conn.QuerySingleAsync<long>(insertSql, candle, tx);
            }
            tx.Commit();
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
        if (!features.Any()) return;

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
            await conn.ExecuteAsync(insertSql, features, tx);
            tx.Commit();
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
}
