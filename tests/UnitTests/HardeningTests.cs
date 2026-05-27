using CryptoTrading.Application.Services;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.UnitTests;

public class HardeningTests
{
    [Fact]
    public void SecretRedactor_MasksSensitiveValues()
    {
        var redactor = new SecretRedactor();

        var result = redactor.Redact("api_key=abc123 secret:super-secret token=\"jwt-value\" symbol=BTCUSDT");

        Assert.DoesNotContain("abc123", result);
        Assert.DoesNotContain("super-secret", result);
        Assert.DoesNotContain("jwt-value", result);
        Assert.Contains("***REDACTED***", result);
        Assert.Contains("symbol=BTCUSDT", result);
    }

    [Fact]
    public void ChaosScenarioRunner_AllRegisteredScenariosPass()
    {
        var runner = new ChaosScenarioRunner();

        var results = runner.Run();

        Assert.NotEmpty(results);
        Assert.All(results, result => Assert.True(result.Passed));
    }

    [Fact]
    public void HardeningReportService_GeneratesReleaseCandidateReport()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
        {
            { "Binance:Testnet:Enabled", "false" }
        }).Build();
        var runtimeStatus = new RuntimeStatusService(config);
        var service = new HardeningReportService(new ChaosScenarioRunner(), new BenchmarkCatalog(), runtimeStatus);

        var report = service.Generate();

        Assert.Equal("hardening-report/v1", report.Version);
        Assert.True(report.IsReleaseCandidate);
        Assert.All(report.Gates, gate => Assert.True(gate.Passed));
        Assert.NotEmpty(report.Benchmarks);
        Assert.NotEmpty(report.KnownRisks);
        Assert.Contains(report.KnownRisks, risk => risk.Area == "FeatureStore benchmark");
        Assert.NotEmpty(report.Alerts);
    }

    [Fact]
    public void BenchmarkCatalog_RegistersExecutableHarnessCommands()
    {
        var catalog = new BenchmarkCatalog();

        var benchmarks = catalog.Build();

        Assert.Contains(benchmarks, b => b.Name == "IndicatorService.CalculateFeatures");
        Assert.Contains(benchmarks, b => b.Name == "IndicatorService.CalculateFeatures" && b.Status == "Mandatory smoke");
        Assert.Contains(benchmarks, b => b.Name == "FeatureStore.GetMarketDataPointsAsync"
            && b.Command.Contains("--iterations 3")
            && b.Tool.Contains("Testcontainers")
            && b.Status == "Opt-in validated");
        Assert.Contains(benchmarks, b => b.Name == "ApiWorker.NativeAot.Publish" && b.Status == "Opt-in validated");
        Assert.Contains(benchmarks, b => b.Command.Contains("tools/benchmarks/CryptoTrading.Benchmarks"));
        Assert.All(benchmarks, b => Assert.False(string.IsNullOrWhiteSpace(b.Command)));
        Assert.All(benchmarks, b => Assert.False(string.IsNullOrWhiteSpace(b.Status)));
    }
}
