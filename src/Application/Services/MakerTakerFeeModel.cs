using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class MakerTakerFeeModel : IFeeModel
{
    private readonly decimal _makerFeeRate;
    private readonly decimal _takerFeeRate;

    public MakerTakerFeeModel(decimal makerFeeRate = 0.001m, decimal takerFeeRate = 0.001m)
    {
        _makerFeeRate = makerFeeRate;
        _takerFeeRate = takerFeeRate;
    }

    public decimal CalculateFee(decimal size, decimal price)
    {
        // By default, backtesting assumes market orders (taker) unless stated otherwise
        // A more advanced engine would receive order type. Here we default to taker.
        return size * price * _takerFeeRate;
    }
}
