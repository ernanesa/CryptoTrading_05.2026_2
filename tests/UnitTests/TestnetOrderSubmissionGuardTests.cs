using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.UnitTests;

public class TestnetOrderSubmissionGuardTests
{
    private static readonly DateTime Now = new(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_ApprovedMatchingRiskDecision_AllowsSubmission()
    {
        var order = CreateOrder();
        var risk = CreateRiskDecision();

        var result = TestnetOrderSubmissionGuard.Validate(order, risk, Now);

        Assert.True(result.IsApproved);
        Assert.Contains("aprovada", result.Reason);
    }

    [Fact]
    public void Validate_MissingRiskDecision_BlocksSubmission()
    {
        var result = TestnetOrderSubmissionGuard.Validate(CreateOrder(), null, Now);

        Assert.False(result.IsApproved);
        Assert.Contains("ausente", result.Reason);
    }

    [Fact]
    public void Validate_ExpiredRiskDecision_BlocksSubmission()
    {
        var risk = CreateRiskDecision(expiresAt: Now.AddSeconds(-1));

        var result = TestnetOrderSubmissionGuard.Validate(CreateOrder(), risk, Now);

        Assert.False(result.IsApproved);
        Assert.Contains("expirada", result.Reason);
    }

    [Fact]
    public void Validate_IncompatibleQuantity_BlocksSubmission()
    {
        var risk = CreateRiskDecision(quantity: 0.2m);

        var result = TestnetOrderSubmissionGuard.Validate(CreateOrder(), risk, Now);

        Assert.False(result.IsApproved);
        Assert.Contains("quantidade", result.Reason);
    }

    [Fact]
    public void CreateDecisionAudit_UsesRejectedValidation()
    {
        var validation = TestnetOrderSubmissionValidation.Rejected("RiskDecision ausente.");

        var audit = TestnetOrderSubmissionGuard.CreateDecisionAudit(CreateOrder(), null, validation, Now);

        Assert.Equal("BinanceTestnetRestBridge", audit.StrategyName);
        Assert.Equal("REJECTED", audit.Decision);
        Assert.Equal("RiskDecision ausente.", audit.Reason);
        Assert.Equal(Now, audit.Timestamp);
    }

    private static TestnetOrder CreateOrder() => new()
    {
        Symbol = "BTCUSDT",
        Side = "BUY",
        Type = "LIMIT",
        Price = 50000m,
        Quantity = 0.1m
    };

    private static RiskDecision CreateRiskDecision(
        string decision = "APPROVED",
        string symbol = "BTCUSDT",
        string side = "BUY",
        decimal price = 50000m,
        decimal quantity = 0.1m,
        DateTime? expiresAt = null)
    {
        return new RiskDecision(
            symbol,
            decision,
            "unit-test",
            Now.AddMinutes(-1),
            expiresAt ?? Now.AddMinutes(5),
            side,
            price,
            quantity);
    }
}
