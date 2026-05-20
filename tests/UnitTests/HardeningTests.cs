using CryptoTrading.Application.Services;

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
        var service = new HardeningReportService(new ChaosScenarioRunner());

        var report = service.Generate();

        Assert.Equal("hardening-report/v1", report.Version);
        Assert.True(report.IsReleaseCandidate);
        Assert.All(report.Gates, gate => Assert.True(gate.Passed));
        Assert.NotEmpty(report.Benchmarks);
        Assert.NotEmpty(report.KnownRisks);
        Assert.NotEmpty(report.Alerts);
    }
}
