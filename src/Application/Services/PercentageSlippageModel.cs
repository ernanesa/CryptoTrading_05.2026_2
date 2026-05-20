using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class PercentageSlippageModel : ISlippageModel
{
    private readonly decimal _slippagePercent;

    public PercentageSlippageModel(decimal slippagePercent = 0.0005m) // Padrão: 0.05%
    {
        _slippagePercent = slippagePercent;
    }

    public decimal ApplySlippage(decimal price, PositionType type)
    {
        // Ao comprar (Long entry ou Short exit), o preço aumenta com o slippage.
        // Ao vender (Short entry ou Long exit), o preço diminui com o slippage.
        return type switch
        {
            PositionType.Long => price * (1m + _slippagePercent),
            PositionType.Short => price * (1m - _slippagePercent),
            _ => price
        };
    }
}
