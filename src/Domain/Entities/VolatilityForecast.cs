namespace CryptoTrading.Domain.Entities;

public class VolatilityForecast
{
    public string ModelVersion { get; set; } = "volatility-heuristic-m6-v1";
    public string ScoreSource { get; set; } = "FeatureStore.CandleFeature";
    public int HorizonMinutes { get; set; }
    public decimal ForecastScore { get; set; }
    public decimal ExpectedAtrPercent { get; set; }
    public decimal Confidence { get; set; }
    public string RiskBand { get; set; } = "Normal";
}
