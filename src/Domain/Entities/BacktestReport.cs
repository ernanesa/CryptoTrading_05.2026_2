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
    public List<Position> Trades { get; set; } = new();
}
