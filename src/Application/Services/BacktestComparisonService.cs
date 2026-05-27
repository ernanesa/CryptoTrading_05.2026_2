using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class BacktestComparisonService
{
    private readonly BacktestEngine _backtestEngine;

    public BacktestComparisonService(BacktestEngine backtestEngine)
    {
        _backtestEngine = backtestEngine;
    }

    public ComparisonResult Compare(BacktestReport fixedReport, BacktestReport adaptiveReport)
    {
        var pnlImprovement = adaptiveReport.TotalPnL - fixedReport.TotalPnL;
        var pnlPercentImprovement = adaptiveReport.TotalPnLPercent - fixedReport.TotalPnLPercent;
        var maxDrawdownImprovement = fixedReport.MaxDrawdownPercent - adaptiveReport.MaxDrawdownPercent;

        var verdict = pnlImprovement > 0m 
            ? "Adaptive outperforms Fixed strategy." 
            : "Fixed strategy matches or outperforms Adaptive strategy.";

        return new ComparisonResult
        {
            FixedPnL = fixedReport.TotalPnL,
            AdaptivePnL = adaptiveReport.TotalPnL,
            PnLImprovement = pnlImprovement,
            PnLPercentImprovement = pnlPercentImprovement,
            MaxDrawdownImprovement = maxDrawdownImprovement,
            Verdict = verdict,
            Explanation = $"Fixed strategy total PnL is ${fixedReport.TotalPnL:F2} ({fixedReport.TotalPnLPercent:F2}%) vs Adaptive strategy total PnL ${adaptiveReport.TotalPnL:F2} ({adaptiveReport.TotalPnLPercent:F2}%)."
        };
    }
}

public class ComparisonResult
{
    public decimal FixedPnL { get; set; }
    public decimal AdaptivePnL { get; set; }
    public decimal PnLImprovement { get; set; }
    public decimal PnLPercentImprovement { get; set; }
    public decimal MaxDrawdownImprovement { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}
