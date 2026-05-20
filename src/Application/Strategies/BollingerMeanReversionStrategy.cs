using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Strategies;

public class BollingerMeanReversionStrategy : IStrategy
{
    public string Name => "Bollinger Mean Reversion";

    public TradeSignal GenerateSignal(MarketDataPoint current, List<MarketDataPoint> history)
    {
        var signal = new TradeSignal
        {
            Symbol = current.Candle.Symbol,
            Timestamp = current.Candle.OpenTime,
            Type = TradeSignalType.Hold,
            Description = "Aguardando sinal Bollinger"
        };

        // Evita warmup periods onde BB ainda não foi calculada
        if (current.Feature.BbLower == 0m || current.Feature.BbMiddle == 0m || current.Feature.BbUpper == 0m)
        {
            return signal;
        }

        var close = current.Candle.Close;

        // Compra: preço de fechamento abaixo da banda inferior (sobrevenda estatística)
        var isUnderLowerBand = close < current.Feature.BbLower;

        // Venda/Saída: preço atinge ou supera a média (retorno à média)
        var isAboveMiddleBand = close >= current.Feature.BbMiddle;

        if (isUnderLowerBand)
        {
            signal.Type = TradeSignalType.Buy;
            signal.Description = $"Preço ({close:F2}) abaixo da Banda Inferior ({current.Feature.BbLower:F2})";
        }
        else if (isAboveMiddleBand)
        {
            signal.Type = TradeSignalType.Exit;
            signal.Description = $"Preço ({close:F2}) cruzou média de Bollinger ({current.Feature.BbMiddle:F2})";
        }

        return signal;
    }
}
