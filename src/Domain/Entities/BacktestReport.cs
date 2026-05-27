namespace CryptoTrading.Domain.Entities;

public class BacktestReport
{
    public string StrategyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal TotalPnLPercent { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal Expectancy { get; set; }
    public decimal TotalFees { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal CalmarRatio { get; set; }

    // Advanced metrics (Task F)
    public decimal ExposureTimePercent { get; set; }
    public double AvgHoldingTimeHours { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal FeeImpactPercent { get; set; }
    public decimal SlippageImpactPercent { get; set; }
    public Dictionary<string, RegimePerformance> RegimeBreakdown { get; set; } = new();

    public List<Position> Trades { get; set; } = new();
}

public class RegimePerformance
{
    public string Regime { get; set; } = string.Empty;
    public int Trades { get; set; }
    public decimal WinRate { get; set; }
    public decimal PnL { get; set; }
    public decimal AvgReturn { get; set; }
}
