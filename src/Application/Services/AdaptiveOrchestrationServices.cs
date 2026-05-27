using CryptoTrading.Contracts.Interfaces;
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
    public AssetScoreSnapshot Rank(string symbol, IntelligenceSnapshot intelligence, ExecutionCostEstimate cost, AdaptiveOrchestrationRequest request)
    {
        var baseScore = (100m - intelligence.FeatureVector.LiquidityStressScore) * 0.20m
            + cost.Score * 0.15m
            + (100m - intelligence.VolatilityForecast.ForecastScore) * 0.15m
            + intelligence.FeatureVector.TrendScore * 0.15m
            + intelligence.FeatureVector.MomentumScore * 0.15m
            + intelligence.MetaLabel.QualityScore * 0.10m
            + 95m * 0.05m
            + (100m - intelligence.SentimentRisk.RiskScore) * 0.05m;

        var bestMetric = request.RealMetrics.Values.Where(m => m.Symbol == symbol).OrderByDescending(m => m.ProfitFactor).FirstOrDefault();
        if (bestMetric != null) baseScore += System.Math.Clamp(bestMetric.ProfitFactor * 5m, 0m, 15m);

        return new AssetScoreSnapshot
        {
            Symbol = symbol.ToUpperInvariant(),
            Score = System.Math.Round(System.Math.Clamp(baseScore, 0m, 100m), 2),
            Explanation = "Ranked with real metrics boost."
        };
    }
}

public class StrategyPerformanceTracker
{
    public decimal EstimateExpectancyScore(string strategyName, IntelligenceSnapshot intelligence, AdaptiveOrchestrationRequest request)
    {
        var baseScore = 50m;
        if (request.RealMetrics.TryGetValue(strategyName, out var realMetric))
        {
            baseScore = System.Math.Clamp((realMetric.WinRate * 100m) + (realMetric.ProfitFactor * 10m) - (realMetric.MaxDrawdown * 2m) - (realMetric.ConsecutiveLosses * 5m), 0m, 100m);
        }
        else if (request.HistoricalReports.TryGetValue(strategyName, out var report))
        {
            baseScore = System.Math.Clamp((report.WinRate * 100m) + (report.SharpeRatio * 10m), 0m, 100m);
        }
        else
        {
            baseScore = strategyName switch
            {
                "EMA Trend Following" => intelligence.FeatureVector.TrendScore,
                "MACD ADX Trend Following" => (intelligence.FeatureVector.TrendScore + intelligence.FeatureVector.MomentumScore) / 2m,
                "ATR Breakout" => (intelligence.FeatureVector.TrendScore + intelligence.VolatilityForecast.ForecastScore) / 2m,
                "RSI Mean Reversion" => 100m - System.Math.Abs(intelligence.FeatureVector.MomentumScore - 50m) * 2m,
                "Bollinger Mean Reversion" => 100m - intelligence.FeatureVector.TrendScore,
                _ => 50m
            };
        }
        return System.Math.Round(System.Math.Clamp(baseScore, 0m, 100m), 2);
    }
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
            RegimeFitScore = RoundScore(regimeFitScore),
            ExpectancyScore = RoundScore(expectancyScore),
            ProfitFactorScore = RoundScore(profitFactorScore),
            DrawdownScore = RoundScore(drawdownScore),
            ExecutionCostScore = RoundScore(cost.Score),
            SignalQualityScore = RoundScore(signalQualityScore),
            StabilityScore = RoundScore(stabilityScore),
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
    public StrategyHealthSnapshot Evaluate(StrategyScoreSnapshot score, AdaptiveOrchestrationRequest request)
    {
        var intelligence = request.Intelligence;
        var risk = System.Math.Max(intelligence.SentimentRisk.RiskScore, intelligence.EventRisk.EventRiskScore);
        var health = System.Math.Clamp((score.Score * 0.70m) + ((100m - risk) * 0.30m), 0m, 100m);
        var paused = health < 45m || intelligence.SentimentRisk.RiskBand == "RiskOff";
        string reason = "Strategy health is acceptable.";

        if (request.RealMetrics.TryGetValue(score.StrategyName, out var metric))
        {
            if (metric.MaxDrawdown > 20m) { paused = true; reason = $"Paused due to High Drawdown: {metric.MaxDrawdown:F2}%"; }
            else if (metric.ConsecutiveLosses >= 5) { paused = true; reason = $"Paused due to Streak: {metric.ConsecutiveLosses} losses"; }
            else if (metric.RiskRejections > 10) { paused = true; reason = $"Paused due to Risk Rejections: {metric.RiskRejections}"; }
            else if (!paused) { health = System.Math.Clamp(health + metric.ProfitFactor * 10m, 0m, 100m); }
        }

        if (paused && reason == "Strategy health is acceptable.") reason = "Paused by generic health monitor due to weak score or RiskOff.";

        return new StrategyHealthSnapshot
        {
            IsPaused = paused,
            HealthScore = System.Math.Round(health, 2),
            Reason = reason
        };
    }
}

