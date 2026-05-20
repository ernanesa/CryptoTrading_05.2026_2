namespace CryptoTrading.Domain.Entities;

public class SentimentRiskSnapshot
{
    public string ModelVersion { get; set; } = "sentiment-risk-heuristic-m6-v1";
    public string ScoreSource { get; set; } = "internal-market-context";
    public decimal SentimentScore { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskBand { get; set; } = "Neutral";
    public List<string> Sources { get; set; } = new();
}
