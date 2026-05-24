using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

public interface IBacktestRepository
{
    Task SaveReportAsync(BacktestReport report);
    Task<IEnumerable<BacktestReport>> GetReportsAsync(int limit = 50);
}
