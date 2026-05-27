using System.IO;
using System.Text;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class BacktestReportExporter
{
    public string ExportToJson(BacktestReport report)
    {
        return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public string ExportToMarkdown(BacktestReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Relatório de Backtesting: {report.StrategyName}");
        sb.AppendLine();
        sb.AppendLine($"*   **Ativo:** {report.Symbol}");
        sb.AppendLine($"*   **Tempo Gráfico:** {report.Interval}");
        sb.AppendLine($"*   **Período:** {report.StartTime:yyyy-MM-dd HH:mm} até {report.EndTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## Métricas Principais");
        sb.AppendLine();
        sb.AppendLine($"*   **Capital Inicial:** ${report.InitialCapital:F2}");
        sb.AppendLine($"*   **Capital Final:** ${report.FinalCapital:F2}");
        sb.AppendLine($"*   **PnL Total:** ${report.TotalPnL:F2} ({report.TotalPnLPercent:F2}%)");
        sb.AppendLine($"*   **Total de Trades:** {report.TotalTrades}");
        sb.AppendLine($"*   **Taxa de Acerto (Win Rate):** {report.WinRate * 100m:F2}% (Wins: {report.WinningTrades} / Losses: {report.LosingTrades})");
        sb.AppendLine($"*   **Profit Factor:** {report.ProfitFactor:F2}");
        sb.AppendLine($"*   **Expectancy:** {report.Expectancy:F2}");
        sb.AppendLine($"*   **Taxas Pagas:** ${report.TotalFees:F2}");
        sb.AppendLine();
        sb.AppendLine("## Métricas de Risco Avançadas");
        sb.AppendLine();
        sb.AppendLine($"*   **Sharpe Ratio:** {report.SharpeRatio:F2}");
        sb.AppendLine($"*   **Sortino Ratio:** {report.SortinoRatio:F2}");
        sb.AppendLine($"*   **Calmar Ratio:** {report.CalmarRatio:F2}");
        sb.AppendLine($"*   **Max Drawdown:** ${report.MaxDrawdown:F2} ({report.MaxDrawdownPercent:F2}%)");
        sb.AppendLine($"*   **Tempo de Exposição:** {report.ExposureTimePercent:F2}%");
        sb.AppendLine($"*   **Tempo Médio de Custódia:** {report.AvgHoldingTimeHours:F2} horas");
        sb.AppendLine($"*   **Máximo de Derrotas Consecutivas:** {report.MaxConsecutiveLosses}");
        sb.AppendLine($"*   **Impacto de Taxas:** {report.FeeImpactPercent:F2}%");
        sb.AppendLine($"*   **Impacto de Slippage:** {report.SlippageImpactPercent:F2}%");
        sb.AppendLine();
        sb.AppendLine("## Performance por Regime de Mercado");
        sb.AppendLine();
        sb.AppendLine("| Regime | Qtd Trades | Win Rate | PnL Total | Retorno Médio |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: |");
        foreach (var regime in report.RegimeBreakdown.Values)
        {
            sb.AppendLine($"| {regime.Regime} | {regime.Trades} | {regime.WinRate * 100m:F2}% | ${regime.PnL:F2} | {regime.AvgReturn:F2}% |");
        }

        return sb.ToString();
    }
}
