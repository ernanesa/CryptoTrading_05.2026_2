using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Strategies;

public class AtrBreakoutStrategy : IStrategy
{
    public string Name => "ATR Breakout";

    public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
    {
        var signal = new TradeSignal
        {
            Symbol = current.Candle.Symbol,
            Timestamp = current.Candle.OpenTime,
            Type = TradeSignalType.Hold,
            Description = "Aguardando rompimento"
        };

        if (history == null || history.Count < 14)
        {
            return signal;
        }

        // Evita warmup periods onde ATR ou ADX estão zerados
        if (current.Feature.Atr14 == 0m || current.Feature.Adx == 0m)
        {
            return signal;
        }

        // Calcula a máxima dos últimos 14 fechamentos
        var recentHistory = history.TakeLast(14).ToList();
        var highestClose = recentHistory.Max(p => p.Candle.Close);

        // Compra: preço rompe a máxima recente + 1.5 * ATR, com tendência forte confirmada (ADX > 20)
        var isBreakout = current.Candle.Close > (highestClose + 1.5m * current.Feature.Atr14);
        var isTrending = current.Feature.Adx > 20m;

        // Saída: stop baseado em ATR (queda acentuada de 2 * ATR em relação à abertura)
        var isStopTriggered = current.Candle.Close < (current.Candle.Open - 2.0m * current.Feature.Atr14);

        if (isBreakout && isTrending)
        {
            signal.Type = TradeSignalType.Buy;
            signal.Description = $"Rompimento de alta ({current.Candle.Close:F2} > {highestClose:F2} + ATR) com ADX ({current.Feature.Adx:F1})";
        }
        else if (isStopTriggered)
        {
            signal.Type = TradeSignalType.Exit;
            signal.Description = $"Stop ATR acionado ({current.Candle.Close:F2} < {current.Candle.Open:F2} - 2*ATR)";
        }

        return signal;
    }
}
