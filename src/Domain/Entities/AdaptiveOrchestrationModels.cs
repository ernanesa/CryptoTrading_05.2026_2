using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class AdaptiveOrchestrationRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public IntelligenceSnapshot Intelligence { get; set; } = new();
    public List<string> StrategyNames { get; set; } = new();
    public string? CurrentStrategyName { get; set; }
    public DateTime? LastSwitchAt { get; set; }
    public int PersistentAdvantageCycles { get; set; }
    public decimal PortfolioValue { get; set; } = 10000m;
    public RiskStatus RiskStatus { get; set; } = RiskStatus.Normal;
    public bool DataQualityPassed { get; set; } = true;
    public Dictionary<string, BacktestReport> HistoricalReports { get; set; } = new();
}

public class AdaptiveOrchestrationDecision
{
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public string MarketRegime { get; set; } = string.Empty;
    public string ActiveStrategyName { get; set; } = string.Empty;
    public string CandidateStrategyName { get; set; } = string.Empty;
    public bool ShouldSwitchStrategy { get; set; }
    public decimal StrategyScore { get; set; }
    public decimal AssetScore { get; set; }
    public decimal MarketHealthScore { get; set; }
    public decimal AllocationWeight { get; set; }
    public decimal PositionSize { get; set; }
    public ExecutionCostEstimate ExecutionCost { get; set; } = new();
    public StrategyHealthSnapshot StrategyHealth { get; set; } = new();
    public DynamicExitPolicy ExitPolicy { get; set; } = new();
    public TradeAttributionSnapshot Attribution { get; set; } = new();
    public WalkForwardEvaluation WalkForward { get; set; } = new();
    public BanditAllocation BanditAllocation { get; set; } = new();
    public List<StrategyScoreSnapshot> StrategyScores { get; set; } = new();
    public List<AssetScoreSnapshot> AssetScores { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
}

public class StrategyScoreSnapshot
{
    public string StrategyName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string RegimeFit { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class AssetScoreSnapshot
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class ExecutionCostEstimate
{
    public decimal CostBps { get; set; }
    public decimal Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class StrategyHealthSnapshot
{
    public bool IsPaused { get; set; }
    public decimal HealthScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class DynamicExitPolicy
{
    public decimal StopAtrMultiplier { get; set; }
    public decimal TakeProfitAtrMultiplier { get; set; }
    public string PolicyName { get; set; } = string.Empty;
}

public class TradeAttributionSnapshot
{
    public string PrimaryDriver { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
}

public class WalkForwardEvaluation
{
    public decimal FixedStrategyScore { get; set; }
    public decimal AdaptiveStrategyScore { get; set; }
    public decimal Improvement { get; set; }
    public string Verdict { get; set; } = string.Empty;
}

public class BanditAllocation
{
    public string SelectedArm { get; set; } = string.Empty;
    public decimal ExplorationWeight { get; set; }
    public decimal ExploitationWeight { get; set; }
}
