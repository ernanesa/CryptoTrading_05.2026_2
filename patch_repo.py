import re

with open('src/Infrastructure/Persistence/BacktestRepository.cs', 'r') as f:
    content = f.read()

content = content.replace(
"""        INSERT INTO backtest_runs (strategy_name, symbol, interval, start_time, end_time, initial_capital, final_capital, total_trades, winning_trades, losing_trades, win_rate, max_drawdown_percent, sharpe_ratio, profit_factor)
        VALUES (@StrategyName, @Symbol, @Interval, @StartTime, @EndTime, @InitialCapital, @FinalCapital, @TotalTrades, @WinningTrades, @LosingTrades, @WinRate, @MaxDrawdownPercent, @SharpeRatio, @ProfitFactor)""",
"""        INSERT INTO backtest_runs (strategy_name, symbol, interval, start_time, end_time, initial_capital, final_capital, total_trades, winning_trades, losing_trades, win_rate, max_drawdown_percent, sharpe_ratio, profit_factor, sortino_ratio, calmar_ratio)
        VALUES (@StrategyName, @Symbol, @Interval, @StartTime, @EndTime, @InitialCapital, @FinalCapital, @TotalTrades, @WinningTrades, @LosingTrades, @WinRate, @MaxDrawdownPercent, @SharpeRatio, @ProfitFactor, @SortinoRatio, @CalmarRatio)"""
)

content = content.replace(
"""        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval, start_time AS StartTime, end_time AS EndTime, initial_capital AS InitialCapital, final_capital AS FinalCapital, total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades, win_rate AS WinRate, max_drawdown_percent AS MaxDrawdownPercent, sharpe_ratio AS SharpeRatio, profit_factor AS ProfitFactor 
        FROM backtest_runs""",
"""        SELECT id, strategy_name AS StrategyName, symbol AS Symbol, interval AS Interval, start_time AS StartTime, end_time AS EndTime, initial_capital AS InitialCapital, final_capital AS FinalCapital, total_trades AS TotalTrades, winning_trades AS WinningTrades, losing_trades AS LosingTrades, win_rate AS WinRate, max_drawdown_percent AS MaxDrawdownPercent, sharpe_ratio AS SharpeRatio, profit_factor AS ProfitFactor, sortino_ratio AS SortinoRatio, calmar_ratio AS CalmarRatio 
        FROM backtest_runs"""
)

with open('src/Infrastructure/Persistence/BacktestRepository.cs', 'w') as f:
    f.write(content)
