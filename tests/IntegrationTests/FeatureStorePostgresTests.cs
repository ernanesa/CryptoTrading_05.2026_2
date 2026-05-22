using CryptoTrading.Domain.Entities;
using CryptoTrading.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace CryptoTrading.IntegrationTests;

public sealed class FeatureStorePostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cryptotrading_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FeatureStorePersistsAndReadsMarketDataPoints()
    {
        var store = new FeatureStore(_postgres.GetConnectionString());
        await store.InitializeSchemaAsync();

        var openTime = DateTime.UtcNow.AddMinutes(-10).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond));
        var candle = new Candle
        {
            Symbol = "BTCUSDT",
            Interval = "1m",
            OpenTime = openTime,
            Open = 100m,
            High = 110m,
            Low = 95m,
            Close = 108m,
            Volume = 42m,
            TakerBuyVolume = 24m,
            CloseTime = openTime.AddMinutes(1)
        };

        await store.SaveCandlesAsync([candle]);
        await store.SaveFeaturesAsync([
            new CandleFeature
            {
                CandleId = candle.Id,
                Symbol = candle.Symbol,
                OpenTime = candle.OpenTime,
                Ema9 = 101m,
                Ema21 = 102m,
                Ema50 = 103m,
                Ema200 = 104m,
                Rsi14 = 55m,
                MacdValue = 1.1m,
                MacdSignal = 0.9m,
                MacdHistogram = 0.2m,
                Atr14 = 2.5m,
                BbUpper = 112m,
                BbMiddle = 104m,
                BbLower = 96m,
                Adx = 25m,
                Returns = 0.08m,
                VolumeZScore = 1.5m,
                Spread = 0.01m,
                Imbalance = 0.12m,
                CalculatedAt = DateTime.UtcNow
            }
        ]);

        var points = (await store.GetMarketDataPointsAsync(
            "BTCUSDT",
            "1m",
            openTime.AddMinutes(-1),
            openTime.AddMinutes(2))).ToList();

        Assert.Single(points);
        Assert.Equal(candle.Id, points[0].Candle.Id);
        Assert.Equal(108m, points[0].Candle.Close);
        Assert.Equal(55m, points[0].Feature.Rsi14);
        Assert.Equal(0.12m, points[0].Feature.Imbalance);
    }
}
