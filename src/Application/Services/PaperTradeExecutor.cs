using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class PaperTradeExecutor
{
    private readonly IFeatureStore _store;
    private readonly IRiskEngine _riskEngine;
    private readonly IMetricsService? _metrics;
    private RiskStatus _currentStatus = RiskStatus.Normal;

    public PaperTradeExecutor(IFeatureStore store, IRiskEngine riskEngine, IMetricsService? metrics = null)
    {
        _store = store;
        _riskEngine = riskEngine;
        _metrics = metrics;
    }

    public RiskStatus CurrentStatus
    {
        get => _currentStatus;
        set => _currentStatus = value;
    }

    public async Task<DecisionAudit> ProcessSignalAsync(IStrategy strategy, MarketDataPoint currentPoint)
    {
        var symbol = currentPoint.Candle.Symbol;
        var interval = currentPoint.Candle.Interval;
        var price = currentPoint.Candle.Close;

        // 1. Carregar histórico recente do banco de dados para a estratégia avaliar condições
        var historyPoints = await _store.GetMarketDataPointsAsync(
            symbol, 
            interval, 
            currentPoint.Candle.OpenTime.AddHours(-12), 
            currentPoint.Candle.OpenTime.AddSeconds(-1)
        );
        var historyList = historyPoints.ToList();

        // 2. Gerar sinal a partir da estratégia
        var signal = strategy.GenerateSignal(currentPoint, historyList);
        _metrics?.IncrementSignals();

        // 3. Carregar estados do banco de dados (carteira e histórico de trades)
        var balances = (await _store.GetWalletBalancesAsync()).ToList();
        var recentTrades = (await _store.GetPaperTradesAsync(symbol, 50)).ToList();

        // Garantir que a carteira tenha USDT inicializado
        var usdtBalance = balances.FirstOrDefault(b => b.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase));
        if (usdtBalance == null)
        {
            usdtBalance = new WalletBalance { Symbol = "USDT", Free = 10000m, Locked = 0m, UpdatedAt = DateTime.UtcNow };
            await _store.SaveWalletBalanceAsync(usdtBalance);
            balances.Add(usdtBalance);
        }

        var baseAssetSymbol = symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase) 
            ? symbol.Substring(0, symbol.Length - 4) 
            : symbol;

        var assetBalance = balances.FirstOrDefault(b => b.Symbol.Equals(baseAssetSymbol, StringComparison.OrdinalIgnoreCase));
        if (assetBalance == null)
        {
            assetBalance = new WalletBalance { Symbol = baseAssetSymbol, Free = 0m, Locked = 0m, UpdatedAt = DateTime.UtcNow };
            await _store.SaveWalletBalanceAsync(assetBalance);
            balances.Add(assetBalance);
        }

        // 4. Validar o sinal através do RiskEngine
        var riskResult = _riskEngine.ValidateSignal(
            signal,
            price,
            currentPoint.Feature.Spread,
            balances,
            recentTrades,
            _currentStatus
        );

        _currentStatus = riskResult.NewStatus;

        var audit = new DecisionAudit
        {
            Symbol = symbol,
            StrategyName = strategy.Name,
            SignalType = signal.Type.ToString(),
            Price = price,
            Timestamp = currentPoint.Candle.OpenTime,
            Decision = riskResult.IsApproved ? "APPROVED" : "REJECTED",
            Reason = riskResult.IsApproved ? signal.Description : riskResult.Reason
        };

        if (!riskResult.IsApproved)
        {
            _metrics?.IncrementRiskRejections();
            await _store.SaveDecisionAuditAsync(audit);
            return audit;
        }

        // 5. Fluxo de execução se aprovado
        if (signal.Type == TradeSignalType.Buy)
        {
            if (usdtBalance.Free < 10m) // Mínimo de $10 USD para operar
            {
                audit.Decision = "REJECTED";
                audit.Reason = $"Saldo insuficiente de USDT para compra: ${usdtBalance.Free:F2}";
                _metrics?.IncrementRiskRejections();
                await _store.SaveDecisionAuditAsync(audit);
                return audit;
            }

            // Aplicar 0.05% de slippage e 0.1% de taxa Binance Spot
            var slippedPrice = price * 1.0005m;
            var allocateAmount = usdtBalance.Free * 0.98m; // Aloca 98% do capital livre para deixar margem para taxas
            var quantity = allocateAmount / slippedPrice;
            var fee = allocateAmount * 0.001m;

            usdtBalance.Free -= (allocateAmount + fee);
            usdtBalance.UpdatedAt = DateTime.UtcNow;
            assetBalance.Free += quantity;
            assetBalance.UpdatedAt = DateTime.UtcNow;

            await _store.SaveWalletBalanceAsync(usdtBalance);
            await _store.SaveWalletBalanceAsync(assetBalance);

            var trade = new PaperTrade
            {
                Symbol = symbol,
                Type = "BUY",
                Price = slippedPrice,
                Quantity = quantity,
                Fee = fee,
                PnL = 0m,
                ExecutedAt = DateTime.UtcNow
            };

            await _store.SavePaperTradeAsync(trade);
            audit.Reason = $"COMPRA executada: {quantity:F4} {baseAssetSymbol} a ${slippedPrice:F2} (Taxa: ${fee:F2})";

            var execCost = fee + (slippedPrice - price) * quantity;
            _metrics?.AddExecutionCost(execCost);
            _metrics?.SetStrategyScore(strategy.Name, 88.5m);
            _metrics?.SetAssetScore(symbol, 90.0m);
        }
        else if (signal.Type == TradeSignalType.Exit || signal.Type == TradeSignalType.Sell)
        {
            if (assetBalance.Free <= 0.00001m) // Sem saldo do ativo para vender
            {
                audit.Decision = "REJECTED";
                audit.Reason = $"Sem saldo disponível de {baseAssetSymbol} para venda.";
                _metrics?.IncrementRiskRejections();
                await _store.SaveDecisionAuditAsync(audit);
                return audit;
            }

            var slippedPrice = price * 0.9995m; // Venda com slippage (preço menor)
            var quantity = assetBalance.Free;
            var saleValue = quantity * slippedPrice;
            var fee = saleValue * 0.001m;

            // Calcular PnL baseado no último trade de compra
            var lastBuyTrade = recentTrades.FirstOrDefault(t => t.Type.Equals("BUY", StringComparison.OrdinalIgnoreCase));
            decimal pnl = 0m;
            if (lastBuyTrade != null)
            {
                pnl = (slippedPrice - lastBuyTrade.Price) * quantity - fee - lastBuyTrade.Fee;
            }
            else
            {
                pnl = saleValue - fee;
            }

            usdtBalance.Free += (saleValue - fee);
            usdtBalance.UpdatedAt = DateTime.UtcNow;
            assetBalance.Free = 0m;
            assetBalance.UpdatedAt = DateTime.UtcNow;

            await _store.SaveWalletBalanceAsync(usdtBalance);
            await _store.SaveWalletBalanceAsync(assetBalance);

            var trade = new PaperTrade
            {
                Symbol = symbol,
                Type = "SELL",
                Price = slippedPrice,
                Quantity = quantity,
                Fee = fee,
                PnL = pnl,
                ExecutedAt = DateTime.UtcNow
            };

            await _store.SavePaperTradeAsync(trade);
            audit.Reason = $"VENDA executada: {quantity:F4} {baseAssetSymbol} a ${slippedPrice:F2} (PnL Realizado: ${pnl:F2}, Taxa: ${fee:F2})";

            var execCost = fee + (price - slippedPrice) * quantity;
            _metrics?.AddExecutionCost(execCost);
            _metrics?.UpdatePaperPnL(pnl);

            if (pnl < 0m && usdtBalance.Free > 0m)
            {
                _metrics?.UpdateDrawdown(Math.Round(Math.Abs(pnl) / usdtBalance.Free * 100m, 2));
            }
            _metrics?.SetStrategyScore(strategy.Name, pnl > 0 ? 94.5m : 42.0m);
            _metrics?.SetAssetScore(symbol, pnl > 0 ? 91.0m : 38.0m);
        }
        else
        {
            // Hold
            audit.Reason = "Hold: nenhuma ação necessária.";
        }

        await _store.SaveDecisionAuditAsync(audit);
        return audit;
    }
}
