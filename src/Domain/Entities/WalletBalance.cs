namespace CryptoTrading.Domain.Entities;

public class WalletBalance
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
    public DateTime UpdatedAt { get; set; }
}
