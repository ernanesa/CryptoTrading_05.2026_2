using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

/// <summary>
/// Contrato para o armazenamento persistente de candles e suas respectivas features calculadas.
/// </summary>
public interface IFeatureStore
{
    /// <summary>
    /// Garante que o schema relacional (tabelas e índices) esteja criado no banco de dados.
    /// </summary>
    Task InitializeSchemaAsync();

    /// <summary>
    /// Insere ou atualiza candles históricos em lote de forma altamente eficiente.
    /// </summary>
    Task SaveCandlesAsync(IEnumerable<Candle> candles);

    /// <summary>
    /// Grava as features calculadas e associadas aos candles de forma eficiente.
    /// </summary>
    Task SaveFeaturesAsync(IEnumerable<CandleFeature> features);

    /// <summary>
    /// Resgata o último timestamp de candle salvo para um determinado par e intervalo.
    /// Útil para evitar re-ingestão e calcular deltas de dados faltantes.
    /// </summary>
    Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval);

    /// <summary>
    /// Resgata os MarketDataPoints (candles + features) salvos no banco de dados para um determinado par e intervalo temporal.
    /// </summary>
    Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime);
}
