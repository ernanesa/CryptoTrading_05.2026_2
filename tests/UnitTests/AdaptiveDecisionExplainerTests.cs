using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class AdaptiveDecisionExplainerTests
{
    [Fact]
    public void Explain_ShouldReturnMarkdownText()
    {
        var explainer = new AdaptiveDecisionExplainer();
        var decision = new AdaptiveOrchestrationDecision
        {
            Symbol = "BTCUSDT",
            Interval = "1m",
            MarketRegime = "TrendingUp",
            ActiveStrategyName = "EMA Trend Following",
            CandidateStrategyName = "EMA Trend Following",
            ShouldSwitchStrategy = false,
            StrategyScore = 85.50m,
            AssetScore = 90.00m,
            MarketHealthScore = 95.00m,
            PositionSize = 2500m,
            AllocationWeight = 0.25m,
            StrategyScores = new List<StrategyScoreSnapshot>
            {
                new StrategyScoreSnapshot
                {
                    StrategyName = "EMA Trend Following",
                    Score = 85.50m,
                    RegimeFitScore = 95m,
                    ExpectancyScore = 80m,
                    ProfitFactorScore = 85m,
                    DrawdownScore = 90m,
                    ExecutionCostScore = 80m
                }
            },
            Reasons = new List<string> { "Trending market favors trend following strategies." }
        };

        var markdown = explainer.Explain(decision);

        Assert.NotEmpty(markdown);
        Assert.Contains("# Explicação de Decisão Adaptativa", markdown);
        Assert.Contains("EMA Trend Following", markdown);
        Assert.Contains("COOLDOWN", markdown);
    }
}
public class MultiArmedBanditSafetyTests
{
    [Fact]
    public void Select_ExplorationWeight_ShouldSetBasedOnEvidence()
    {
        var allocator = new MultiArmedBanditAllocator();
        var scores = new List<StrategyScoreSnapshot>
        {
            new StrategyScoreSnapshot { StrategyName = "EMA Trend Following", Score = 85m }
        };

        // Standard request has empty real metrics, triggers default exploration weight 0.20
        var request = new AdaptiveOrchestrationRequest
        {
            Symbol = "BTCUSDT",
            Interval = "1m"
        };

        var allocation = allocator.Select(scores, request);

        Assert.Equal("EMA Trend Following", allocation.SelectedArm);
        Assert.Equal(0.20m, allocation.ExplorationWeight);
    }
}
