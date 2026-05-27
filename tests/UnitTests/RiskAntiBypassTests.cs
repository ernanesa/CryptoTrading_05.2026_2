using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Application.Services;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using Xunit;

namespace CryptoTrading.UnitTests;

public class RiskAntiBypassTests
{
    private readonly RiskEngine _riskEngine = new();

    [Fact]
    public void ValidateSignal_HaltedStatus_MustRejectAllNewTrades()
    {
        var buySignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Buy };
        var sellSignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Sell };

        var balances = new List<WalletBalance>
        {
            new WalletBalance { Symbol = "USDT", Free = 10000m }
        };

        var resultBuy = _riskEngine.ValidateSignal(buySignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);
        var resultSell = _riskEngine.ValidateSignal(sellSignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);

        // Nenhuma ação de compra/venda pode bypassar o estado Halted
        Assert.False(resultBuy.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultBuy.NewStatus);

        Assert.False(resultSell.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultSell.NewStatus);
        
        // E o sinal de Exit também é bloqueado pois o check Halted é prioritário no início do método
        var exitSignal = new TradeSignal { Symbol = "BTCUSDT", Type = TradeSignalType.Exit };
        var resultExit = _riskEngine.ValidateSignal(exitSignal, 50000m, 5m, balances, Array.Empty<PaperTrade>(), RiskStatus.Halted);
        Assert.False(resultExit.IsApproved);
        Assert.Equal(RiskStatus.Halted, resultExit.NewStatus);
    }
}
