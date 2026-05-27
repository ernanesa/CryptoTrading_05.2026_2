using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class BacktestReplayTests
{
    private class FakeStrategy : IStrategy
    {
        public string Name => "Fake";
        public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
        {
            return new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Buy };
        }
    }

    [Fact]
    public void Replay_ShouldExecuteSequentially()
    {
        var engine = new BacktestEngine();
        var replayService = new BacktestReplayService(engine);

        var dataset = new List<MarketDataPoint>
        {
            new MarketDataPoint { Candle = new Candle { Symbol = "BTCUSDT", Interval = "1m", OpenTime = DateTime.UtcNow.AddMinutes(-2), Close = 50000m }, Feature = new CandleFeature { Spread = 10m } },
            new MarketDataPoint { Candle = new Candle { Symbol = "BTCUSDT", Interval = "1m", OpenTime = DateTime.UtcNow.AddMinutes(-1), Close = 51000m }, Feature = new CandleFeature { Spread = 10m } }
        };

        var feeModel = new MakerTakerFeeModel(0.001m, 0.001m);
        var slippageModel = new VolumeBasedSlippageModel(0.0005m, 0.0001m);

        var report = replayService.Replay(new FakeStrategy(), dataset, 10000m, feeModel, slippageModel);

        Assert.Equal("Fake", report.StrategyName);
        Assert.Equal(1, report.TotalTrades);
    }
}
