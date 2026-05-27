using System.Text.Json;
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

    public BacktestRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task SaveReportAsync(BacktestReport report)
    {
        const string runSql = @"
        INSERT INTO backtest_runs (
            strategy_name, symbol, interval, start_time, end_time,
            initial_capital, final_capital, total_pnl, total_pnl_percent,
            total_trades, winning_trades, losing_trades, win_rate,
            max_drawdown, max_drawdown_percent, profit_factor, expectancy, total_fees,
            sharpe_ratio, sortino_ratio, calmar_ratio,
            exposure_time_percent, avg_holding_time_hours, max_consecutive_losses,
            fee_impact_percent, slippage_impact_percent, regime_breakdown)
        VALUES (
            @StrategyName, @Symbol, @Interval, @StartTime, @EndTime,
            @InitialCapital, @FinalCapital, @TotalPnL, @TotalPnLPercent,
            @TotalTrades, @WinningTrades, @LosingTrades, @WinRate,
            @MaxDrawdown, @MaxDrawdownPercent, @ProfitFactor, @Expectancy, @TotalFees,
            @SharpeRatio, @SortinoRatio, @CalmarRatio,
            @ExposureTimePercent, @AvgHoldingTimeHours, @MaxConsecutiveLosses,
            @FeeImpactPercent, @SlippageImpactPercent, CAST(@RegimeBreakdownJson AS jsonb))
        RETURNING id;";

        const string tradeSql = @"
        INSERT INTO backtest_trades (backtest_run_id, symbol, type, entry_price, exit_price, quantity, entry_time, exit_time, fees_paid, pnl, regime)
        VALUES (@BacktestRunId, @Symbol, @Type, @EntryPrice, @ExitPrice, @Quantity, @EntryTime, @ExitTime, @FeesPaid, @PnL, @Regime);";

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var runId = await conn.ExecuteScalarAsync<long>(runSql, new
            {
                report.StrategyName,
                report.Symbol,
                report.Interval,
                report.StartTime,
                report.EndTime,
                report.InitialCapital,
                report.FinalCapital,
                report.TotalPnL,
                report.TotalPnLPercent,
                report.TotalTrades,
                report.WinningTrades,
                report.LosingTrades,
                report.WinRate,
                report.MaxDrawdown,
                report.MaxDrawdownPercent,
                report.ProfitFactor,
                report.Expectancy,
                report.TotalFees,
                report.SharpeRatio,
                report.SortinoRatio,
                report.CalmarRatio,
                report.ExposureTimePercent,
                report.AvgHoldingTimeHours,
                report.MaxConsecutiveLosses,
                report.FeeImpactPercent,
                report.SlippageImpactPercent,
                RegimeBreakdownJson = JsonSerializer.Serialize(report.RegimeBreakdown)
            }, tx);

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
                    PnL = t.RealizedPnL,
                    Regime = string.IsNullOrWhiteSpace(t.Regime) ? "Unknown" : t.Regime
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
        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval,
            start_time AS StartTime, end_time AS EndTime,
            initial_capital AS InitialCapital, final_capital AS FinalCapital,
            total_pnl AS TotalPnL, total_pnl_percent AS TotalPnLPercent,
            total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades,
            win_rate AS WinRate, max_drawdown AS MaxDrawdown, max_drawdown_percent AS MaxDrawdownPercent,
            profit_factor AS ProfitFactor, expectancy AS Expectancy, total_fees AS TotalFees,
            sharpe_ratio AS SharpeRatio, sortino_ratio AS SortinoRatio, calmar_ratio AS CalmarRatio,
            exposure_time_percent AS ExposureTimePercent, avg_holding_time_hours AS AvgHoldingTimeHours,
            max_consecutive_losses AS MaxConsecutiveLosses, fee_impact_percent AS FeeImpactPercent,
            slippage_impact_percent AS SlippageImpactPercent, regime_breakdown::text AS RegimeBreakdownJson
        FROM backtest_runs 
        ORDER BY executed_at DESC 
        LIMIT @Limit;";

        using var conn = CreateConnection();
        var rows = await conn.QueryAsync<BacktestReportRow>(sql, new { Limit = limit });
        return rows.Select(MapReport);
    }

    public async Task<BacktestReport?> GetLatestReportAsync(string strategyName, string symbol)
    {
        const string sql = @"
        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval,
            start_time AS StartTime, end_time AS EndTime,
            initial_capital AS InitialCapital, final_capital AS FinalCapital,
            total_pnl AS TotalPnL, total_pnl_percent AS TotalPnLPercent,
            total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades,
            win_rate AS WinRate, max_drawdown AS MaxDrawdown, max_drawdown_percent AS MaxDrawdownPercent,
            profit_factor AS ProfitFactor, expectancy AS Expectancy, total_fees AS TotalFees,
            sharpe_ratio AS SharpeRatio, sortino_ratio AS SortinoRatio, calmar_ratio AS CalmarRatio,
            exposure_time_percent AS ExposureTimePercent, avg_holding_time_hours AS AvgHoldingTimeHours,
            max_consecutive_losses AS MaxConsecutiveLosses, fee_impact_percent AS FeeImpactPercent,
            slippage_impact_percent AS SlippageImpactPercent, regime_breakdown::text AS RegimeBreakdownJson
        FROM backtest_runs 
        WHERE strategy_name = @StrategyName AND symbol = @Symbol
        ORDER BY executed_at DESC 
        LIMIT 1;";

        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<BacktestReportRow>(sql, new { StrategyName = strategyName, Symbol = symbol });
        return row is null ? null : MapReport(row);
    }

    private static BacktestReport MapReport(BacktestReportRow row)
    {
        return new BacktestReport
        {
            StrategyName = row.StrategyName,
            Symbol = row.Symbol,
            Interval = row.Interval,
            StartTime = row.StartTime,
            EndTime = row.EndTime,
            InitialCapital = row.InitialCapital,
            FinalCapital = row.FinalCapital,
            TotalPnL = row.TotalPnL,
            TotalPnLPercent = row.TotalPnLPercent,
            TotalTrades = row.TotalTrades,
            WinningTrades = row.WinningTrades,
            LosingTrades = row.LosingTrades,
            WinRate = row.WinRate,
            MaxDrawdown = row.MaxDrawdown,
            MaxDrawdownPercent = row.MaxDrawdownPercent,
            ProfitFactor = row.ProfitFactor,
            Expectancy = row.Expectancy,
            TotalFees = row.TotalFees,
            SharpeRatio = row.SharpeRatio,
            SortinoRatio = row.SortinoRatio,
            CalmarRatio = row.CalmarRatio,
            ExposureTimePercent = row.ExposureTimePercent,
            AvgHoldingTimeHours = row.AvgHoldingTimeHours,
            MaxConsecutiveLosses = row.MaxConsecutiveLosses,
            FeeImpactPercent = row.FeeImpactPercent,
            SlippageImpactPercent = row.SlippageImpactPercent,
            RegimeBreakdown = DeserializeRegimeBreakdown(row.RegimeBreakdownJson)
        };
    }

    private static Dictionary<string, RegimePerformance> DeserializeRegimeBreakdown(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, RegimePerformance>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, RegimePerformance>>(json)
            ?? new Dictionary<string, RegimePerformance>();
    }

    private sealed class BacktestReportRow
    {
        public string StrategyName { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal InitialCapital { get; set; }
        public decimal FinalCapital { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal TotalPnLPercent { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal MaxDrawdownPercent { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal Expectancy { get; set; }
        public decimal TotalFees { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal SortinoRatio { get; set; }
        public decimal CalmarRatio { get; set; }
        public decimal ExposureTimePercent { get; set; }
        public double AvgHoldingTimeHours { get; set; }
        public int MaxConsecutiveLosses { get; set; }
        public decimal FeeImpactPercent { get; set; }
        public decimal SlippageImpactPercent { get; set; }
        public string? RegimeBreakdownJson { get; set; }
    }
}
