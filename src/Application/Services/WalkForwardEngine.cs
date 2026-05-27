using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class WalkForwardEngine
{
    private readonly BacktestEngine _backtestEngine;

    public WalkForwardEngine(BacktestEngine backtestEngine)
    {
        _backtestEngine = backtestEngine;
    }

    public List<BacktestReport> RunWalkForward(
        IStrategy strategy,
        List<MarketDataPoint> data,
        int trainWindowDays,
        int testWindowDays,
        decimal initialCapital,
        IFeeModel feeModel,
        ISlippageModel slippageModel)
    {
        var sortedData = data.OrderBy(d => d.Candle.OpenTime).ToList();
        if (!sortedData.Any()) return new List<BacktestReport>();

        var firstDate = sortedData.First().Candle.OpenTime;
        var lastDate = sortedData.Last().Candle.OpenTime;

        var reports = new List<BacktestReport>();
        var currentTrainStart = firstDate;

        int windowIndex = 1;
        while (true)
        {
            var trainEnd = currentTrainStart.AddDays(trainWindowDays);
            var testEnd = trainEnd.AddDays(testWindowDays);

            if (trainEnd > lastDate) break;

            var trainData = sortedData.Where(d => d.Candle.OpenTime >= currentTrainStart && d.Candle.OpenTime < trainEnd).ToList();
            var testData = sortedData.Where(d => d.Candle.OpenTime >= trainEnd && d.Candle.OpenTime < testEnd).ToList();

            if (!testData.Any()) break;

            // Optional: You could use trainData to optimize strategy parameters here (if adaptive)
            
            // Run on Test Window
            var report = _backtestEngine.Run(strategy, testData, initialCapital, feeModel, slippageModel);
            report.StrategyName = $"{strategy.Name}_WF_{windowIndex}";
            reports.Add(report);

            currentTrainStart = currentTrainStart.AddDays(testWindowDays); // Rolling Window
            windowIndex++;
        }

        return reports;
    }
}
