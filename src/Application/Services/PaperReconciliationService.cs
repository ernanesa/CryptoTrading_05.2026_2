using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class PaperReconciliationService
{
    private readonly IFeatureStore _store;

    public PaperReconciliationService(IFeatureStore store)
    {
        _store = store;
    }

    public async Task<ReconciliationResult> ReconcileAsync(string symbol)
    {
        var balances = (await _store.GetWalletBalancesAsync()).ToList();
        var trades = (await _store.GetPaperTradesAsync(symbol, int.MaxValue)).ToList();
        var activePosition = await _store.GetActivePaperPositionAsync(symbol);
        var orders = (await _store.GetActivePaperOrdersAsync(symbol)).ToList();

        var usdtBalance = balances.FirstOrDefault(b => b.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase));
        var baseAssetSymbol = symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase)
            ? symbol.Substring(0, symbol.Length - 4)
            : symbol;
        var assetBalance = balances.FirstOrDefault(b => b.Symbol.Equals(baseAssetSymbol, StringComparison.OrdinalIgnoreCase));

        // Reconcile total holdings:
        decimal calculatedAssetQty = 0;
        decimal totalSpentUsdt = 0;
        decimal totalFeeUsdt = 0;

        foreach (var trade in trades.OrderBy(t => t.ExecutedAt))
        {
            if (trade.Type.Equals("BUY", StringComparison.OrdinalIgnoreCase))
            {
                calculatedAssetQty += trade.Quantity;
                totalSpentUsdt += (trade.Price * trade.Quantity);
                totalFeeUsdt += trade.Fee;
            }
            else if (trade.Type.Equals("SELL", StringComparison.OrdinalIgnoreCase))
            {
                calculatedAssetQty -= trade.Quantity;
                totalSpentUsdt -= (trade.Price * trade.Quantity);
                totalFeeUsdt += trade.Fee;
            }
        }

        if (calculatedAssetQty < 0) calculatedAssetQty = 0;

        bool assetMatches = Math.Abs((assetBalance?.Free ?? 0m) - calculatedAssetQty) < 0.00001m;

        // Position alignment
        bool positionMatches = true;
        if (activePosition != null && !activePosition.IsClosed)
        {
            positionMatches = Math.Abs(activePosition.Quantity - (assetBalance?.Free ?? 0m)) < 0.00001m;
        }
        else
        {
            positionMatches = (assetBalance?.Free ?? 0m) < 0.00001m;
        }

        bool isPerfect = assetMatches && positionMatches;

        return new ReconciliationResult
        {
            Symbol = symbol,
            AssetFreeBalance = assetBalance?.Free ?? 0m,
            CalculatedAssetQty = calculatedAssetQty,
            AssetMatches = assetMatches,
            PositionMatches = positionMatches,
            IsPerfect = isPerfect,
            ReconciledAt = DateTime.UtcNow
        };
    }
}

public class ReconciliationResult
{
    public string Symbol { get; set; } = string.Empty;
    public decimal AssetFreeBalance { get; set; }
    public decimal CalculatedAssetQty { get; set; }
    public bool AssetMatches { get; set; }
    public bool PositionMatches { get; set; }
    public bool IsPerfect { get; set; }
    public DateTime ReconciledAt { get; set; }
}
