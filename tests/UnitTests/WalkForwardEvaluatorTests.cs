using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class WalkForwardEvaluatorTests
{
    [Fact]
    public void Compare_ShouldCalculateImprovementCorrectly()
    {
        var evaluator = new WalkForwardEvaluator();
        var fixedStrategy = new StrategyScoreSnapshot { StrategyName = "Fixed", Score = 70m };
        var adaptiveStrategy = new StrategyScoreSnapshot { StrategyName = "Adaptive", Score = 82m };

        var evaluation = evaluator.Compare(fixedStrategy, adaptiveStrategy);

        Assert.Equal(12m, evaluation.Improvement);
        Assert.Equal("AdaptivePreferred", evaluation.Verdict);
    }
}
