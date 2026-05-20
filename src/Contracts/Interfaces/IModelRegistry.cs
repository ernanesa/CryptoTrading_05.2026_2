using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IModelRegistry
{
    IReadOnlyList<RegisteredModelInfo> GetRegisteredModels();
}
