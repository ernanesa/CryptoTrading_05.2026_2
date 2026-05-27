using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class BacktestReplayService
{
    private readonly BacktestEngine _backtestEngine;

    public BacktestReplayService(BacktestEngine backtestEngine)
    {
        _backtestEngine = backtestEngine;
    }

    public BacktestReport Replay(
        IStrategy strategy,
        List<MarketDataPoint> dataset,
        decimal initialCapital,
        IFeeModel feeModel,
        ISlippageModel slippageModel)
    {
        // Replay determinístico garantindo que o dataset seja exatamente sequencial
        var orderedData = dataset.OrderBy(d => d.Candle.OpenTime).ToList();
        return _backtestEngine.Run(strategy, orderedData, initialCapital, feeModel, slippageModel);
    }
}
