namespace CryptoTrading.Domain.Entities;

public class MetaLabelingResult
{
    public string ModelVersion { get; set; } = "meta-label-heuristic-m6-v1";
    public string ScoreSource { get; set; } = "FeatureStore.CandleFeature";
    public string Label { get; set; } = "Neutral";
    public decimal Probability { get; set; }
    public decimal QualityScore { get; set; }
    public bool IsTradeContextFavorable { get; set; }
}
