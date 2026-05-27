using System;
using Microsoft.Extensions.Logging.Abstractions;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using Xunit;

namespace CryptoTrading.UnitTests;

public class ModelDriftMonitorTests
{
    [Fact]
    public void RecordPrediction_ShouldNotCrash()
    {
        var monitor = new ModelDriftMonitor(NullLogger<ModelDriftMonitor>.Instance);
        
        for (int i = 0; i < 60; i++)
        {
            monitor.RecordPrediction("TestModel", 10m, 12m); // 2.0 error (below 5.0 drift limit)
        }

        Assert.NotNull(monitor);
    }
}

public class ShadowModelRunnerTests
{
    [Fact]
    public void Run_ShouldProduceAuxiliaryOutput()
    {
        var runner = new ShadowModelRunner();
        var featureVector = new IntelligenceFeatureVector { LiquidityStressScore = 30m, MomentumScore = 60m };
        var volatility = new VolatilityForecast { ForecastScore = 40m };

        var output = runner.Run(featureVector, volatility, 15m);

        Assert.True(output.IsShadowMode);
        Assert.Equal("ShadowModelRunner", output.Source);
        Assert.Contains("gated by RiskEngine", output.Explanation);
    }
}

public class IntelligenceFallbackTests
{
    [Fact]
    public void GetFallback_ShouldReturnCleanAuxiliarySnapshot()
    {
        var policy = new IntelligenceFallbackPolicy();
        var snapshot = policy.GetFallback("BTCUSDT", "1m", DateTime.UtcNow, "Unit test forced fallback");

        Assert.Equal("BTCUSDT", snapshot.Symbol);
        Assert.Equal("fallback/v1", snapshot.SchemaVersion);
        Assert.Equal("Unknown", snapshot.MarketRegime);
        Assert.NotEmpty(snapshot.Insights);
        Assert.Contains("Fallback triggered", snapshot.Insights[0]);
    }
}
