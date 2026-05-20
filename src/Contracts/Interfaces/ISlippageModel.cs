using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Contracts.Interfaces;

public interface ISlippageModel
{
    decimal ApplySlippage(decimal price, PositionType type);
}
