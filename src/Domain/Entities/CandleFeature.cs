namespace CryptoTrading.Domain.Entities;

/// <summary>
/// Representa os indicadores técnicos calculados (Features) associados a um candle.
/// </summary>
public class CandleFeature
{
    public long CandleId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime OpenTime { get; set; }
    public decimal Ema9 { get; set; }
    public decimal Ema21 { get; set; }
    public decimal Ema50 { get; set; }
    public decimal Ema200 { get; set; }
    public decimal Rsi14 { get; set; }
    public decimal MacdValue { get; set; }
    public decimal MacdSignal { get; set; }
    public decimal MacdHistogram { get; set; }
    public decimal Atr14 { get; set; }
    public decimal BbUpper { get; set; }
    public decimal BbMiddle { get; set; }
    public decimal BbLower { get; set; }
    public decimal Adx { get; set; }
    public decimal Returns { get; set; }
    public decimal VolumeZScore { get; set; }
    public decimal Spread { get; set; }
    public decimal Imbalance { get; set; }
    public DateTime CalculatedAt { get; set; }
}
