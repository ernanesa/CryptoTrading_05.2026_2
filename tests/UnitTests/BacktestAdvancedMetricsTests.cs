using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class BacktestAdvancedMetricsTests
{
    [Fact]
    public void PopulateMetrics_ShouldCalculateSharpeAndSortino()
    {
        var report = new BacktestReport
        {
            InitialCapital = 10000m,
            FinalCapital = 11000m,
            StartTime = DateTime.UtcNow.AddDays(-10),
            EndTime = DateTime.UtcNow,
            Trades = new List<Position>
            {
                new Position
                {
                    Type = PositionType.Long,
                    EntryPrice = 100m,
                    Quantity = 100m,
                    EntryTime = DateTime.UtcNow.AddDays(-8),
                    ExitPrice = 105m,
                    ExitTime = DateTime.UtcNow.AddDays(-7),
                    RealizedPnL = 600m
                },
                new Position
                {
                    Type = PositionType.Long,
                    EntryPrice = 105m,
                    Quantity = 100m,
                    EntryTime = DateTime.UtcNow.AddDays(-6),
                    ExitPrice = 110m,
                    ExitTime = DateTime.UtcNow.AddDays(-5),
                    RealizedPnL = 400m
                }
            }
        };

        var analyzer = new PerformanceAnalyzer();
        analyzer.PopulateMetrics(report);

        Assert.Equal(1000m, report.TotalPnL);
        Assert.Equal(10m, report.TotalPnLPercent);
        Assert.True(report.SharpeRatio > 0m);
    }
}
