namespace CryptoTrading.Domain.Entities;

public class TestnetAuditLog
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // e.g. "SUBMIT_ORDER", "QUERY_BALANCE", "SYNC_ORDER"
    public string Status { get; set; } = string.Empty; // SUCCESS, FAILED
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
