using System;

namespace CryptoTrading.Domain.Entities;

public record RiskDecision(
    string Symbol,
    string Decision, // APPROVED, REJECTED
    string Reason,
    DateTime Timestamp,
    DateTime ExpiresAt,
    string OrderSide, // BUY, SELL
    decimal Price,
    decimal Quantity
);
