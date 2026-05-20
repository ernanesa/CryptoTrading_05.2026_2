using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.UnitTests;

public class IndicatorServiceTests
{
    private readonly IndicatorService _service = new();

    private static List<Candle> CreateSampleCandles(int count = 250)
    {
        var candles = new List<Candle>();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var random = new Random(42); // Seed para resultados reprodutíveis

        var price = 68000m;

        for (var i = 0; i < count; i++)
        {
            var change = (decimal)(random.NextDouble() * 200 - 100); // +-100
            var open = price;
            var close = open + change;
            var high = Math.Max(open, close) + (decimal)(random.NextDouble() * 50);
            var low = Math.Min(open, close) - (decimal)(random.NextDouble() * 50);
            var volume = 1000m + (decimal)(random.NextDouble() * 5000);
            var takerBuyVolume = volume * (decimal)(0.4 + random.NextDouble() * 0.2); // ~40% a ~60%

            candles.Add(new Candle
            {
                Id = i + 1,
                Symbol = "BTCUSDT",
                Interval = "1m",
                OpenTime = baseTime.AddMinutes(i),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                TakerBuyVolume = takerBuyVolume,
                CloseTime = baseTime.AddMinutes(i).AddSeconds(59)
            });

            price = close;
        }

        return candles;
    }

    [Fact]
    public void CalculateFeatures_EmptyList_ReturnsEmptyList()
    {
        var features = _service.CalculateFeatures(new List<Candle>());
        Assert.Empty(features);
    }

    [Fact]
    public void CalculateFeatures_NullList_ReturnsEmptyList()
    {
        var features = _service.CalculateFeatures(null!);
        Assert.Empty(features);
    }

    [Fact]
    public void CalculateFeatures_ValidCandles_ReturnsSameCount()
    {
        var candles = CreateSampleCandles(250);
        var features = _service.CalculateFeatures(candles);

        Assert.Equal(candles.Count, features.Count);
    }

    [Fact]
    public void CalculateFeatures_SufficientData_ProducesNonZeroEma9()
    {
        var candles = CreateSampleCandles(250);
        var features = _service.CalculateFeatures(candles);

        // A EMA 9 precisa de pelo menos 9 candles. O 250o candle deve ter valor não-zero.
        var lastFeature = features.Last();
        Assert.NotEqual(0m, lastFeature.Ema9);
    }

    [Fact]
    public void CalculateFeatures_SufficientData_ProducesNonZeroRsi()
    {
        var candles = CreateSampleCandles(250);
        var features = _service.CalculateFeatures(candles);

        var lastFeature = features.Last();
        Assert.NotEqual(0m, lastFeature.Rsi14);
    }

    [Fact]
    public void CalculateFeatures_SufficientData_ProducesNonZeroMacd()
    {
        var candles = CreateSampleCandles(250);
        var features = _service.CalculateFeatures(candles);

        var lastFeature = features.Last();
        // MACD pode ser negativo, mas com 250 candles ele não deve ser exatamente zero
        Assert.NotEqual(0m, lastFeature.MacdValue);
    }

    [Fact]
    public void CalculateFeatures_SufficientData_ProducesNonZeroBollingerBands()
    {
        var candles = CreateSampleCandles(250);
        var features = _service.CalculateFeatures(candles);

        var lastFeature = features.Last();
        Assert.NotEqual(0m, lastFeature.BbUpper);
        Assert.NotEqual(0m, lastFeature.BbMiddle);
        Assert.NotEqual(0m, lastFeature.BbLower);
        Assert.True(lastFeature.BbUpper > lastFeature.BbMiddle, "Bollinger Upper deve ser > Middle");
        Assert.True(lastFeature.BbMiddle > lastFeature.BbLower, "Bollinger Middle deve ser > Lower");
    }

    [Fact]
    public void CalculateFeatures_PreservesSymbolAndOpenTime()
    {
        var candles = CreateSampleCandles(50);
        var features = _service.CalculateFeatures(candles);

        for (var i = 0; i < candles.Count; i++)
        {
            Assert.Equal(candles[i].Symbol, features[i].Symbol);
            Assert.Equal(candles[i].OpenTime, features[i].OpenTime);
        }
    }

    [Fact]
    public void CalculateFeatures_ValidCandles_ProducesNewCustomFeatures()
    {
        var candles = CreateSampleCandles(50);
        var features = _service.CalculateFeatures(candles);

        // O primeiro candle não tem retorno (0m), mas a partir do segundo deve ter retorno
        Assert.Equal(0m, features.First().Returns);
        Assert.NotEqual(0m, features[1].Returns);

        // Todos os candles devem ter spread positivo
        Assert.All(features, f => Assert.True(f.Spread > 0m));

        // Z-score de volume deve ser calculado
        Assert.NotEqual(0m, features.Last().VolumeZScore);

        // Imbalance deve estar entre -1 e 1
        Assert.All(features, f => Assert.True(f.Imbalance >= -1m && f.Imbalance <= 1m));
    }
}
