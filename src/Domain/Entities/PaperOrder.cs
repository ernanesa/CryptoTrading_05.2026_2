using System;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class PaperOrder
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ClientOrderId { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty; // BUY, SELL
    public OrderType Type { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal AverageFillPrice { get; set; }
    public decimal FeePaid { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public decimal RemainingQuantity => Quantity - FilledQuantity;
}
