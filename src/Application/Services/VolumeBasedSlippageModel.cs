using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class VolumeBasedSlippageModel : ISlippageModel
{
    private readonly decimal _baseSlippageRate;
    private readonly decimal _volumeImpactFactor;

    public VolumeBasedSlippageModel(decimal baseSlippageRate = 0.0005m, decimal volumeImpactFactor = 0.0001m)
    {
        _baseSlippageRate = baseSlippageRate;
        _volumeImpactFactor = volumeImpactFactor;
    }

    public decimal ApplySlippage(decimal basePrice, PositionType positionType, decimal orderVolume = 0m, decimal marketVolume = 0m)
    {
        decimal slippageRate = _baseSlippageRate;

        // Apply additional slippage if the order volume is a significant fraction of the market volume
        if (orderVolume > 0 && marketVolume > 0)
        {
            var volumeRatio = orderVolume / marketVolume;
            slippageRate += volumeRatio * _volumeImpactFactor;
        }

        if (positionType == PositionType.Long)
        {
            // Buy higher
            return basePrice * (1m + slippageRate);
        }
        else
        {
            // Sell lower
            return basePrice * (1m - slippageRate);
        }
    }

    public decimal ApplySlippage(decimal basePrice, PositionType positionType)
    {
        return ApplySlippage(basePrice, positionType, 0, 0);
    }
}
