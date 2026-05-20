using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Application.Strategies;

namespace CryptoTrading.Application.Services;

public class StrategyRegistry
{
    private readonly Dictionary<string, IStrategy> _strategies = new(StringComparer.OrdinalIgnoreCase);

    public StrategyRegistry()
    {
        // Registro automático das estratégias padrão
        Register(new EmaTrendFollowingStrategy());
        Register(new RsiMeanReversionStrategy());
        Register(new BollingerMeanReversionStrategy());
        Register(new AtrBreakoutStrategy());
    }

    public void Register(IStrategy strategy)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        _strategies[strategy.Name] = strategy;
    }

    public IStrategy? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _strategies.TryGetValue(name, out var strategy) ? strategy : null;
    }

    public IEnumerable<IStrategy> GetAll()
    {
        return _strategies.Values;
    }
}
