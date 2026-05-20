using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Strategies;

public class RsiMeanReversionStrategy : IStrategy
{
    public string Name => "RSI Mean Reversion";

    public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
    {
        var signal = new TradeSignal
        {
            Symbol = current.Candle.Symbol,
            Timestamp = current.Candle.OpenTime,
            Type = TradeSignalType.Hold,
            Description = "Aguardando sinal RSI"
        };

        if (history == null || history.Count < 1)
        {
            return signal;
        }

        var prev = history.Last();

        // Evita warmup periods onde RSI ainda não foi calculado (0m)
        if (current.Feature.Rsi14 == 0m || prev.Feature.Rsi14 == 0m)
        {
            return signal;
        }

        // Compra: RSI cruzando para cima de 30 (retomada após sobrevenda)
        var isRsiRebound = prev.Feature.Rsi14 < 30m && current.Feature.Rsi14 >= 30m;

        // Venda/Saída: RSI maior ou igual a 70 (sobrecompra)
        var isOverbought = current.Feature.Rsi14 >= 70m;

        if (isRsiRebound)
        {
            signal.Type = TradeSignalType.Buy;
            signal.Description = $"RSI ({current.Feature.Rsi14:F2}) cruzando acima de 30 (sobrevenda concluída)";
        }
        else if (isOverbought)
        {
            signal.Type = TradeSignalType.Exit;
            signal.Description = $"RSI ({current.Feature.Rsi14:F2}) atingiu sobrecompra (>= 70)";
        }

        return signal;
    }
}
