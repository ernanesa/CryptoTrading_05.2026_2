namespace CryptoTrading.Domain.Entities;

/// <summary>
/// Representa um candle (KLine) unificado no domínio de negociação.
/// </summary>
public class Candle
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime OpenTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime CloseTime { get; set; }
}
