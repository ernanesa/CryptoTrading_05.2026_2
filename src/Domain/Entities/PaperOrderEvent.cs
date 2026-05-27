using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class PaperOrderEvent
{
    public long Id { get; set; }
    public long PaperOrderId { get; set; }
    public string ClientOrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal? FillQuantity { get; set; }
    public decimal? FillPrice { get; set; }
    public decimal? Fee { get; set; }
    public DateTime CreatedAt { get; set; }
}