public class MultiArmedBanditAllocator
{
    public BanditAllocation Select(System.Collections.Generic.IReadOnlyList<StrategyScoreSnapshot> scores, AdaptiveOrchestrationRequest request)
    {
        var best = scores.OrderByDescending(s => s.Score).First();
        decimal explorationWeight = 0.20m;

        if (request.RealMetrics.TryGetValue(best.StrategyName, out var realMetric))
        {
            if (realMetric.WinRate > 0.55m && realMetric.ProfitFactor > 1.5m) explorationWeight = 0.05m;
            else if (realMetric.WinRate < 0.40m || realMetric.MaxDrawdown > 15m) explorationWeight = 0.40m;
        }
        else if (request.HistoricalReports.TryGetValue(best.StrategyName, out var report))
        {
            if (report.WinRate > 0.55m && report.SharpeRatio > 1.5m) explorationWeight = 0.05m;
            else if (report.WinRate < 0.40m || report.MaxDrawdownPercent > 15m) explorationWeight = 0.40m;
        }

        return new BanditAllocation
        {
            SelectedArm = best.StrategyName,
            ExplorationWeight = explorationWeight,
            ExploitationWeight = 1.0m - explorationWeight
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
    public TradeAttributionSnapshot Attribute(StrategyScoreSnapshot strategy, AssetScoreSnapshot asset, IntelligenceSnapshot intelligence, AdaptiveOrchestrationRequest request)
    {
        var primary = strategy.Score >= asset.Score ? "StrategyScore" : "AssetScore";
        if (request.RealMetrics.TryGetValue(strategy.StrategyName, out var metric) && metric.ProfitFactor > 2m)
            primary = "RealHistoricalAdvantage";

        return new TradeAttributionSnapshot
        {
            PrimaryDriver = primary,
            Factors = new System.Collections.Generic.List<string> { $"Strategy: {strategy.Score:F2}", $"Asset: {asset.Score:F2}" }
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

public class AdaptiveMetricsAggregator
{
    private readonly IFeatureStore _store;
    private readonly IBacktestRepository _backtests;

    public AdaptiveMetricsAggregator(IFeatureStore store, IBacktestRepository backtests)
    {
        _store = store;
        _backtests = backtests;
    }

    public async Task<AdaptiveMetricBreakdown> BuildBreakdownAsync(
        string strategyName,
        string symbol,
        string timeframe,
        string regime,
        AdaptiveMetricsAggregationOptions? options = null)
    {
        var effectiveOptions = NormalizeOptions(options);
        var normalizedSymbol = symbol.ToUpperInvariant();
        var normalizedRegime = string.IsNullOrWhiteSpace(regime) ? "Unknown" : regime;
        var normalizedTimeframe = string.IsNullOrWhiteSpace(timeframe) ? "unknown" : timeframe;

        var reports = (await _backtests.GetReportsAsync(effectiveOptions.BacktestReportLimit))
            .Where(r => Matches(r.StrategyName, strategyName)
                && Matches(r.Symbol, normalizedSymbol)
                && Matches(r.Interval, normalizedTimeframe))
            .ToList();

        var paperTrades = (await _store.GetPaperTradesAsync(normalizedSymbol, effectiveOptions.PaperTradeLimit))
            .OrderBy(t => t.ExecutedAt)
            .ToList();

        var audits = (await _store.GetDecisionAuditsAsync(effectiveOptions.DecisionAuditLimit))
            .Where(a => Matches(a.Symbol, normalizedSymbol) && Matches(a.StrategyName, strategyName))
            .OrderBy(a => a.Timestamp)
            .ToList();

        var approvedAudits = audits.Where(a => Matches(a.Decision, "APPROVED")).ToList();
        var riskRejections = audits.Count(a => Matches(a.Decision, "REJECTED"));
        var matchedPaperTrades = MatchPaperTradesToAudits(paperTrades, approvedAudits, effectiveOptions.AuditTradeMatchWindow);

        var backtestSamples = 0;
        var backtestWins = 0;
        var weightedProfitFactor = 0m;
        var profitFactorWeight = 0;
        var maxDrawdown = 0m;
        var consecutiveLosses = 0;
        var slippageWeight = 0m;
        var slippageSamples = 0;

        foreach (var report in reports)
        {
            var sampleCount = GetBacktestSampleCount(report, normalizedRegime);
            if (sampleCount <= 0)
            {
                continue;
            }

            var wins = GetBacktestWins(report, normalizedRegime, sampleCount);
            backtestSamples += sampleCount;
            backtestWins += wins;
            weightedProfitFactor += BoundProfitFactor(report.ProfitFactor) * sampleCount;
            profitFactorWeight += sampleCount;
            maxDrawdown = Math.Max(maxDrawdown, report.MaxDrawdownPercent);
            consecutiveLosses = Math.Max(consecutiveLosses, report.MaxConsecutiveLosses);
            slippageWeight += report.SlippageImpactPercent * sampleCount;
            slippageSamples += sampleCount;
        }

        var paperOutcomeTrades = matchedPaperTrades.Where(t => t.PnL != 0m).ToList();
        var paperSamples = paperOutcomeTrades.Count;
        var paperWins = paperOutcomeTrades.Count(t => t.PnL > 0m);
        var paperProfitFactor = CalculateProfitFactor(paperOutcomeTrades);
        if (paperSamples > 0)
        {
            weightedProfitFactor += BoundProfitFactor(paperProfitFactor) * paperSamples;
            profitFactorWeight += paperSamples;
            consecutiveLosses = Math.Max(consecutiveLosses, CalculateCurrentLossStreak(paperOutcomeTrades));
        }

        var outcomeSamples = backtestSamples + paperSamples;
        var evidenceSamples = outcomeSamples + riskRejections;
        var hasMinimumEvidence = evidenceSamples >= effectiveOptions.MinimumEvidenceSamples;
        var winRate = outcomeSamples > 0
            ? (backtestWins + paperWins) / (decimal)outcomeSamples
            : 0.50m;
        var profitFactor = profitFactorWeight > 0
            ? weightedProfitFactor / profitFactorWeight
            : 1m;

        var metric = new StrategyPerformanceMetric
        {
            StrategyName = strategyName,
            Symbol = normalizedSymbol,
            Timeframe = normalizedTimeframe,
            Regime = normalizedRegime,
            WinRate = RoundRate(winRate),
            ProfitFactor = RoundScore(profitFactor),
            MaxDrawdown = RoundScore(maxDrawdown),
            ConsecutiveLosses = consecutiveLosses,
            SlippageTolerance = slippageSamples > 0 ? RoundScore(slippageWeight / slippageSamples) : 0m,
            RiskRejections = riskRejections,
            LastUpdated = DateTime.UtcNow
        };

        return new AdaptiveMetricBreakdown
        {
            Metric = metric,
            BacktestSamples = backtestSamples,
            PaperTradeSamples = paperSamples,
            ApprovedAudits = approvedAudits.Count,
            RiskRejections = riskRejections,
            EvidenceSamples = evidenceSamples,
            MinimumEvidenceSamples = effectiveOptions.MinimumEvidenceSamples,
            HasMinimumEvidence = hasMinimumEvidence,
            SourceSummary = $"backtests={backtestSamples}; paper_trades={paperSamples}; approved_audits={approvedAudits.Count}; risk_rejections={riskRejections}"
        };
    }

    public async Task<AdaptiveMetricBreakdown> AggregateAndPersistAsync(
        string strategyName,
        string symbol,
        string timeframe,
        string regime,
        AdaptiveMetricsAggregationOptions? options = null)
    {
        var breakdown = await BuildBreakdownAsync(strategyName, symbol, timeframe, regime, options);
        if (breakdown.HasMinimumEvidence)
        {
            await _store.SaveStrategyPerformanceMetricAsync(breakdown.Metric);
        }

        return breakdown;
    }

    public async Task<IReadOnlyList<AdaptiveMetricBreakdown>> AggregateAndPersistAsync(
        IEnumerable<string> strategyNames,
        string symbol,
        string timeframe,
        string regime,
        AdaptiveMetricsAggregationOptions? options = null)
    {
        var results = new List<AdaptiveMetricBreakdown>();
        foreach (var strategyName in strategyNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            results.Add(await AggregateAndPersistAsync(strategyName, symbol, timeframe, regime, options));
        }

        return results;
    }

    private static AdaptiveMetricsAggregationOptions NormalizeOptions(AdaptiveMetricsAggregationOptions? options)
    {
        var effective = options ?? new AdaptiveMetricsAggregationOptions();
        effective.MinimumEvidenceSamples = Math.Max(1, effective.MinimumEvidenceSamples);
        effective.BacktestReportLimit = Math.Max(1, effective.BacktestReportLimit);
        effective.PaperTradeLimit = Math.Max(1, effective.PaperTradeLimit);
        effective.DecisionAuditLimit = Math.Max(1, effective.DecisionAuditLimit);
        if (effective.AuditTradeMatchWindow <= TimeSpan.Zero)
        {
            effective.AuditTradeMatchWindow = TimeSpan.FromHours(2);
        }

        return effective;
    }

    private static List<PaperTrade> MatchPaperTradesToAudits(
        IReadOnlyList<PaperTrade> trades,
        IReadOnlyList<DecisionAudit> approvedAudits,
        TimeSpan matchWindow)
    {
        return trades
            .Where(t => t.PnL != 0m)
            .Where(t => approvedAudits.Any(a => Math.Abs((t.ExecutedAt - a.Timestamp).TotalMinutes) <= matchWindow.TotalMinutes))
            .ToList();
    }

    private static int GetBacktestSampleCount(BacktestReport report, string regime)
    {
        if (report.RegimeBreakdown.TryGetValue(regime, out var regimePerformance) && regimePerformance.Trades > 0)
        {
            return regimePerformance.Trades;
        }

        return report.TotalTrades;
    }

    private static int GetBacktestWins(BacktestReport report, string regime, int sampleCount)
    {
        if (report.RegimeBreakdown.TryGetValue(regime, out var regimePerformance) && regimePerformance.Trades > 0)
        {
            return (int)Math.Round(regimePerformance.WinRate * regimePerformance.Trades, MidpointRounding.AwayFromZero);
        }

        if (report.WinningTrades > 0 || report.LosingTrades > 0)
        {
            return report.WinningTrades;
        }

        return (int)Math.Round(report.WinRate * sampleCount, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateProfitFactor(IReadOnlyList<PaperTrade> trades)
    {
        var grossProfit = trades.Where(t => t.PnL > 0m).Sum(t => t.PnL);
        var grossLoss = Math.Abs(trades.Where(t => t.PnL < 0m).Sum(t => t.PnL));

        if (grossLoss > 0m)
        {
            return grossProfit / grossLoss;
        }

        return grossProfit > 0m ? 5m : 1m;
    }

    private static int CalculateCurrentLossStreak(IReadOnlyList<PaperTrade> trades)
    {
        var streak = 0;
        foreach (var trade in trades.OrderByDescending(t => t.ExecutedAt))
        {
            if (trade.PnL < 0m)
            {
                streak++;
                continue;
            }

            break;
        }

        return streak;
    }

    private static bool Matches(string left, string right) => left.Equals(right, StringComparison.OrdinalIgnoreCase);

    private static decimal BoundProfitFactor(decimal value) => Math.Clamp(value <= 0m ? 1m : value, 0m, 5m);

    private static decimal RoundRate(decimal value) => Math.Round(Math.Clamp(value, 0m, 1m), 4);

    private static decimal RoundScore(decimal value) => Math.Round(Math.Clamp(value, 0m, 100m), 2);
}

public class AdaptiveFeedbackStateProjector
{
    public StrategyState Project(
        AdaptiveOrchestrationRequest request,
        AdaptiveOrchestrationDecision decision,
        StrategyState? previousState,
        DateTime nowUtc)
    {
        var hasCandidateAdvantage = !decision.CandidateStrategyName.Equals(decision.ActiveStrategyName, StringComparison.OrdinalIgnoreCase)
            && decision.CandidateStrategyName.Length > 0
            && decision.StrategyScores.FirstOrDefault(s => s.StrategyName.Equals(decision.CandidateStrategyName, StringComparison.OrdinalIgnoreCase))?.Score > decision.StrategyScore;

        var advantageCycles = decision.ShouldSwitchStrategy
            ? 0
            : hasCandidateAdvantage
                ? Math.Max(request.PersistentAdvantageCycles, previousState?.AdvantageCycles ?? 0) + 1
                : 0;

        return new StrategyState
        {
            StrategyName = decision.ActiveStrategyName,
            Symbol = decision.Symbol,
            IsPaused = decision.StrategyHealth.IsPaused,
            CooldownUntil = decision.ShouldSwitchStrategy ? nowUtc : previousState?.CooldownUntil ?? request.LastSwitchAt,
            LastScore = decision.StrategyScore,
            AdvantageCycles = advantageCycles,
            LastUpdated = nowUtc
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
        var asset = _assetRanking.Rank(request.Symbol, request.Intelligence, cost, request);
        var strategyScores = request.StrategyNames
            .Select(name => _strategyScoring.Score(name, request, cost))
            .OrderByDescending(score => score.Score)
            .ToList();

        var candidate = strategyScores.First();
        var active = ResolveActiveStrategyScore(strategyScores, request.CurrentStrategyName);
        var candidateHealth = _healthMonitor.Evaluate(candidate, request);
        var shouldSwitch = ShouldSwitch(active, candidate, request, candidateHealth);
        var selected = shouldSwitch ? candidate : active;
        var selectedHealth = _healthMonitor.Evaluate(selected, request);
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
            Attribution = _attribution.Attribute(selected, asset, request.Intelligence, request),
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
