using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Strategies;

public class MacdAdxTrendFollowingStrategy : IStrategy
{
    public string Name => "MACD ADX Trend Following";

    public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
    {
        var signal = new TradeSignal
        {
            Symbol = current.Candle.Symbol,
            Timestamp = current.Candle.OpenTime,
            Type = TradeSignalType.Hold,
            Description = "Aguardando sinal de tendência forte"
        };

        if (history == null || history.Count < 1)
        {
            return signal;
        }

        var prev = history.Last();

        // Evita warmup periods onde MACD ou ADX ainda não foram calculados (estão zerados)
        if (current.Feature.MacdValue == 0m || current.Feature.Adx == 0m ||
            prev.Feature.MacdValue == 0m || prev.Feature.Adx == 0m)
        {
            return signal;
        }

        // Crossover de alta: MACD cruza acima da linha de sinal E ADX > 25 (tendência forte)
        var isMacdCrossUp = prev.Feature.MacdValue <= prev.Feature.MacdSignal && current.Feature.MacdValue > current.Feature.MacdSignal;
        var isStrongTrend = current.Feature.Adx > 25m;
        
        // Crossover de baixa: MACD cruza abaixo da linha de sinal
        var isMacdCrossDown = prev.Feature.MacdValue >= prev.Feature.MacdSignal && current.Feature.MacdValue < current.Feature.MacdSignal;

        if (isMacdCrossUp && isStrongTrend)
        {
            signal.Type = TradeSignalType.Buy;
            signal.Description = $"MACD ({current.Feature.MacdValue:F2}) cruzou acima do Signal ({current.Feature.MacdSignal:F2}) com ADX forte ({current.Feature.Adx:F2} > 25)";
        }
        else if (isMacdCrossDown)
        {
            signal.Type = TradeSignalType.Exit;
            signal.Description = $"MACD ({current.Feature.MacdValue:F2}) cruzou abaixo do Signal ({current.Feature.MacdSignal:F2})";
        }

        return signal;
    }
}
