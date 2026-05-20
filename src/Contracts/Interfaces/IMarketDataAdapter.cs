using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

/// <summary>
/// Contrato de adaptador para captação de dados de mercado brutos de exchanges.
/// </summary>
public interface IMarketDataAdapter
{
    /// <summary>
    /// Obtém os candles históricos para um determinado par, intervalo e período.
    /// </summary>
    Task<IEnumerable<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, DateTime startTime, DateTime endTime);
}
