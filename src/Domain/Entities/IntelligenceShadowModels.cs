namespace CryptoTrading.Domain.Entities;

public class FeatureSchemaVersion
{
    public string Version { get; set; } = "feature-schema/v1";
    public string Source { get; set; } = "FeatureStore.CandleFeature";
    public IReadOnlyList<string> Fields { get; set; } =
    [
        "Ema21",
        "Ema50",
        "Adx",
        "Atr14",
        "Spread",
        "VolumeZScore",
        "Imbalance",
        "Returns"
    ];
}

public class ShadowModelOutput
{
    public string ModelName { get; set; } = "shadow-risk-context";
    public string ModelVersion { get; set; } = "shadow-heuristic-v1";
    public string Source { get; set; } = "ShadowModelRunner";
    public bool IsShadowMode { get; set; } = true;
    public decimal Confidence { get; set; }
    public decimal Score { get; set; }
    public string Label { get; set; } = "Neutral";
    public string DriftStatus { get; set; } = "Stable";
    public string Explanation { get; set; } = "Shadow output is auxiliary context only and cannot execute trades.";
}
