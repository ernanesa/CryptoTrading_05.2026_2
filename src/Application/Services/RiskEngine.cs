using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class RiskEngine : IRiskEngine
{
    private const decimal MaxDailyLossLimit = 200m;       // Perda diária máxima de $200 USD
    private const decimal MaxDrawdownPercent = 5.0m;      // Max Drawdown permitido de 5%
    private const decimal MaxAssetExposurePercent = 50.0m; // Exposição máxima por ativo de 50%
    private const decimal MaxTotalExposurePercent = 90.0m; // Exposição total máxima de 90%
    private const int MaxOpenAssets = 5;                   // Máximo de 5 ativos em carteira simultaneamente
    private const decimal MaxSpreadPercent = 1.0m;         // Spread máximo permitido de 1%
    private const int CooldownLossCount = 3;               // Cooldown após 3 perdas seguidas
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromHours(1);

    public RiskValidationResult ValidateSignal(
        TradeSignal signal,
        decimal price,
        decimal spread,
        IEnumerable<WalletBalance> balances,
        IEnumerable<PaperTrade> recentTrades,
        RiskStatus currentStatus)
    {
        // 1. Verificar se o sistema está em modo Halted (bloqueado)
        if (currentStatus == RiskStatus.Halted)
        {
            return RiskValidationResult.Reject("Negociador pausado devido a regras críticas de risco (Halted Mode).", RiskStatus.Halted);
        }

        // 2. Sinais do tipo Hold ou Exit não aumentam o risco e são sempre aprovados pelo RiskEngine
        if (signal.Type == TradeSignalType.Hold || signal.Type == TradeSignalType.Exit)
        {
            return RiskValidationResult.Approve(currentStatus);
        }

        // 3. Validação de Spread Máximo
        if (price > 0m)
        {
            var spreadPercent = (spread / price) * 100m;
            if (spreadPercent > MaxSpreadPercent)
            {
                return RiskValidationResult.Reject($"Spread muito alto: {spreadPercent:F2}% (Limite: {MaxSpreadPercent}%)", currentStatus);
            }
        }

        // 4. Validação de Perda Diária Máxima (realizada hoje)
        var today = DateTime.UtcNow.Date;
        var todayTrades = recentTrades.Where(t => t.ExecutedAt.Date == today).ToList();
        var dailyPnL = todayTrades.Sum(t => t.PnL);
        if (dailyPnL < -MaxDailyLossLimit)
        {
            return RiskValidationResult.Reject($"Perda diária excedeu o limite: ${Math.Abs(dailyPnL):F2} (Limite: ${MaxDailyLossLimit}). Sistema interrompido.", RiskStatus.Halted);
        }

        // 5. Cooldown após sequência de perdas
        if (recentTrades.Count() >= CooldownLossCount)
        {
            var lastTrades = recentTrades.OrderByDescending(t => t.ExecutedAt).Take(CooldownLossCount).ToList();
            var allLosses = lastTrades.All(t => t.PnL < 0m);
            if (allLosses)
            {
                var mostRecentTrade = lastTrades.First();
                var timeSinceLastTrade = DateTime.UtcNow - mostRecentTrade.ExecutedAt;
                if (timeSinceLastTrade < CooldownDuration)
                {
                    var timeRemaining = CooldownDuration - timeSinceLastTrade;
                    return RiskValidationResult.Reject($"Sistema em cooldown após {CooldownLossCount} perdas seguidas. Tempo restante: {timeRemaining.Minutes}m {timeRemaining.Seconds}s.", currentStatus);
                }
            }
        }

        // 6. Cálculo do valor total da carteira (USDT + valor de mercado dos outros ativos)
        var balancesList = balances.ToList();
        var usdtBalance = balancesList.FirstOrDefault(b => b.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase));
        decimal usdtFree = usdtBalance?.Free ?? 0m;
        
        // Simular valor total para cálculo de exposição
        decimal targetAssetValue = 0m;
        decimal otherAssetsValue = 0m;

        foreach (var balance in balancesList)
        {
            if (balance.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase)) continue;

            // Se for o ativo do sinal
            if (signal.Symbol.StartsWith(balance.Symbol, StringComparison.OrdinalIgnoreCase))
            {
                targetAssetValue = balance.Free * price;
            }
            else
            {
                // Para simplificar a exposição, consideramos o saldo livre * preço atual fictício
                otherAssetsValue += balance.Free * price;
            }
        }

        decimal totalPortfolioValue = usdtFree + targetAssetValue + otherAssetsValue;
        if (totalPortfolioValue <= 0m)
        {
            return RiskValidationResult.Reject("Valor do portfólio zerado ou inválido.", currentStatus);
        }

        // 7. Validação de Exposição Máxima por Ativo (limite de 50% do portfólio por moeda)
        // Se formos comprar o ativo, o novo valor estimado seria o valor atual + o tamanho da ordem (estimado como 98% do USDT livre)
        if (signal.Type == TradeSignalType.Buy)
        {
            var estimatedOrderSize = usdtFree * 0.98m;
            var newTargetAssetValue = targetAssetValue + estimatedOrderSize;
            var assetExposure = (newTargetAssetValue / totalPortfolioValue) * 100m;

            if (assetExposure > MaxAssetExposurePercent)
            {
                return RiskValidationResult.Reject($"Exposição estimada ao ativo ({assetExposure:F2}%) excederia o limite de {MaxAssetExposurePercent}%.", currentStatus);
            }

            // 8. Validação de Exposição Total (ativos de risco não podem ultrapassar 90% do portfólio)
            var newTotalExposure = ((targetAssetValue + otherAssetsValue + estimatedOrderSize) / totalPortfolioValue) * 100m;
            if (newTotalExposure > MaxTotalExposurePercent)
            {
                return RiskValidationResult.Reject($"Exposição total estimada ({newTotalExposure:F2}%) excederia o limite de {MaxTotalExposurePercent}%.", currentStatus);
            }

            // 9. Validação do Máximo de Ativos Abertos
            var activeAssetsCount = balancesList.Count(b => !b.Symbol.Equals("USDT", StringComparison.OrdinalIgnoreCase) && b.Free > 0m);
            var isNewAsset = !balancesList.Any(b => signal.Symbol.StartsWith(b.Symbol, StringComparison.OrdinalIgnoreCase) && b.Free > 0m);
            if (isNewAsset && activeAssetsCount >= MaxOpenAssets)
            {
                return RiskValidationResult.Reject($"Número máximo de ativos abertos atingido: {MaxOpenAssets}", currentStatus);
            }
        }

        return RiskValidationResult.Approve(currentStatus);
    }
}
