using System;
using System.IO;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using Xunit;

namespace CryptoTrading.UnitTests;

public class BacktestReportRoundtripTests
{
    [Fact]
    public void ExportToJsonAndMarkdown_ShouldProduceValidStrings()
    {
        var report = new BacktestReport
        {
            StrategyName = "TestStrategy",
            Symbol = "BTCUSDT",
            Interval = "1m",
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow,
            InitialCapital = 10000m,
            FinalCapital = 10500m,
            TotalPnL = 500m,
            TotalPnLPercent = 5m,
            RegimeBreakdown = new System.Collections.Generic.Dictionary<string, RegimePerformance>
            {
                { "TrendingUp", new RegimePerformance { Regime = "TrendingUp", Trades = 5, WinRate = 0.8m, PnL = 400m, AvgReturn = 4m } }
            }
        };

        var exporter = new BacktestReportExporter();
        var json = exporter.ExportToJson(report);
        var markdown = exporter.ExportToMarkdown(report);

        Assert.NotEmpty(json);
        Assert.Contains("TestStrategy", json);
        Assert.NotEmpty(markdown);
        Assert.Contains("Relatório de Backtesting", markdown);
    }
}
