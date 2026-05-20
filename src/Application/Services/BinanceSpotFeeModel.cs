using CryptoTrading.Contracts.Interfaces;

namespace CryptoTrading.Application.Services;

public class BinanceSpotFeeModel : IFeeModel
{
    private readonly decimal _feeRate;

    public BinanceSpotFeeModel(decimal feeRate = 0.001m) // Padrão: 0.1%
    {
        _feeRate = feeRate;
    }

    public decimal CalculateFee(decimal size, decimal price)
    {
        return size * price * _feeRate;
    }
}
