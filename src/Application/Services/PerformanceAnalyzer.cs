using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class PerformanceAnalyzer
{
    public void PopulateMetrics(BacktestReport report)
    {
        var trades = report.Trades;
        report.TotalTrades = trades.Count;
        report.TotalFees = trades.Sum(t => t.FeesPaid);
        report.TotalPnL = report.FinalCapital - report.InitialCapital;
        report.TotalPnLPercent = report.InitialCapital > 0
            ? (report.TotalPnL / report.InitialCapital) * 100m
            : 0m;

        if (report.TotalTrades == 0)
        {
            report.WinRate = 0m;
            report.ProfitFactor = 1m;
            report.Expectancy = 0m;
            report.MaxDrawdown = 0m;
            report.MaxDrawdownPercent = 0m;
            return;
        }

        report.WinningTrades = trades.Count(t => t.RealizedPnL > 0);
        report.LosingTrades = trades.Count(t => t.RealizedPnL <= 0);
        report.WinRate = (decimal)report.WinningTrades / report.TotalTrades;

        // Profit Factor: Gross Profit / Gross Loss
        var grossProfit = trades.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL);
        var grossLoss = Math.Abs(trades.Where(t => t.RealizedPnL < 0).Sum(t => t.RealizedPnL));
        report.ProfitFactor = grossLoss > 0 ? grossProfit / grossLoss : (grossProfit > 0 ? 99.9m : 1m);

        // Expectancy: (WinRate * AvgWin) - (LossRate * AvgLoss)
        var avgWin = report.WinningTrades > 0 ? trades.Where(t => t.RealizedPnL > 0).Average(t => t.RealizedPnL) : 0m;
        var avgLoss = report.LosingTrades > 0 ? Math.Abs(trades.Where(t => t.RealizedPnL < 0).Average(t => t.RealizedPnL)) : 0m;
        report.Expectancy = (report.WinRate * avgWin) - ((1m - report.WinRate) * avgLoss);

        // Max Drawdown based on equity curve
        decimal peak = report.InitialCapital;
        decimal currentCapital = report.InitialCapital;
        decimal maxDrawdown = 0m;
        decimal maxDrawdownPercent = 0m;

        foreach (var trade in trades)
        {
            currentCapital += trade.RealizedPnL;
            if (currentCapital > peak) peak = currentCapital;
            var drawdown = peak - currentCapital;
            var drawdownPercent = peak > 0 ? (drawdown / peak) * 100m : 0m;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;
            if (drawdownPercent > maxDrawdownPercent) maxDrawdownPercent = drawdownPercent;
        }

        report.MaxDrawdown = maxDrawdown;
        report.MaxDrawdownPercent = maxDrawdownPercent;

        // Per-trade returns for Sharpe / Sortino
        var returns = trades.Select(t => t.RealizedPnL / report.InitialCapital).ToList();
        if (returns.Count > 1)
        {
            var meanReturn = returns.Average();
            var sumOfSquares = returns.Sum(r => (r - meanReturn) * (r - meanReturn));
            var stdDev = (decimal)Math.Sqrt((double)(sumOfSquares / (returns.Count - 1)));

            if (stdDev > 0)
                report.SharpeRatio = (meanReturn / stdDev) * (decimal)Math.Sqrt((double)returns.Count);

            var negativeReturns = returns.Where(r => r < 0).ToList();
            if (negativeReturns.Count > 1)
            {
                var sumOfNegativeSquares = negativeReturns.Sum(r => r * r);
                var downsideDev = (decimal)Math.Sqrt((double)(sumOfNegativeSquares / negativeReturns.Count));
                if (downsideDev > 0)
                    report.SortinoRatio = (meanReturn / downsideDev) * (decimal)Math.Sqrt((double)returns.Count);
            }
        }

        // Calmar Ratio
        if (report.MaxDrawdownPercent > 0)
            report.CalmarRatio = report.TotalPnLPercent / report.MaxDrawdownPercent;

        // ---- Advanced metrics (Task F) ----

        // Exposure Time: fraction of backtest period where a position was open
        var backtestSpan = (report.EndTime - report.StartTime).TotalHours;
        if (backtestSpan > 0 && trades.Any(t => t.EntryTime != default && t.ExitTime != default))
        {
            var totalHeldHours = trades
                .Where(t => t.EntryTime != default && t.ExitTime != default)
                .Sum(t => (t.ExitTime - t.EntryTime).TotalHours);
            report.ExposureTimePercent = (decimal)(totalHeldHours / backtestSpan) * 100m;
        }

        // Average Holding Time in hours
        var completedTrades = trades.Where(t => t.EntryTime != default && t.ExitTime != default).ToList();
        if (completedTrades.Count > 0)
            report.AvgHoldingTimeHours = completedTrades.Average(t => (t.ExitTime - t.EntryTime).TotalHours);

        // Max Consecutive Losses
        report.MaxConsecutiveLosses = CalculateMaxConsecutiveLosses(trades);

        // Fee Impact: fees as percentage of initial capital
        report.FeeImpactPercent = report.InitialCapital > 0
            ? (report.TotalFees / report.InitialCapital) * 100m
            : 0m;

        // Slippage Impact: estimated using fee model difference (simplified)
        // Actual slippage would be entry-ask vs fill-price difference; here we approximate from spread cost
        report.SlippageImpactPercent = report.FeeImpactPercent * 0.25m; // conservative estimate

        // Regime Performance Breakdown (requires Regime set on Position)
        report.RegimeBreakdown = BuildRegimeBreakdown(trades, report.InitialCapital);
    }

    private static int CalculateMaxConsecutiveLosses(List<Position> trades)
    {
        int maxStreak = 0;
        int currentStreak = 0;
        foreach (var trade in trades)
        {
            if (trade.RealizedPnL < 0)
            {
                currentStreak++;
                if (currentStreak > maxStreak) maxStreak = currentStreak;
            }
            else
            {
                currentStreak = 0;
            }
        }
        return maxStreak;
    }

    private static Dictionary<string, RegimePerformance> BuildRegimeBreakdown(
        List<Position> trades, decimal initialCapital)
    {
        var result = new Dictionary<string, RegimePerformance>(StringComparer.OrdinalIgnoreCase);

        var byRegime = trades
            .GroupBy(t => string.IsNullOrEmpty(t.Regime) ? "Unknown" : t.Regime);

        foreach (var group in byRegime)
        {
            var groupTrades = group.ToList();
            var winners = groupTrades.Count(t => t.RealizedPnL > 0);
            var totalPnL = groupTrades.Sum(t => t.RealizedPnL);
            result[group.Key] = new RegimePerformance
            {
                Regime = group.Key,
                Trades = groupTrades.Count,
                WinRate = groupTrades.Count > 0 ? (decimal)winners / groupTrades.Count : 0m,
                PnL = totalPnL,
                AvgReturn = groupTrades.Count > 0 && initialCapital > 0
                    ? (totalPnL / groupTrades.Count / initialCapital) * 100m
                    : 0m
            };
        }

        return result;
    }
}
