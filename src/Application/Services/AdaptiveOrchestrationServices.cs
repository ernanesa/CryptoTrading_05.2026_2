using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class MarketRegimeService
{
    public string Resolve(IntelligenceSnapshot intelligence) => intelligence.MarketRegime;
}

public class MarketHealthScore
{
    public decimal Calculate(IntelligenceSnapshot intelligence, bool dataQualityPassed)
    {
        if (!dataQualityPassed)
        {
            return 0m;
        }

        var score = 100m
            - (intelligence.VolatilityForecast.ForecastScore * 0.35m)
            - (intelligence.SentimentRisk.RiskScore * 0.25m)
            - (intelligence.EventRisk.EventRiskScore * 0.25m)
            - (intelligence.AnomalyScore * 0.15m);

        return RoundScore(score);
    }

    private static decimal RoundScore(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

public class ExecutionCostModel
{
    public ExecutionCostEstimate Estimate(IntelligenceSnapshot intelligence)
    {
        var costBps = 2m
            + (intelligence.FeatureVector.LiquidityStressScore * 0.08m)
            + (intelligence.VolatilityForecast.ForecastScore * 0.04m);

        return new ExecutionCostEstimate
        {
            CostBps = Math.Round(costBps, 2),
            Score = Math.Round(Math.Clamp(100m - (costBps * 4m), 0m, 100m), 2),
            Explanation = $"Estimated execution cost {costBps:F2} bps from liquidity and volatility context."
        };
    }
}

public class AssetRankingService
{
    public AssetScoreSnapshot Rank(string symbol, IntelligenceSnapshot intelligence, ExecutionCostEstimate cost)
    {
        var liquidityScore = 100m - intelligence.FeatureVector.LiquidityStressScore;
        var spreadScore = cost.Score;
        var volatilityScore = 100m - intelligence.VolatilityForecast.ForecastScore;
        var trendScore = intelligence.FeatureVector.TrendScore;
        var momentumScore = intelligence.FeatureVector.MomentumScore;
        var strategyFitScore = intelligence.MetaLabel.QualityScore;
        var correlationPenalty = 95m;
        var riskPenalty = 100m - intelligence.SentimentRisk.RiskScore;

        var score = (liquidityScore * 0.20m)
            + (spreadScore * 0.15m)
            + (volatilityScore * 0.15m)
            + (trendScore * 0.15m)
            + (momentumScore * 0.15m)
            + (strategyFitScore * 0.10m)
            + (correlationPenalty * 0.05m)
            + (riskPenalty * 0.05m);

        return new AssetScoreSnapshot
        {
            Symbol = symbol.ToUpperInvariant(),
            Score = RoundScore(score),
            Explanation = "Liquidity, spread, volatility, trend, momentum, strategy fit and risk penalty combined."
        };
    }

    private static decimal RoundScore(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

public class StrategyPerformanceTracker
{
    public decimal EstimateExpectancyScore(string strategyName, IntelligenceSnapshot intelligence, AdaptiveOrchestrationRequest request)
    {
        var baseScore = strategyName switch
        {
            "EMA Trend Following" => intelligence.FeatureVector.TrendScore,
            "MACD ADX Trend Following" => (intelligence.FeatureVector.TrendScore + intelligence.FeatureVector.MomentumScore) / 2m,
            "ATR Breakout" => (intelligence.FeatureVector.TrendScore + intelligence.VolatilityForecast.ForecastScore) / 2m,
            "RSI Mean Reversion" => 100m - Math.Abs(intelligence.FeatureVector.MomentumScore - 50m) * 2m,
            "Bollinger Mean Reversion" => 100m - intelligence.FeatureVector.TrendScore,
            _ => 50m
        };

        if (request.HistoricalReports.TryGetValue(strategyName, out var report))
        {
            // Histórico compõe 50% do score se existir
            var historicalScore = Math.Clamp((report.WinRate * 100m) + (report.SharpeRatio * 10m), 0m, 100m);
            baseScore = (baseScore * 0.5m) + (historicalScore * 0.5m);
        }

        return RoundScore(baseScore);
    }

    private static decimal RoundScore(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

public class StrategyScoringService
{
    private readonly StrategyPerformanceTracker _performanceTracker;

    public StrategyScoringService(StrategyPerformanceTracker performanceTracker)
    {
        _performanceTracker = performanceTracker;
    }

    public StrategyScoreSnapshot Score(string strategyName, AdaptiveOrchestrationRequest request, ExecutionCostEstimate cost)
    {
        var intelligence = request.Intelligence;
        var regimeFitScore = CalculateRegimeFit(strategyName, intelligence.MarketRegime);
        var expectancyScore = _performanceTracker.EstimateExpectancyScore(strategyName, intelligence, request);
        var profitFactorScore = Math.Clamp(expectancyScore + 8m, 0m, 100m);
        var drawdownScore = 100m - intelligence.SentimentRisk.RiskScore;
        var signalQualityScore = intelligence.MetaLabel.QualityScore;
        var stabilityScore = 100m - intelligence.AnomalyScore;

        var score = (regimeFitScore * 0.25m)
            + (expectancyScore * 0.20m)
            + (profitFactorScore * 0.15m)
            + (drawdownScore * 0.15m)
            + (cost.Score * 0.10m)
            + (signalQualityScore * 0.10m)
            + (stabilityScore * 0.05m);

        return new StrategyScoreSnapshot
        {
            StrategyName = strategyName,
            Score = RoundScore(score),
            RegimeFit = $"{intelligence.MarketRegime}:{regimeFitScore:F2}",
            Explanation = "Regime fit, expectancy, cost, signal quality and stability combined."
        };
    }

    private static decimal CalculateRegimeFit(string strategyName, string regime)
    {
        return (strategyName, regime) switch
        {
            ("EMA Trend Following", "TrendingUp" or "TrendingDown") => 95m,
            ("MACD ADX Trend Following", "TrendingUp" or "TrendingDown") => 98m,
            ("ATR Breakout", "HighVolatility" or "TrendingUp" or "TrendingDown") => 88m,
            ("RSI Mean Reversion", "Sideways") => 86m,
            ("Bollinger Mean Reversion", "Sideways") => 90m,
            (_, "Unknown") => 35m,
            _ => 55m
        };
    }

    private static decimal RoundScore(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

public class StrategyHealthMonitor
{
    public StrategyHealthSnapshot Evaluate(StrategyScoreSnapshot score, IntelligenceSnapshot intelligence)
    {
        var risk = Math.Max(intelligence.SentimentRisk.RiskScore, intelligence.EventRisk.EventRiskScore);
        var health = Math.Clamp((score.Score * 0.70m) + ((100m - risk) * 0.30m), 0m, 100m);
        var paused = health < 45m || intelligence.SentimentRisk.RiskBand == "RiskOff";

        return new StrategyHealthSnapshot
        {
            IsPaused = paused,
            HealthScore = Math.Round(health, 2),
            Reason = paused ? "Strategy paused by health monitor due to weak score or RiskOff context." : "Strategy health is acceptable."
        };
    }
}

public class MultiArmedBanditAllocator
{
    public BanditAllocation Select(IReadOnlyList<StrategyScoreSnapshot> scores, AdaptiveOrchestrationRequest request)
    {
        var best = scores.OrderByDescending(s => s.Score).First();
        
        // Se temos histórico, podemos ajustar o exploration weight
        decimal explorationWeight = 0.20m; // Base epsilon
        
        if (request.HistoricalReports.TryGetValue(best.StrategyName, out var report))
        {
            // Se o win rate for alto e sharpe for bom, menos exploração (mais exploitation)
            if (report.WinRate > 0.55m && report.SharpeRatio > 1.5m)
            {
                explorationWeight = 0.05m;
            }
            // Se a estratégia está performando mal historicamente, forçamos mais exploração
            else if (report.WinRate < 0.40m || report.MaxDrawdownPercent > 15m)
            {
                explorationWeight = 0.40m;
            }
        }
        else
        {
            // Estratégia sem histórico ganha peso maior de exploração inicial
            explorationWeight = 0.30m;
        }

        var exploitationWeight = 1m - explorationWeight;

        return new BanditAllocation
        {
            SelectedArm = best.StrategyName,
            ExploitationWeight = Math.Round(exploitationWeight, 2),
            ExplorationWeight = Math.Round(explorationWeight, 2)
        };
    }
}

public class AdaptivePortfolioAllocator
{
    public decimal Allocate(decimal assetScore, decimal strategyScore, decimal marketHealthScore)
    {
        var raw = ((assetScore * 0.35m) + (strategyScore * 0.40m) + (marketHealthScore * 0.25m)) / 100m;
        return Math.Round(Math.Clamp(raw, 0m, 0.98m), 4);
    }
}

public class DynamicPositionSizingService
{
    public decimal Calculate(decimal portfolioValue, decimal allocationWeight, RiskStatus riskStatus, StrategyHealthSnapshot health)
    {
        if (riskStatus == RiskStatus.Halted || health.IsPaused)
        {
            return 0m;
        }

        var riskMultiplier = riskStatus == RiskStatus.Warning ? 0.50m : 1m;
        return Math.Round(portfolioValue * allocationWeight * riskMultiplier, 2);
    }
}

public class DynamicExitEngine
{
    public DynamicExitPolicy Build(IntelligenceSnapshot intelligence)
    {
        var highRisk = intelligence.VolatilityForecast.RiskBand == "Elevated" || intelligence.SentimentRisk.RiskBand == "RiskOff";
        return new DynamicExitPolicy
        {
            PolicyName = highRisk ? "ProtectiveAtrExit" : "BalancedAtrExit",
            StopAtrMultiplier = highRisk ? 1.2m : 2.0m,
            TakeProfitAtrMultiplier = highRisk ? 2.0m : 3.2m
        };
    }
}

public class TradeAttributionService
{
    public TradeAttributionSnapshot Attribute(StrategyScoreSnapshot strategy, AssetScoreSnapshot asset, IntelligenceSnapshot intelligence)
    {
        return new TradeAttributionSnapshot
        {
            PrimaryDriver = strategy.Score >= asset.Score ? "StrategyScore" : "AssetScore",
            Factors = new List<string>
            {
                $"Strategy {strategy.StrategyName} scored {strategy.Score:F2}.",
                $"Asset {asset.Symbol} scored {asset.Score:F2}.",
                $"Regime {intelligence.MarketRegime} and risk {intelligence.SentimentRisk.RiskBand}."
            }
        };
    }
}

public class WalkForwardEvaluator
{
    public WalkForwardEvaluation Compare(StrategyScoreSnapshot fixedStrategy, StrategyScoreSnapshot adaptiveStrategy)
    {
        var improvement = adaptiveStrategy.Score - fixedStrategy.Score;
        return new WalkForwardEvaluation
        {
            FixedStrategyScore = fixedStrategy.Score,
            AdaptiveStrategyScore = adaptiveStrategy.Score,
            Improvement = Math.Round(improvement, 2),
            Verdict = improvement >= 5m ? "AdaptivePreferred" : "FixedComparable"
        };
    }
}

public class AdaptiveStrategyOrchestrator
{
    private static readonly TimeSpan SwitchCooldown = TimeSpan.FromMinutes(30);
    private const decimal MinimumSwitchAdvantage = 7m;
    private const int RequiredAdvantageCycles = 2;

    private readonly MarketRegimeService _regimeService;
    private readonly AssetRankingService _assetRanking;
    private readonly StrategyScoringService _strategyScoring;
    private readonly AdaptivePortfolioAllocator _portfolioAllocator;
    private readonly DynamicPositionSizingService _positionSizing;
    private readonly DynamicExitEngine _exitEngine;
    private readonly ExecutionCostModel _costModel;
    private readonly StrategyHealthMonitor _healthMonitor;
    private readonly TradeAttributionService _attribution;
    private readonly WalkForwardEvaluator _walkForward;
    private readonly MultiArmedBanditAllocator _bandit;
    private readonly MarketHealthScore _marketHealth;

    public AdaptiveStrategyOrchestrator(
        MarketRegimeService regimeService,
        AssetRankingService assetRanking,
        StrategyScoringService strategyScoring,
        AdaptivePortfolioAllocator portfolioAllocator,
        DynamicPositionSizingService positionSizing,
        DynamicExitEngine exitEngine,
        ExecutionCostModel costModel,
        StrategyHealthMonitor healthMonitor,
        TradeAttributionService attribution,
        WalkForwardEvaluator walkForward,
        MultiArmedBanditAllocator bandit,
        MarketHealthScore marketHealth)
    {
        _regimeService = regimeService;
        _assetRanking = assetRanking;
        _strategyScoring = strategyScoring;
        _portfolioAllocator = portfolioAllocator;
        _positionSizing = positionSizing;
        _exitEngine = exitEngine;
        _costModel = costModel;
        _healthMonitor = healthMonitor;
        _attribution = attribution;
        _walkForward = walkForward;
        _bandit = bandit;
        _marketHealth = marketHealth;
    }

    public AdaptiveOrchestrationDecision Decide(AdaptiveOrchestrationRequest request)
    {
        if (request.StrategyNames.Count == 0)
        {
            throw new ArgumentException("At least one strategy is required.", nameof(request));
        }

        var regime = _regimeService.Resolve(request.Intelligence);
        var marketHealthScore = _marketHealth.Calculate(request.Intelligence, request.DataQualityPassed);
        var cost = _costModel.Estimate(request.Intelligence);
        var asset = _assetRanking.Rank(request.Symbol, request.Intelligence, cost);
        var strategyScores = request.StrategyNames
            .Select(name => _strategyScoring.Score(name, request, cost))
            .OrderByDescending(score => score.Score)
            .ToList();

        var candidate = strategyScores.First();
        var active = ResolveActiveStrategyScore(strategyScores, request.CurrentStrategyName);
        var candidateHealth = _healthMonitor.Evaluate(candidate, request.Intelligence);
        var shouldSwitch = ShouldSwitch(active, candidate, request, candidateHealth);
        var selected = shouldSwitch ? candidate : active;
        var selectedHealth = _healthMonitor.Evaluate(selected, request.Intelligence);
        var bandit = _bandit.Select(strategyScores, request);
        var allocation = _portfolioAllocator.Allocate(asset.Score, selected.Score, marketHealthScore);
        var positionSize = request.DataQualityPassed
            ? _positionSizing.Calculate(request.PortfolioValue, allocation, request.RiskStatus, selectedHealth)
            : 0m;
        var exitPolicy = _exitEngine.Build(request.Intelligence);

        return new AdaptiveOrchestrationDecision
        {
            Symbol = request.Symbol.ToUpperInvariant(),
            Interval = request.Interval,
            MarketRegime = regime,
            ActiveStrategyName = selected.StrategyName,
            CandidateStrategyName = candidate.StrategyName,
            ShouldSwitchStrategy = shouldSwitch,
            StrategyScore = selected.Score,
            AssetScore = asset.Score,
            MarketHealthScore = marketHealthScore,
            AllocationWeight = allocation,
            PositionSize = positionSize,
            ExecutionCost = cost,
            StrategyHealth = selectedHealth,
            ExitPolicy = exitPolicy,
            Attribution = _attribution.Attribute(selected, asset, request.Intelligence),
            WalkForward = _walkForward.Compare(active, candidate),
            BanditAllocation = bandit,
            StrategyScores = strategyScores,
            AssetScores = new List<AssetScoreSnapshot> { asset },
            Reasons = BuildReasons(request, active, candidate, shouldSwitch, selectedHealth, marketHealthScore)
        };
    }

    private static StrategyScoreSnapshot ResolveActiveStrategyScore(
        IReadOnlyList<StrategyScoreSnapshot> scores,
        string? currentStrategyName)
    {
        return scores.FirstOrDefault(s => s.StrategyName.Equals(currentStrategyName, StringComparison.OrdinalIgnoreCase))
            ?? scores.Last();
    }

    private static bool ShouldSwitch(
        StrategyScoreSnapshot active,
        StrategyScoreSnapshot candidate,
        AdaptiveOrchestrationRequest request,
        StrategyHealthSnapshot health)
    {
        if (!request.DataQualityPassed || request.RiskStatus == RiskStatus.Halted || health.IsPaused)
        {
            return false;
        }

        var hasScoreAdvantage = candidate.Score >= active.Score + MinimumSwitchAdvantage;
        var hasPersistence = request.PersistentAdvantageCycles >= RequiredAdvantageCycles;
        var cooldownPassed = request.LastSwitchAt == null || DateTime.UtcNow - request.LastSwitchAt.Value >= SwitchCooldown;

        return hasScoreAdvantage && hasPersistence && cooldownPassed;
    }

    private static List<string> BuildReasons(
        AdaptiveOrchestrationRequest request,
        StrategyScoreSnapshot active,
        StrategyScoreSnapshot candidate,
        bool shouldSwitch,
        StrategyHealthSnapshot health,
        decimal marketHealthScore)
    {
        return new List<string>
        {
            $"Candidate {candidate.StrategyName} scored {candidate.Score:F2}; active baseline {active.StrategyName} scored {active.Score:F2}.",
            $"Switch decision: {(shouldSwitch ? "approved by hysteresis/cooldown" : "held by hysteresis/cooldown/risk gates")}.",
            $"DataQualityGate: {(request.DataQualityPassed ? "OK" : "BLOCKED")}; market health {marketHealthScore:F2}.",
            health.Reason
        };
    }
}
