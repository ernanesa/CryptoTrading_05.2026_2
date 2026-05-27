namespace CryptoTrading.Domain.Entities;

public class TestnetOrder
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ClientOrderId { get; set; } = string.Empty;
    public string? BinanceOrderId { get; set; }
    public string Side { get; set; } = string.Empty; // BUY, SELL
    public string Type { get; set; } = string.Empty; // LIMIT, MARKET
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = "New";
    public string? OriginalExchangeStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
