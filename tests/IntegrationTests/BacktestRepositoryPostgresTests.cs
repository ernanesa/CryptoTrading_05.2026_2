using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace CryptoTrading.IntegrationTests;

public sealed class BacktestRepositoryPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cryptotrading_backtest_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveReportAndGetLatest_RoundTripsAdvancedMetrics()
    {
        DatabaseMigrator.Migrate(_postgres.GetConnectionString());
        var repository = new BacktestRepository(_postgres.GetConnectionString());

        var report = new BacktestReport
        {
            StrategyName = "Deterministic Advanced",
            Symbol = "BTCUSDT",
            Interval = "1h",
            StartTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            InitialCapital = 10000m,
            FinalCapital = 10025m,
            TotalPnL = 25m,
            TotalPnLPercent = 0.25m,
            TotalTrades = 3,
            WinningTrades = 1,
            LosingTrades = 2,
            WinRate = 0.3333m,
            MaxDrawdown = 75m,
            MaxDrawdownPercent = 0.7426m,
            ProfitFactor = 1.3333m,
            Expectancy = 8.3333m,
            TotalFees = 20m,
            SharpeRatio = 0.1234m,
            SortinoRatio = 0.5678m,
            CalmarRatio = 0.3367m,
            ExposureTimePercent = 60m,
            AvgHoldingTimeHours = 2d,
            MaxConsecutiveLosses = 2,
            FeeImpactPercent = 0.2m,
            SlippageImpactPercent = 0.05m,
            RegimeBreakdown = new Dictionary<string, RegimePerformance>
            {
                ["Trending"] = new()
                {
                    Regime = "Trending",
                    Trades = 2,
                    WinRate = 0.5m,
                    PnL = 75m,
                    AvgReturn = 0.375m
                },
                ["Sideways"] = new()
                {
                    Regime = "Sideways",
                    Trades = 1,
                    WinRate = 0m,
                    PnL = -50m,
                    AvgReturn = -0.5m
                }
            },
            Trades =
            [
                new Position
                {
                    Symbol = "BTCUSDT",
                    Type = PositionType.Long,
                    EntryPrice = 50000m,
                    ExitPrice = 51000m,
                    Quantity = 0.1m,
                    EntryTime = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExitTime = new DateTime(2026, 5, 1, 2, 0, 0, DateTimeKind.Utc),
                    FeesPaid = 10m,
                    RealizedPnL = 100m,
                    Regime = "Trending"
                },
                new Position
                {
                    Symbol = "BTCUSDT",
                    Type = PositionType.Long,
                    EntryPrice = 51000m,
                    ExitPrice = 50500m,
                    Quantity = 0.1m,
                    EntryTime = new DateTime(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc),
                    ExitTime = new DateTime(2026, 5, 1, 4, 0, 0, DateTimeKind.Utc),
                    FeesPaid = 5m,
                    RealizedPnL = -50m,
                    Regime = "Sideways"
                }
            ]
        };

        await repository.SaveReportAsync(report);

        var latest = await repository.GetLatestReportAsync("Deterministic Advanced", "BTCUSDT");

        Assert.NotNull(latest);
        Assert.Equal(report.TotalPnL, latest.TotalPnL);
        Assert.Equal(report.TotalPnLPercent, latest.TotalPnLPercent);
        Assert.Equal(report.MaxDrawdown, latest.MaxDrawdown);
        Assert.Equal(report.Expectancy, latest.Expectancy);
        Assert.Equal(report.TotalFees, latest.TotalFees);
        Assert.Equal(report.SortinoRatio, latest.SortinoRatio);
        Assert.Equal(report.CalmarRatio, latest.CalmarRatio);
        Assert.Equal(report.ExposureTimePercent, latest.ExposureTimePercent);
        Assert.Equal(report.AvgHoldingTimeHours, latest.AvgHoldingTimeHours);
        Assert.Equal(report.MaxConsecutiveLosses, latest.MaxConsecutiveLosses);
        Assert.Equal(report.FeeImpactPercent, latest.FeeImpactPercent);
        Assert.Equal(report.SlippageImpactPercent, latest.SlippageImpactPercent);
        Assert.Equal(2, latest.RegimeBreakdown["Trending"].Trades);
        Assert.Equal(75m, latest.RegimeBreakdown["Trending"].PnL);
        Assert.Equal(-0.5m, latest.RegimeBreakdown["Sideways"].AvgReturn);
    }
}
