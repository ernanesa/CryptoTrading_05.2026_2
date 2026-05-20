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

        // 0. Garantir ordenação cronológica estrita
        var sortedCandles = candles.OrderBy(c => c.OpenTime).ToList();

        // 1. Converter para Quotes da biblioteca Skender.Indicators
        var quotes = sortedCandles.Select(c => new Quote
        {
            Date = c.OpenTime,
            Open = c.Open,
            High = c.High,
            Low = c.Low,
            Close = c.Close,
            Volume = c.Volume
        }).ToList();

        // 2. Executar cálculos matemáticos estruturados (Skender)
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

        for (var i = 0; i < sortedCandles.Count; i++)
        {
            var candle = sortedCandles[i];
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

            // --- Cálculos matemáticos customizados para as features adicionais da M1 ---
            
            // 1. Returns: (Close - PrevClose) / PrevClose
            decimal returns = 0m;
            if (i > 0)
            {
                var prevClose = sortedCandles[i - 1].Close;
                returns = prevClose != 0 ? (candle.Close - prevClose) / prevClose : 0m;
            }

            // 2. Spread: High - Low
            decimal spread = candle.High - candle.Low;

            // 3. Volume Z-Score (Rolling 20-period window)
            decimal volumeZScore = 0m;
            var startWindow = Math.Max(0, i - 19);
            var windowSize = i - startWindow + 1;
            if (windowSize > 0)
            {
                var windowVolumes = new List<double>();
                for (var w = startWindow; w <= i; w++)
                {
                    windowVolumes.Add((double)sortedCandles[w].Volume);
                }

                var avgVolume = windowVolumes.Average();
                var sumSq = windowVolumes.Sum(v => Math.Pow(v - avgVolume, 2));
                var stdDev = Math.Sqrt(sumSq / windowSize);

                volumeZScore = stdDev > 0 ? (decimal)(((double)candle.Volume - avgVolume) / stdDev) : 0m;
            }

            // 4. Imbalance: (2 * TakerBuyVolume - Volume) / Volume (Order Book Imbalance Simples proxy)
            decimal imbalance = candle.Volume > 0 
                ? (2 * candle.TakerBuyVolume - candle.Volume) / candle.Volume 
                : 0m;

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
                Returns = returns,
                VolumeZScore = volumeZScore,
                Spread = spread,
                Imbalance = imbalance,
                CalculatedAt = DateTime.UtcNow
            });
        }

        return featuresList;
    }
}
