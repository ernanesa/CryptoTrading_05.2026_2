namespace CryptoTrading.Domain.Entities;

public class IntelligenceSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime SnapshotTime { get; set; }
    public string SchemaVersion { get; set; } = "intelligence-snapshot/v1";
    public string ModelVersion { get; set; } = "heuristic-m6-v1";
    public string ScoreVersion { get; set; } = "score-v1";
    public string ScoreSource { get; set; } = "FeatureStore.CandleFeature";
    public string MarketRegime { get; set; } = "Sideways";
    public decimal RegimeConfidence { get; set; }
    public decimal AnomalyScore { get; set; }
    public decimal VolatilityScore { get; set; }
    public bool HasAnomaly { get; set; }
    public List<string> Insights { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
