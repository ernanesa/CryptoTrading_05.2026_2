namespace CryptoTrading.Domain.Entities;

public class IntelligenceFeatureVector
{
    public string Version { get; set; } = "feature-vector/v1";
    public string Source { get; set; } = "FeatureStore.CandleFeature";
    public DateTime OpenTime { get; set; }
    public decimal MomentumScore { get; set; }
    public decimal TrendScore { get; set; }
    public decimal VolumePressureScore { get; set; }
    public decimal LiquidityStressScore { get; set; }
    public decimal NormalizedReturn { get; set; }
    public decimal AtrPercent { get; set; }
}
