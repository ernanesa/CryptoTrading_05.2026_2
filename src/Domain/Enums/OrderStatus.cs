namespace CryptoTrading.Domain.Enums;

public enum OrderStatus
{
    New,
    Open,
    PartiallyFilled,
    Filled,
    Rejected,
    Cancelled,
    Expired
}
