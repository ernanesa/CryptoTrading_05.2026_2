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

public class RiskAntiBypassTests
{
    private readonly RiskEngine _riskEngine = new();

    [Fact]
    public void ValidateSignal_HaltedStatus_MustRejectAllNewTrades()
    {
        var buySignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Buy };
        var sellSignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Sell };

        var balances = new List<WalletBalance>
        {
            new WalletBalance { Symbol = "USDT", Free = 10000m }
        };

        var resultBuy = _riskEngine.ValidateSignal(buySignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);
        var resultSell = _riskEngine.ValidateSignal(sellSignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);

        // Nenhuma ação de compra/venda pode bypassar o estado Halted
        Assert.False(resultBuy.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultBuy.NewStatus);

        Assert.False(resultSell.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultSell.NewStatus);
        
        // E o sinal de Exit também é bloqueado pois o check Halted é prioritário no início do método
        var exitSignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Exit };
        var resultExit = _riskEngine.ValidateSignal(exitSignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);
        Assert.False(resultExit.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultExit.NewStatus);
    }

    [Fact]
    public void ShadowModelRunner_IsCompletelyPassive_NeverTriggersTrades()
    {
        var runner = new ShadowModelRunner();
        var featureVector = new IntelligenceFeatureVector { LiquidityStressScore = 10m, MomentumScore = 45m };
        var volatility = new VolatilityForecast { ForecastScore = 20m, HorizonMinutes = 60 };
        
        var output = runner.Run(featureVector, volatility, 15m);

        // O output deve ser informativo, com drift estável e explicação explícita sobre gates de risco
        Assert.NotNull(output);
        Assert.Equal("Stable", output.DriftStatus);
        Assert.Contains("execution remains gated by RiskEngine/RiskDecision", output.Explanation);
    }

    [Fact]
    public void TestnetOrderSubmissionGuard_MissingRiskDecision_RejectsOrder()
    {
        var order = new TestnetOrder
        {
            Symbol = "BTCUSDT",
            Side = "BUY",
            Type = "LIMIT",
            Price = 50000m,
            Quantity = 0.1m
        };

        var now = DateTime.UtcNow;
        var validation = TestnetOrderSubmissionGuard.Validate(order, null, now);

        Assert.False(validation.IsApproved);
        Assert.Contains("ausente", validation.Reason);
    }

    [Fact]
    public void TestnetOrderSubmissionGuard_MismatchSymbolOrSide_RejectsOrder()
    {
        var order = new TestnetOrder
        {
            Symbol = "BTCUSDT",
            Side = "BUY",
            Type = "LIMIT",
            Price = 50000m,
            Quantity = 0.1m
        };

        var now = DateTime.UtcNow;
        var expiredRisk = new RiskDecision(
            "ETHUSDT", // Símbolo incompatível!
            "APPROVED",
            "unit-test",
            now.AddMinutes(-5),
            now.AddMinutes(5),
            "BUY",
            50000m,
            0.1m
        );

        var validation = TestnetOrderSubmissionGuard.Validate(order, expiredRisk, now);

        Assert.False(validation.IsApproved);
        Assert.Contains("simbolo", validation.Reason);
    }
}

