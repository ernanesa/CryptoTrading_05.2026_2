using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Strategies;

public class EmaTrendFollowingStrategy : IStrategy
{
    public string Name => "EMA Trend Following";

    public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
    {
        var signal = new TradeSignal
        {
            Symbol = current.Candle.Symbol,
            Timestamp = current.Candle.OpenTime,
            Type = TradeSignalType.Hold,
            Description = "Aguardando crossover"
        };

        if (history == null || history.Count < 1)
        {
            return signal;
        }

        var prev = history.Last();

        // Evita warmup periods onde EMA ainda não foi calculada (está zerada)
        if (current.Feature.Ema9 == 0m || current.Feature.Ema21 == 0m ||
            prev.Feature.Ema9 == 0m || prev.Feature.Ema21 == 0m)
        {
            return signal;
        }

        // Crossover de alta: EMA 9 cruza para cima da EMA 21
        var isCrossUp = prev.Feature.Ema9 <= prev.Feature.Ema21 && current.Feature.Ema9 > current.Feature.Ema21;
        
        // Crossover de baixa: EMA 9 cruza para baixo da EMA 21
        var isCrossDown = prev.Feature.Ema9 >= prev.Feature.Ema21 && current.Feature.Ema9 < current.Feature.Ema21;

        if (isCrossUp)
        {
            signal.Type = TradeSignalType.Buy;
            signal.Description = $"EMA 9 ({current.Feature.Ema9:F2}) cruzou acima da EMA 21 ({current.Feature.Ema21:F2})";
        }
        else if (isCrossDown)
        {
            signal.Type = TradeSignalType.Exit;
            signal.Description = $"EMA 9 ({current.Feature.Ema9:F2}) cruzou abaixo da EMA 21 ({current.Feature.Ema21:F2})";
        }

        return signal;
    }
}
