namespace CryptoTrading.Domain.Entities;

public class EventRiskSnapshot
{
    public string ModelVersion { get; set; } = "event-risk-heuristic-m6-v1";
    public string ScoreSource { get; set; } = "market-volatility-volume-context";
    public decimal EventRiskScore { get; set; }
    public string Severity { get; set; } = "Low";
    public List<string> EventTags { get; set; } = new();
}
