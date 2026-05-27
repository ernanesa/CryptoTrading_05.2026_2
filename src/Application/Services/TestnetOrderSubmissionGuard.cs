using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public static class TestnetOrderSubmissionGuard
{
    public static TestnetOrderSubmissionValidation Validate(TestnetOrder order, RiskDecision? riskDecision, DateTime nowUtc)
    {
        if (riskDecision == null)
        {
            return TestnetOrderSubmissionValidation.Rejected("RiskDecision ausente.");
        }

        if (!riskDecision.Decision.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision nao aprovada: {riskDecision.Decision}.");
        }

        if (riskDecision.ExpiresAt <= nowUtc)
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision expirada em {riskDecision.ExpiresAt:O}.");
        }

        if (!riskDecision.Symbol.Equals(order.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision simbolo {riskDecision.Symbol} incompativel com ordem {order.Symbol}.");
        }

        if (!riskDecision.OrderSide.Equals(order.Side, StringComparison.OrdinalIgnoreCase))
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision lado {riskDecision.OrderSide} incompativel com ordem {order.Side}.");
        }

        if (riskDecision.Quantity != order.Quantity)
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision quantidade {riskDecision.Quantity} incompativel com ordem {order.Quantity}.");
        }

        if (order.Price > 0 && riskDecision.Price != order.Price)
        {
            return TestnetOrderSubmissionValidation.Rejected($"RiskDecision preco {riskDecision.Price} incompativel com ordem {order.Price}.");
        }

        return TestnetOrderSubmissionValidation.Approved("RiskDecision aprovada, vigente e compativel com a ordem.");
    }

    public static DecisionAudit CreateDecisionAudit(TestnetOrder order, RiskDecision? riskDecision, TestnetOrderSubmissionValidation validation, DateTime nowUtc)
    {
        return new DecisionAudit
        {
            Symbol = string.IsNullOrWhiteSpace(order.Symbol) ? riskDecision?.Symbol ?? "UNKNOWN" : order.Symbol,
            StrategyName = "BinanceTestnetRestBridge",
            SignalType = string.IsNullOrWhiteSpace(order.Side) ? riskDecision?.OrderSide ?? "UNKNOWN" : order.Side,
            Price = order.Price > 0 ? order.Price : riskDecision?.Price ?? 0m,
            Timestamp = nowUtc,
            Decision = validation.IsApproved ? "APPROVED" : "REJECTED",
            Reason = validation.Reason
        };
    }
}

public sealed record TestnetOrderSubmissionValidation(bool IsApproved, string Reason)
{
    public static TestnetOrderSubmissionValidation Approved(string reason) => new(true, reason);

    public static TestnetOrderSubmissionValidation Rejected(string reason) => new(false, reason);
}
