namespace CryptoTrading.Domain.Entities;

public class DecisionAudit
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public string Decision { get; set; } = string.Empty; // APPROVED, REJECTED
    public string Reason { get; set; } = string.Empty;
}
