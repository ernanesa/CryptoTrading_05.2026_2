namespace CryptoTrading.Domain.Entities;

public class HardeningGate
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Evidence { get; set; } = string.Empty;
}

public class BenchmarkRegistration
{
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Tool { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Status { get; set; } = "Registered";
}

public class ChaosScenarioResult
{
    public string Scenario { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string ExpectedBehavior { get; set; } = string.Empty;
    public string ObservedBehavior { get; set; } = string.Empty;
}

public class KnownRisk
{
    public string Area { get; set; } = string.Empty;
    public string Risk { get; set; } = string.Empty;
    public string Mitigation { get; set; } = string.Empty;
}

public class HardeningReport
{
    public string Version { get; set; } = "hardening-report/v1";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsReleaseCandidate { get; set; }
    public List<HardeningGate> Gates { get; set; } = new();
    public List<BenchmarkRegistration> Benchmarks { get; set; } = new();
    public List<ChaosScenarioResult> ChaosScenarios { get; set; } = new();
    public List<KnownRisk> KnownRisks { get; set; } = new();
    public List<string> Alerts { get; set; } = new();
}
