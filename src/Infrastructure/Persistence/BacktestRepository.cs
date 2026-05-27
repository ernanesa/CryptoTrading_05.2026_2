using System.Data;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CryptoTrading.Infrastructure.Persistence;

public class BacktestRepository : IBacktestRepository
{
    private readonly string _connectionString;

    public BacktestRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=cryptotrading;Username=postgres;Password=postgres";
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task SaveReportAsync(BacktestReport report)
    {
        const string runSql = @"
        INSERT INTO backtest_runs (strategy_name, symbol, interval, start_time, end_time, initial_capital, final_capital, total_trades, winning_trades, losing_trades, win_rate, max_drawdown_percent, sharpe_ratio, profit_factor, sortino_ratio, calmar_ratio)
        VALUES (@StrategyName, @Symbol, @Interval, @StartTime, @EndTime, @InitialCapital, @FinalCapital, @TotalTrades, @WinningTrades, @LosingTrades, @WinRate, @MaxDrawdownPercent, @SharpeRatio, @ProfitFactor, @SortinoRatio, @CalmarRatio)
        RETURNING id;";

        const string tradeSql = @"
        INSERT INTO backtest_trades (backtest_run_id, symbol, type, entry_price, exit_price, quantity, entry_time, exit_time, fees_paid, pnl)
        VALUES (@BacktestRunId, @Symbol, @Type, @EntryPrice, @ExitPrice, @Quantity, @EntryTime, @ExitTime, @FeesPaid, @PnL);";

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var runId = await conn.ExecuteScalarAsync<long>(runSql, report, tx);

            if (report.Trades.Any())
            {
                var tradeData = report.Trades.Select(t => new
                {
                    BacktestRunId = runId,
                    t.Symbol,
                    Type = t.Type.ToString(),
                    t.EntryPrice,
                    t.ExitPrice,
                    t.Quantity,
                    t.EntryTime,
                    t.ExitTime,
                    t.FeesPaid,
                    PnL = t.RealizedPnL
                });

                await conn.ExecuteAsync(tradeSql, tradeData, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<BacktestReport>> GetReportsAsync(int limit = 50)
    {
        const string sql = @"
        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval, start_time AS StartTime, end_time AS EndTime, initial_capital AS InitialCapital, final_capital AS FinalCapital, total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades, win_rate AS WinRate, max_drawdown_percent AS MaxDrawdownPercent, sharpe_ratio AS SharpeRatio, profit_factor AS ProfitFactor, sortino_ratio AS SortinoRatio, calmar_ratio AS CalmarRatio 
        FROM backtest_runs 
        ORDER BY executed_at DESC 
        LIMIT @Limit;";

        using var conn = CreateConnection();
        return await conn.QueryAsync<BacktestReport>(sql, new { Limit = limit });
    }

    public async Task<BacktestReport?> GetLatestReportAsync(string strategyName, string symbol)
    {
        const string sql = @"
        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval, start_time AS StartTime, end_time AS EndTime, initial_capital AS InitialCapital, final_capital AS FinalCapital, total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades, win_rate AS WinRate, max_drawdown_percent AS MaxDrawdownPercent, sharpe_ratio AS SharpeRatio, profit_factor AS ProfitFactor, sortino_ratio AS SortinoRatio, calmar_ratio AS CalmarRatio 
        FROM backtest_runs 
        WHERE strategy_name = @StrategyName AND symbol = @Symbol
        ORDER BY executed_at DESC 
        LIMIT 1;";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BacktestReport>(sql, new { StrategyName = strategyName, Symbol = symbol });
    }
}
