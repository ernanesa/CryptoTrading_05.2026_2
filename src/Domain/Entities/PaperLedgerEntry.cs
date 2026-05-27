using System;

namespace CryptoTrading.Domain.Entities;

public class PaperLedgerEntry
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Asset { get; set; } = string.Empty; // e.g. USDT, BTC
    public decimal Amount { get; set; }
    public string EntryType { get; set; } = string.Empty; // DEPOSIT, WITHDRAW, FEE, REALIZED_PNL
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
