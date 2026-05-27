using System.Text.Json;
using System.Text;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public static class ReportExporter
{
    public static string ToJson(BacktestReport report)
    {
        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string ToMarkdown(BacktestReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Backtest Report: {report.StrategyName}");
        sb.AppendLine();
        sb.AppendLine($"**Symbol:** {report.Symbol}");
        sb.AppendLine($"**Interval:** {report.Interval}");
        sb.AppendLine($"**Period:** {report.StartTime:yyyy-MM-dd} to {report.EndTime:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("## Performance Summary");
        sb.AppendLine($"- **Initial Capital:** {report.InitialCapital:C}");
        sb.AppendLine($"- **Final Capital:** {report.FinalCapital:C}");
        sb.AppendLine($"- **Total PnL:** {report.TotalPnL:C} ({report.TotalPnLPercent:F2}%)");
        sb.AppendLine($"- **Total Fees:** {report.TotalFees:C}");
        sb.AppendLine();
        sb.AppendLine("## Trade Statistics");
        sb.AppendLine($"- **Total Trades:** {report.TotalTrades}");
        sb.AppendLine($"- **Winning / Losing:** {report.WinningTrades} / {report.LosingTrades}");
        sb.AppendLine($"- **Win Rate:** {report.WinRate * 100:F2}%");
        sb.AppendLine($"- **Profit Factor:** {report.ProfitFactor:F2}");
        sb.AppendLine($"- **Expectancy:** {report.Expectancy:C}");
        sb.AppendLine();
        sb.AppendLine("## Risk Metrics");
        sb.AppendLine($"- **Max Drawdown:** {report.MaxDrawdown:C} ({report.MaxDrawdownPercent:F2}%)");
        sb.AppendLine($"- **Sharpe Ratio:** {report.SharpeRatio:F2}");
        sb.AppendLine($"- **Sortino Ratio:** {report.SortinoRatio:F2}");
        sb.AppendLine($"- **Calmar Ratio:** {report.CalmarRatio:F2}");
        sb.AppendLine();
        sb.AppendLine("## Advanced Metrics");
        sb.AppendLine($"- **Exposure Time:** {report.ExposureTimePercent:F2}%");
        sb.AppendLine($"- **Average Holding Time:** {report.AvgHoldingTimeHours:F2} hours");
        sb.AppendLine($"- **Max Consecutive Losses:** {report.MaxConsecutiveLosses}");
        sb.AppendLine($"- **Fee Impact:** {report.FeeImpactPercent:F2}%");
        sb.AppendLine($"- **Slippage Impact:** {report.SlippageImpactPercent:F2}%");
        sb.AppendLine();
        if (report.RegimeBreakdown.Count > 0)
        {
            sb.AppendLine("## Regime Performance");
            sb.AppendLine("| Regime | Trades | Win Rate | PnL | Avg Return |");
            sb.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var regime in report.RegimeBreakdown.Values.OrderBy(r => r.Regime))
            {
                sb.AppendLine($"| {regime.Regime} | {regime.Trades} | {regime.WinRate * 100:F2}% | {regime.PnL:F2} | {regime.AvgReturn:F2}% |");
            }

            sb.AppendLine();
        }
        sb.AppendLine("## Trades");
        sb.AppendLine("| Entry Time | Exit Time | Type | Entry Price | Exit Price | Qty | PnL | Fees |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        
        foreach (var t in report.Trades)
        {
            sb.AppendLine($"| {t.EntryTime:yyyy-MM-dd HH:mm} | {t.ExitTime:yyyy-MM-dd HH:mm} | {t.Type} | {t.EntryPrice:F4} | {t.ExitPrice:F4} | {t.Quantity:F4} | {t.RealizedPnL:F2} | {t.FeesPaid:F2} |");
        }
        
        return sb.ToString();
    }
}
