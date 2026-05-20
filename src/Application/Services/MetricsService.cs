using System.Collections.Concurrent;
using CryptoTrading.Contracts.Interfaces;

namespace CryptoTrading.Application.Services;

public class MetricsService : IMetricsService
{
    public DateTime StartTime { get; } = DateTime.UtcNow;

    private long _ingestedCandlesCount;
    public long IngestedCandlesCount => Interlocked.Read(ref _ingestedCandlesCount);

    private long _lastDbWriteLatencyMsTicks; // Ticks of double precision, represented as long bits
    public double LastDbWriteLatencyMs 
    {
        get
        {
            var bits = Interlocked.Read(ref _lastDbWriteLatencyMsTicks);
            return BitConverter.Int64BitsToDouble(bits);
        }
    }

    private long _signalsGeneratedCount;
    public long SignalsGeneratedCount => Interlocked.Read(ref _signalsGeneratedCount);

    private long _riskRejectionsCount;
    public long RiskRejectionsCount => Interlocked.Read(ref _riskRejectionsCount);

    // Using decimals with lock protection for accuracy
    private readonly object _lock = new();
    
    private decimal _paperPnL;
    public decimal PaperPnL
    {
        get { lock (_lock) return _paperPnL; }
    }

    private long _testnetRequestsCount;
    public long TestnetRequestsCount => Interlocked.Read(ref _testnetRequestsCount);

    private string _marketRegime = "Sideways";
    public string MarketRegime
    {
        get { lock (_lock) return _marketRegime; }
    }

    private decimal _totalExecutionCost;
    public decimal TotalExecutionCost
    {
        get { lock (_lock) return _totalExecutionCost; }
    }

    private decimal _currentDrawdown;
    public decimal CurrentDrawdown
    {
        get { lock (_lock) return _currentDrawdown; }
    }

    private readonly ConcurrentDictionary<string, decimal> _strategyScores = new();
    private readonly ConcurrentDictionary<string, decimal> _assetScores = new();

    public void IncrementCandles(int count = 1)
    {
        Interlocked.Add(ref _ingestedCandlesCount, count);
    }

    public void SetDbLatency(double milliseconds)
    {
        var bits = BitConverter.DoubleToInt64Bits(milliseconds);
        Interlocked.Exchange(ref _lastDbWriteLatencyMsTicks, bits);
    }

    public void IncrementSignals()
    {
        Interlocked.Increment(ref _signalsGeneratedCount);
    }

    public void IncrementRiskRejections()
    {
        Interlocked.Increment(ref _riskRejectionsCount);
    }

    public void UpdatePaperPnL(decimal pnl)
    {
        lock (_lock)
        {
            _paperPnL += pnl;
        }
    }

    public void IncrementTestnetRequests()
    {
        Interlocked.Increment(ref _testnetRequestsCount);
    }

    public void SetMarketRegime(string regime)
    {
        lock (_lock)
        {
            _marketRegime = regime;
        }
    }

    public void AddExecutionCost(decimal cost)
    {
        lock (_lock)
        {
            _totalExecutionCost += cost;
        }
    }

    public void UpdateDrawdown(decimal drawdown)
    {
        lock (_lock)
        {
            _currentDrawdown = drawdown;
        }
    }

    public void SetStrategyScore(string strategyName, decimal score)
    {
        _strategyScores[strategyName] = score;
    }

    public void SetAssetScore(string assetName, decimal score)
    {
        _assetScores[assetName] = score;
    }

    public Dictionary<string, decimal> GetStrategyScores()
    {
        return new Dictionary<string, decimal>(_strategyScores);
    }

    public Dictionary<string, decimal> GetAssetScores()
    {
        return new Dictionary<string, decimal>(_assetScores);
    }

    public object GetSnapshot()
    {
        lock (_lock)
        {
            return new
            {
                uptimeSeconds = (int)(DateTime.UtcNow - StartTime).TotalSeconds,
                candlesReceived = IngestedCandlesCount,
                dbWriteLatencyMs = Math.Round(LastDbWriteLatencyMs, 2),
                signalsGenerated = SignalsGeneratedCount,
                riskRejections = RiskRejectionsCount,
                paperPnL = Math.Round(PaperPnL, 2),
                testnetRequests = TestnetRequestsCount,
                regime = MarketRegime,
                executionCost = Math.Round(TotalExecutionCost, 2),
                drawdown = Math.Round(CurrentDrawdown, 2),
                strategyScores = GetStrategyScores(),
                assetScores = GetAssetScores()
            };
        }
    }
}
