using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class BacktestEngine
{
    private readonly PerformanceAnalyzer _analyzer = new();

    public BacktestReport Run(
        IStrategy strategy,
        List<MarketDataPoint> data,
        decimal initialCapital,
        IFeeModel feeModel,
        ISlippageModel slippageModel)
    {
        if (strategy == null) throw new ArgumentNullException(nameof(strategy));
        if (data == null || data.Count == 0) throw new ArgumentException("Dados do backtest não podem estar vazios.");
        if (initialCapital <= 0) throw new ArgumentException("Capital inicial deve ser maior que zero.");

        var sortedData = data.OrderBy(d => d.Candle.OpenTime).ToList();
        var symbol = sortedData[0].Candle.Symbol;
        var interval = sortedData[0].Candle.Interval;

        var report = new BacktestReport
        {
            StrategyName = strategy.Name,
            Symbol = symbol,
            Interval = interval,
            StartTime = sortedData.First().Candle.OpenTime,
            EndTime = sortedData.Last().Candle.OpenTime,
            InitialCapital = initialCapital,
            FinalCapital = initialCapital
        };

        decimal capital = initialCapital;
        Position? activePosition = null;
        var trades = new List<Position>();
        long tradeIdCounter = 1;

        for (var i = 0; i < sortedData.Count; i++)
        {
            var current = sortedData[i];
            var history = sortedData.Take(i).ToList();

            var signal = strategy.GenerateSignal(current, history);

            if (activePosition == null)
            {
                // Spot Long Entry
                if (signal.Type == TradeSignalType.Buy)
                {
                    // Alocação de 98% do capital livre para permitir margem de taxas de corretagem
                    var allocationCapital = capital * 0.98m;
                    var executionPrice = slippageModel.ApplySlippage(current.Candle.Close, PositionType.Long);
                    
                    if (executionPrice > 0m && allocationCapital > 0m)
                    {
                        var quantity = allocationCapital / executionPrice;
                        var entryFee = feeModel.CalculateFee(quantity, executionPrice);
                        
                        activePosition = new Position
                        {
                            Id = tradeIdCounter++,
                            Symbol = symbol,
                            Type = PositionType.Long,
                            EntryPrice = executionPrice,
                            Quantity = quantity,
                            EntryTime = current.Candle.OpenTime,
                            FeesPaid = entryFee,
                            State = CryptoTrading.Domain.Enums.PositionState.Open
                        };

                        capital -= (quantity * executionPrice) + entryFee;
                    }
                }
            }
            else
            {
                // Spot Long Exit (Sell or Exit signal)
                if (signal.Type == TradeSignalType.Exit || signal.Type == TradeSignalType.Sell)
                {
                    var executionPrice = slippageModel.ApplySlippage(current.Candle.Close, PositionType.Short);
                    var exitFee = feeModel.CalculateFee(activePosition.Quantity, executionPrice);

                    activePosition.Close(executionPrice, current.Candle.OpenTime, exitFee);
                    capital += (activePosition.Quantity * executionPrice) - exitFee;
                    
                    trades.Add(activePosition);
                    activePosition = null;
                }
            }
        }

        // Força o encerramento da posição aberta no último candle para o relatório refletir o capital final real
        if (activePosition != null)
        {
            var lastPoint = sortedData.Last();
            var executionPrice = slippageModel.ApplySlippage(lastPoint.Candle.Close, PositionType.Short);
            var exitFee = feeModel.CalculateFee(activePosition.Quantity, executionPrice);

            activePosition.Close(executionPrice, lastPoint.Candle.OpenTime, exitFee);
            capital += (activePosition.Quantity * executionPrice) - exitFee;
            
            trades.Add(activePosition);
        }

        report.FinalCapital = capital;
        report.Trades = trades;

        // Analisar performance e popular métricas estruturadas
        _analyzer.PopulateMetrics(report);

        return report;
    }
}
