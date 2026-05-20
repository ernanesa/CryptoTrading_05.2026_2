namespace CryptoTrading.Domain.Entities;

public class ExchangeFilterInfo
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TickSize { get; set; }
    public decimal StepSize { get; set; }
    public decimal MinQty { get; set; }
    public decimal MaxQty { get; set; }
    public decimal MinNotional { get; set; }
    public int PricePrecision { get; set; }
    public int QuantityPrecision { get; set; }
}
