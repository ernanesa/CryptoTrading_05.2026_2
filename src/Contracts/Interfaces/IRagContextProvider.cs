using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IRagContextProvider
{
    RagContextSnapshot BuildContext(string symbol, string interval, string regime);
}
