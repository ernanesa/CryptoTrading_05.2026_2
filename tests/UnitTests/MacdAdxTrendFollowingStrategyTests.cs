using System;
using System.Collections.Generic;
using CryptoTrading.Application.Services;
using CryptoTrading.Application.Strategies;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class MacdAdxTrendFollowingStrategyTests
{
    [Fact]
    public void Name_ShouldBeCorrect()
    {
        var strategy = new MacdAdxTrendFollowingStrategy();
        Assert.Equal("MACD ADX Trend Following", strategy.Name);
    }

    [Fact]
    public void GenerateSignal_ShouldReturnHold_WhenWarmupPeriodActive()
    {
        var strategy = new MacdAdxTrendFollowingStrategy();
        
        var current = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow },
            Feature = new CandleFeature { MacdValue = 0m, MacdSignal = 0m, Adx = 0m }
        };

        var history = new List<MarketDataPoint>
        {
            new()
            {
                Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow.AddMinutes(-15) },
                Feature = new CandleFeature { MacdValue = 0m, MacdSignal = 0m, Adx = 0m }
            }
        };

        var signal = strategy.GenerateSignal(current, history);

        Assert.Equal(TradeSignalType.Hold, signal.Type);
        Assert.Contains("sinal", signal.Description);
    }

    [Fact]
    public void GenerateSignal_ShouldReturnBuy_WhenMacdCrossesUpAndAdxStrong()
    {
        var strategy = new MacdAdxTrendFollowingStrategy();

        var prev = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow.AddMinutes(-15) },
            Feature = new CandleFeature { MacdValue = 1.2m, MacdSignal = 1.3m, Adx = 30m } // MACD abaixo do Signal
        };

        var current = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow },
            Feature = new CandleFeature { MacdValue = 1.5m, MacdSignal = 1.4m, Adx = 30m } // MACD cruzou acima do Signal
        };

        var history = new List<MarketDataPoint> { prev };

        var signal = strategy.GenerateSignal(current, history);

        Assert.Equal(TradeSignalType.Buy, signal.Type);
        Assert.Contains("cruzou acima", signal.Description);
        Assert.Contains("ADX forte", signal.Description);
    }

    [Fact]
    public void GenerateSignal_ShouldReturnHold_WhenMacdCrossesUpButAdxWeak()
    {
        var strategy = new MacdAdxTrendFollowingStrategy();

        var prev = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow.AddMinutes(-15) },
            Feature = new CandleFeature { MacdValue = 1.2m, MacdSignal = 1.3m, Adx = 20m } // MACD abaixo do Signal, ADX fraco
        };

        var current = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow },
            Feature = new CandleFeature { MacdValue = 1.5m, MacdSignal = 1.4m, Adx = 20m } // MACD cruzou acima, mas ADX fraco
        };

        var history = new List<MarketDataPoint> { prev };

        var signal = strategy.GenerateSignal(current, history);

        Assert.Equal(TradeSignalType.Hold, signal.Type);
    }

    [Fact]
    public void GenerateSignal_ShouldReturnExit_WhenMacdCrossesDown()
    {
        var strategy = new MacdAdxTrendFollowingStrategy();

        var prev = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow.AddMinutes(-15) },
            Feature = new CandleFeature { MacdValue = 1.5m, MacdSignal = 1.4m, Adx = 30m } // MACD acima do Signal
        };

        var current = new MarketDataPoint
        {
            Candle = new Candle { Symbol = "BTCUSDT", OpenTime = DateTime.UtcNow },
            Feature = new CandleFeature { MacdValue = 1.2m, MacdSignal = 1.3m, Adx = 30m } // MACD cruzou abaixo do Signal
        };

        var history = new List<MarketDataPoint> { prev };

        var signal = strategy.GenerateSignal(current, history);

        Assert.Equal(TradeSignalType.Exit, signal.Type);
        Assert.Contains("cruzou abaixo", signal.Description);
    }

    [Fact]
    public void Registry_ShouldContainNewStrategy()
    {
        var registry = new StrategyRegistry();
        var strategy = registry.Get("MACD ADX Trend Following");

        Assert.NotNull(strategy);
        Assert.IsType<MacdAdxTrendFollowingStrategy>(strategy);
    }
}
