namespace CryptoTrading.Domain.Entities;

public class MarketDataPoint
{
    public Candle Candle { get; set; } = null!;
    public CandleFeature Feature { get; set; } = null!;
}
