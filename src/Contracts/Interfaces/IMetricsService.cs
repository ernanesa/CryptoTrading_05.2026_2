namespace CryptoTrading.Contracts.Interfaces;

public interface IMetricsService
{
    DateTime StartTime { get; }
    long IngestedCandlesCount { get; }
    double LastDbWriteLatencyMs { get; }
    long SignalsGeneratedCount { get; }
    long RiskRejectionsCount { get; }
    decimal PaperPnL { get; }
    long TestnetRequestsCount { get; }
    string MarketRegime { get; }
    decimal TotalExecutionCost { get; }
    decimal CurrentDrawdown { get; }

    void IncrementCandles(int count = 1);
    void SetDbLatency(double milliseconds);
    void IncrementSignals();
    void IncrementRiskRejections();
    void UpdatePaperPnL(decimal pnl);
    void IncrementTestnetRequests();
    void SetMarketRegime(string regime);
    void AddExecutionCost(decimal cost);
    void UpdateDrawdown(decimal drawdown);
    
    void SetStrategyScore(string strategyName, decimal score);
    void SetAssetScore(string assetName, decimal score);
    
    Dictionary<string, decimal> GetStrategyScores();
    Dictionary<string, decimal> GetAssetScores();
    object GetSnapshot();
}
