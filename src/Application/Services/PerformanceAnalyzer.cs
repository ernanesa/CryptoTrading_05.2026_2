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

        // Profit Factor: Lucro Bruto / Prejuízo Bruto
        var grossProfit = trades.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL);
        var grossLoss = Math.Abs(trades.Where(t => t.RealizedPnL < 0).Sum(t => t.RealizedPnL));
        report.ProfitFactor = grossLoss > 0 ? grossProfit / grossLoss : (grossProfit > 0 ? 99.9m : 1m);

        // Expectancy: (WinRate * AvgWin) - (LossRate * AvgLoss)
        var avgWin = report.WinningTrades > 0 ? trades.Where(t => t.RealizedPnL > 0).Average(t => t.RealizedPnL) : 0m;
        var avgLoss = report.LosingTrades > 0 ? Math.Abs(trades.Where(t => t.RealizedPnL < 0).Average(t => t.RealizedPnL)) : 0m;
        report.Expectancy = (report.WinRate * avgWin) - ((1m - report.WinRate) * avgLoss);

        // Calcular Max Drawdown baseado na curva de capital após cada trade fechado
        decimal peak = report.InitialCapital;
        decimal currentCapital = report.InitialCapital;
        decimal maxDrawdown = 0m;
        decimal maxDrawdownPercent = 0m;

        foreach (var trade in trades)
        {
            currentCapital += trade.RealizedPnL;
            if (currentCapital > peak)
            {
                peak = currentCapital;
            }

            var drawdown = peak - currentCapital;
            var drawdownPercent = peak > 0 ? (drawdown / peak) * 100m : 0m;

            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
            if (drawdownPercent > maxDrawdownPercent)
            {
                maxDrawdownPercent = drawdownPercent;
            }
        }

        report.MaxDrawdown = maxDrawdown;
        report.MaxDrawdownPercent = maxDrawdownPercent;
    }
}
