namespace CryptoTrading.Contracts.Interfaces;

public interface IFeeModel
{
    decimal CalculateFee(decimal size, decimal price);
}
