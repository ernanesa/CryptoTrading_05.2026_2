using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Contracts.Interfaces;

public interface IRiskEngine
{
    RiskValidationResult ValidateSignal(
        TradeSignal signal,
        decimal price,
        decimal spread,
        IEnumerable<WalletBalance> balances,
        IEnumerable<PaperTrade> recentTrades,
        RiskStatus currentStatus);
}
