using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Services;

namespace CryptoTrading.UnitTests;

public class DataQualityGateTests
{
    private readonly DataQualityGate _gate = new();

    private static Candle CreateValidCandle() => new()
    {
        Symbol = "BTCUSDT",
        Interval = "1m",
        OpenTime = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc),
        Open = 68000m,
        High = 68500m,
        Low = 67800m,
        Close = 68200m,
        Volume = 1500m,
        CloseTime = new DateTime(2026, 5, 20, 12, 0, 59, DateTimeKind.Utc)
    };

    [Fact]
    public void Validate_ValidCandle_ReturnsTrue()
    {
        var candle = CreateValidCandle();
        var result = _gate.Validate(candle, out var errors);

        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HighLessThanLow_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.High = 67000m; // High < Low (67800)

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Inconsistência crítica"));
    }

    [Fact]
    public void Validate_NegativeVolume_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.Volume = -100m;

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Volume"));
    }

    [Fact]
    public void Validate_ZeroPrices_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.Open = 0m;

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Preço inválido"));
    }

    [Fact]
    public void Validate_EmptySymbol_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.Symbol = "";

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Símbolo"));
    }

    [Fact]
    public void Validate_OpenTimeAfterCloseTime_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.OpenTime = candle.CloseTime.AddMinutes(1);

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Timestamp"));
    }

    [Fact]
    public void Validate_HighLessThanOpen_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.High = 67900m; // High < Open (68000)

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Máxima inconsistente"));
    }

    [Fact]
    public void Validate_LowGreaterThanClose_ReturnsFalse()
    {
        var candle = CreateValidCandle();
        candle.Low = 68300m; // Low > Close (68200)

        var result = _gate.Validate(candle, out var errors);

        Assert.False(result);
        Assert.Contains(errors, e => e.Contains("Mínima inconsistente"));
    }
}
