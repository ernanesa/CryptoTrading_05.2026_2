using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Domain.Services;

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

        // 3. Carregar estados do banco de dados (carteira e posição ativa)
        var balances = (await _store.GetWalletBalancesAsync()).ToList();
        var activePosition = await _store.GetActivePaperPositionAsync(symbol);
        var recentTrades = (await _store.GetPaperTradesAsync(symbol, 10)).ToList();

        // 3.1 Checar gatilhos de Stop-Loss ou Take-Profit antes da estratégia
        if (activePosition != null && activePosition.Type == PositionType.Long)
        {
            bool triggerExit = false;
            string triggerReason = "";

            if (activePosition.StopLossPrice.HasValue && currentPoint.Candle.Low <= activePosition.StopLossPrice.Value)
            {
                triggerExit = true;
                triggerReason = $"Stop Loss atingido (Low: {currentPoint.Candle.Low:F2} <= SL: {activePosition.StopLossPrice.Value:F2})";
                price = activePosition.StopLossPrice.Value; // Execute at SL price
            }
            else if (activePosition.TakeProfitPrice.HasValue && currentPoint.Candle.High >= activePosition.TakeProfitPrice.Value)
            {
                triggerExit = true;
                triggerReason = $"Take Profit atingido (High: {currentPoint.Candle.High:F2} >= TP: {activePosition.TakeProfitPrice.Value:F2})";
                price = activePosition.TakeProfitPrice.Value; // Execute at TP price
            }

            if (triggerExit)
            {
                // Sobrescrever sinal da estratégia para forçar saída
                signal = new TradeSignal
                {
                    Symbol = symbol,
                    Type = TradeSignalType.Exit,
                    Timestamp = currentPoint.Candle.OpenTime,
                    Description = triggerReason
                };
            }
        }

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

        // 5. Fluxo de criação de ordens
        if (signal.Type == TradeSignalType.Buy)
        {
            if (usdtBalance.Free < 10m)
            {
                audit.Decision = "REJECTED";
                audit.Reason = $"Saldo insuficiente de USDT para compra: ${usdtBalance.Free:F2}";
                _metrics?.IncrementRiskRejections();
                await _store.SaveDecisionAuditAsync(audit);
                return audit;
            }

            var allocateAmount = usdtBalance.Free * 0.98m;
            var quantity = allocateAmount / price;
            
            var order = new PaperOrder
            {
                Symbol = symbol,
                ClientOrderId = Guid.NewGuid().ToString("N"),
                Side = "BUY",
                Type = OrderType.Market,
                Price = price,
                Quantity = quantity,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow
            };
            
            await _store.SavePaperOrderAsync(order);
            await _store.SavePaperOrderEventAsync(PaperOrderStateMachine.Created(order, order.CreatedAt));
            audit.Reason = $"ORDEM CADASTRADA (COMPRA): {quantity:F4} {baseAssetSymbol} a Mercado";
        }
        else if (signal.Type == TradeSignalType.Exit || signal.Type == TradeSignalType.Sell)
        {
            if (assetBalance.Free <= 0.00001m)
            {
                audit.Decision = "REJECTED";
                audit.Reason = $"Sem saldo disponível de {baseAssetSymbol} para venda.";
                _metrics?.IncrementRiskRejections();
                await _store.SaveDecisionAuditAsync(audit);
                return audit;
            }

            var quantity = assetBalance.Free;
            var order = new PaperOrder
            {
                Symbol = symbol,
                ClientOrderId = Guid.NewGuid().ToString("N"),
                Side = "SELL",
                Type = OrderType.Market,
                Price = price,
                Quantity = quantity,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow
            };
            
            await _store.SavePaperOrderAsync(order);
            await _store.SavePaperOrderEventAsync(PaperOrderStateMachine.Created(order, order.CreatedAt));
            audit.Reason = $"ORDEM CADASTRADA (VENDA): {quantity:F4} {baseAssetSymbol} a Mercado";
        }
        else
        {
            audit.Reason = "Hold: nenhuma ação necessária.";
        }
        
        // Loop de reconciliação
        await ReconcileOrdersAsync(currentPoint, balances, activePosition);
        
        await _store.SaveDecisionAuditAsync(audit);
        return audit;
    }
    
    private async Task ReconcileOrdersAsync(MarketDataPoint point, List<WalletBalance> balances, Position? activePosition)
    {
        var symbol = point.Candle.Symbol;
        var orders = (await _store.GetActivePaperOrdersAsync(symbol)).ToList();
        
        decimal availableLiquidity = point.Candle.Volume * 0.01m; // Simula 1% do volume da barra como liquidez acessível
        if (availableLiquidity <= 0) availableLiquidity = 1m;

        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Expired || order.Status == OrderStatus.Filled)
                continue;

            if (order.Status == OrderStatus.New)
            {
                var acceptedAt = DateTime.UtcNow;
                var acceptedEvent = PaperOrderStateMachine.Activate(order, acceptedAt);
                await _store.SavePaperOrderAsync(order);
                await _store.SavePaperOrderEventAsync(acceptedEvent);
            }

            // Simple limit order matching:
            bool canFill = false;
            decimal fillPrice = order.Price;

            if (order.Type == OrderType.Market)
            {
                canFill = true;
                // Add slippage using spread
                decimal slippage = point.Feature.Spread > 0 ? point.Feature.Spread * 0.5m : point.Candle.Close * 0.0005m;
                fillPrice = order.Side == "BUY" ? point.Candle.Close + slippage : point.Candle.Close - slippage;
            }
            else // Limit
            {
                if (order.Side == "BUY" && point.Candle.Low <= order.Price)
                {
                    canFill = true;
                    fillPrice = order.Price;
                }
                else if (order.Side == "SELL" && point.Candle.High >= order.Price)
                {
                    canFill = true;
                    fillPrice = order.Price;
                }
            }

            if (canFill)
            {
                decimal fillQty = Math.Min(order.RemainingQuantity, availableLiquidity);
                if (fillQty <= 0)
                    continue;

                availableLiquidity -= fillQty;

                decimal fillValue = fillQty * fillPrice;
                decimal fee = fillValue * 0.001m; // 0.1% fee

                var realizedBeforeFill = activePosition?.RealizedPnL ?? 0m;

                var fillEvent = PaperOrderStateMachine.ApplyFill(order, fillQty, fillPrice, fee, DateTime.UtcNow);

                await _store.SavePaperOrderAsync(order);
                await _store.SavePaperOrderEventAsync(fillEvent);

                // Update Position and Wallet
                var usdtBalance = balances.First(b => b.Symbol == "USDT");
                var assetSymbol = symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase)
                    ? symbol[..^4]
                    : symbol;
                var assetBalance = balances.First(b => b.Symbol.Equals(assetSymbol, StringComparison.OrdinalIgnoreCase));

                if (order.Side.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                {
                    usdtBalance.Free -= (fillValue + fee);
                    assetBalance.Free += fillQty;

                    if (activePosition == null || activePosition.IsClosed)
                    {
                        activePosition = new Position
                        {
                            Symbol = symbol,
                            Type = PositionType.Long,
                            EntryPrice = fillPrice,
                            Quantity = fillQty,
                            EntryTime = DateTime.UtcNow,
                            FeesPaid = fee,
                            State = PositionState.Open
                        };
                    }
                    else
                    {
                        var totalCost = (activePosition.EntryPrice * activePosition.Quantity) + (fillPrice * fillQty);
                        activePosition.Quantity += fillQty;
                        activePosition.EntryPrice = totalCost / activePosition.Quantity;
                        activePosition.FeesPaid += fee;
                    }
                }
                else // SELL
                {
                    usdtBalance.Free += (fillValue - fee);
                    assetBalance.Free -= fillQty;
                    if (Math.Abs(assetBalance.Free) <= 0.00000001m)
                        assetBalance.Free = 0m;

                    if (activePosition != null && !activePosition.IsClosed)
                    {
                        activePosition.PartiallyClose(fillPrice, fillQty, fee);
                        if (activePosition.IsClosed || activePosition.Quantity <= 0.00000001m)
                        {
                            activePosition.Quantity = 0m;
                            assetBalance.Free = 0m;
                        }
                    }
                }

                await _store.SaveWalletBalanceAsync(usdtBalance);
                await _store.SaveWalletBalanceAsync(assetBalance);
                if (activePosition != null)
                {
                    activePosition.UpdateUnrealizedPnL(point.Candle.Close);
                    await _store.SavePaperPositionAsync(activePosition);
                }

                // Register Trade
                var trade = new PaperTrade
                {
                    Symbol = symbol,
                    Type = order.Side,
                    Price = fillPrice,
                    Quantity = fillQty,
                    Fee = fee,
                    PnL = order.Side.Equals("SELL", StringComparison.OrdinalIgnoreCase) && activePosition != null
                        ? activePosition.RealizedPnL - realizedBeforeFill
                        : 0m,
                    ExecutedAt = DateTime.UtcNow
                };
                await _store.SavePaperTradeAsync(trade);
            }
        }
    }
}
