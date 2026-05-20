using CryptoTrading.Domain.Entities;
using Skender.Stock.Indicators;

namespace CryptoTrading.Application.Services;

/// <summary>
/// Serviço de aplicação especializado no cálculo matemático de indicadores técnicos (Features) usando Skender.Stock.Indicators.
/// </summary>
public class IndicatorService
{
    /// <summary>
    /// Calcula o lote completo de indicadores técnicos obrigatórios para a lista de candles fornecida.
    /// Trata de forma resiliente os warmup periods dos indicadores retornando fallbacks seguros.
    /// </summary>
    public List<CandleFeature> CalculateFeatures(List<Candle> candles)
    {
        if (candles == null || candles.Count == 0)
        {
            return new List<CandleFeature>();
        }

        // 1. Converter para Quotes da biblioteca Skender.Indicators
        var quotes = candles.Select(c => new Quote
        {
            Date = c.OpenTime,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.Volume
        }).OrderBy(q => q.Date).ToList();

        // 2. Executar cálculos matemáticos estruturados
        var ema9Results = quotes.GetEma(9).ToDictionary(r => r.Date);
        var ema21Results = quotes.GetEma(21).ToDictionary(r => r.Date);
        var ema50Results = quotes.GetEma(50).ToDictionary(r => r.Date);
        var ema200Results = quotes.GetEma(200).ToDictionary(r => r.Date);
        var rsiResults = quotes.GetRsi(14).ToDictionary(r => r.Date);
        var macdResults = quotes.GetMacd(12, 26, 9).ToDictionary(r => r.Date);
        var atrResults = quotes.GetAtr(14).ToDictionary(r => r.Date);
        var bbResults = quotes.GetBollingerBands(20, 2).ToDictionary(r => r.Date);
        var adxResults = quotes.GetAdx(14).ToDictionary(r => r.Date);

        var featuresList = new List<CandleFeature>();

        foreach (var candle in candles)
        {
            var date = candle.OpenTime;

            // Auxiliar interno para conversão nula segura (warmup do indicador)
            decimal GetValue(double? val) => val.HasValue ? (decimal)val.Value : 0m;

            var ema9 = ema9Results.TryGetValue(date, out var e9) ? GetValue(e9.Ema) : 0m;
            var ema21 = ema21Results.TryGetValue(date, out var e21) ? GetValue(e21.Ema) : 0m;
            var ema50 = ema50Results.TryGetValue(date, out var e50) ? GetValue(e50.Ema) : 0m;
            var ema200 = ema200Results.TryGetValue(date, out var e200) ? GetValue(e200.Ema) : 0m;
            var rsi = rsiResults.TryGetValue(date, out var r) ? GetValue(r.Rsi) : 0m;
            
            var macdVal = macdResults.TryGetValue(date, out var m) ? GetValue(m.Macd) : 0m;
            var macdSig = macdResults.TryGetValue(date, out var ms) ? GetValue(ms.Signal) : 0m;
            var macdHist = macdResults.TryGetValue(date, out var mh) ? GetValue(mh.Histogram) : 0m;

            var atr = atrResults.TryGetValue(date, out var a) ? GetValue(a.Atr) : 0m;

            var bbUpper = bbResults.TryGetValue(date, out var bbu) ? GetValue(bbu.UpperBand) : 0m;
            var bbMiddle = bbResults.TryGetValue(date, out var bbm) ? GetValue(bbm.Sma) : 0m;
            var bbLower = bbResults.TryGetValue(date, out var bbl) ? GetValue(bbl.LowerBand) : 0m;

            var adx = adxResults.TryGetValue(date, out var ad) ? GetValue(ad.Adx) : 0m;

            featuresList.Add(new CandleFeature
            {
                CandleId = candle.Id,
                Symbol = candle.Symbol,
                OpenTime = candle.OpenTime,
                Ema9 = ema9,
                Ema21 = ema21,
                Ema50 = ema50,
                Ema200 = ema200,
                Rsi14 = rsi,
                MacdValue = macdVal,
                MacdSignal = macdSig,
                MacdHistogram = macdHist,
                Atr14 = atr,
                BbUpper = bbUpper,
                BbMiddle = bbMiddle,
                BbLower = bbLower,
                Adx = adx,
                CalculatedAt = DateTime.UtcNow
            });
        }

        return featuresList;
    }
}
