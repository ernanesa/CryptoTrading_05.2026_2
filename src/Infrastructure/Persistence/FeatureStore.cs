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
            calculated_at TIMESTAMP WITH TIME ZONE NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_candles_lookup ON candles (symbol, interval, open_time DESC);
        CREATE INDEX IF NOT EXISTS idx_features_lookup ON candle_features (symbol, open_time DESC);
        ";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(ddl);
    }

    public async Task SaveCandlesAsync(IEnumerable<Candle> candles)
    {
        const string insertSql = @"
        INSERT INTO candles (symbol, interval, open_time, open, high, low, close, volume, close_time)
        VALUES (@Symbol, @Interval, @OpenTime, @Open, @High, @Low, @Close, @Volume, @CloseTime)
        ON CONFLICT (symbol, interval, open_time) DO UPDATE 
        SET open = EXCLUDED.open,
            high = EXCLUDED.high,
            low = EXCLUDED.low,
            close = EXCLUDED.close,
            volume = EXCLUDED.volume,
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
        INSERT INTO candle_features (candle_id, symbol, open_time, ema_9, ema_21, ema_50, ema_200, rsi_14, macd_value, macd_signal, macd_histogram, atr_14, bb_upper, bb_middle, bb_lower, adx, calculated_at)
        VALUES (@CandleId, @Symbol, @OpenTime, @Ema9, @Ema21, @Ema50, @Ema200, @Rsi14, @MacdValue, @MacdSignal, @MacdHistogram, @Atr14, @BbUpper, @BbMiddle, @BbLower, @Adx, @CalculatedAt)
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
}
