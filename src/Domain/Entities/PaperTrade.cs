namespace CryptoTrading.Domain.Entities;

public class PaperTrade
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // BUY, SELL
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal Fee { get; set; }
    public decimal PnL { get; set; }
    public DateTime ExecutedAt { get; set; }
}
