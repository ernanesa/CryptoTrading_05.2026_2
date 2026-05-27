using System.Text.Json;
using System.Text;
using CryptoTrading.Domain.Entities;
using System.Globalization;
using System.Linq;

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
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "**Period:** {0:yyyy-MM-dd} to {1:yyyy-MM-dd}", report.StartTime, report.EndTime));
        sb.AppendLine();
        sb.AppendLine("## Performance Summary");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Initial Capital:** {0:C}", report.InitialCapital));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Final Capital:** {0:C}", report.FinalCapital));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Total PnL:** {0:C} ({1:F2}%)", report.TotalPnL, report.TotalPnLPercent));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Total Fees:** {0:C}", report.TotalFees));
        sb.AppendLine();
        sb.AppendLine("## Trade Statistics");
        sb.AppendLine($"- **Total Trades:** {report.TotalTrades}");
        sb.AppendLine($"- **Winning / Losing:** {report.WinningTrades} / {report.LosingTrades}");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Win Rate:** {0:F2}%", report.WinRate * 100));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Profit Factor:** {0:F2}", report.ProfitFactor));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Expectancy:** {0:C}", report.Expectancy));
        sb.AppendLine();
        sb.AppendLine("## Risk Metrics");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Max Drawdown:** {0:C} ({1:F2}%)", report.MaxDrawdown, report.MaxDrawdownPercent));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Sharpe Ratio:** {0:F2}", report.SharpeRatio));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Sortino Ratio:** {0:F2}", report.SortinoRatio));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Calmar Ratio:** {0:F2}", report.CalmarRatio));
        sb.AppendLine();
        sb.AppendLine("## Advanced Metrics");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Exposure Time:** {0:F2}%", report.ExposureTimePercent));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Average Holding Time:** {0:F2} hours", report.AvgHoldingTimeHours));
        sb.AppendLine($"- **Max Consecutive Losses:** {report.MaxConsecutiveLosses}");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Fee Impact:** {0:F2}%", report.FeeImpactPercent));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "- **Slippage Impact:** {0:F2}%", report.SlippageImpactPercent));
        sb.AppendLine();
        if (report.RegimeBreakdown.Count > 0)
        {
            sb.AppendLine("## Regime Performance");
            sb.AppendLine("| Regime | Trades | Win Rate | PnL | Avg Return |");
            sb.AppendLine("|---|---:|---:|---:|---:|");

            foreach (var regime in report.RegimeBreakdown.Values.OrderBy(r => r.Regime))
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F2}% | {3:F2} | {4:F2}% |", regime.Regime, regime.Trades, regime.WinRate * 100, regime.PnL, regime.AvgReturn));
            }

            sb.AppendLine();
        }
        sb.AppendLine("## Trades");
        sb.AppendLine("| Entry Time | Exit Time | Type | Entry Price | Exit Price | Qty | PnL | Fees |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        
        foreach (var t in report.Trades)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0:yyyy-MM-dd HH:mm} | {1:yyyy-MM-dd HH:mm} | {2} | {3:F4} | {4:F4} | {5:F4} | {6:F2} | {7:F2} |", t.EntryTime, t.ExitTime, t.Type, t.EntryPrice, t.ExitPrice, t.Quantity, t.RealizedPnL, t.FeesPaid));
        }
        
        return sb.ToString();
    }
}
