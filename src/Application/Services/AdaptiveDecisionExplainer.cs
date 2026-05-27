using System;
using System.Collections.Generic;
using System.Text;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class AdaptiveDecisionExplainer
{
    public string Explain(AdaptiveOrchestrationDecision decision)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Explicação de Decisão Adaptativa: {decision.Symbol}");
        sb.AppendLine();
        sb.AppendLine($"*   **Estratégia Ativa Selecionada:** {decision.ActiveStrategyName} (Score: {decision.StrategyScore:F2})");
        sb.AppendLine($"*   **Melhor Candidata Disponível:** {decision.CandidateStrategyName}");
        sb.AppendLine($"*   **Decisão de Troca (Switch):** {(decision.ShouldSwitchStrategy ? "APROVADA" : "COOLDOWN / HYSTERESIS MANTER ATIVA")}");
        sb.AppendLine($"*   **Regime de Mercado Atual:** {decision.MarketRegime}");
        sb.AppendLine($"*   **Saúde do Mercado (Market Health Score):** {decision.MarketHealthScore:F2}/100.00");
        sb.AppendLine($"*   **Score do Ativo (Asset Score):** {decision.AssetScore:F2}/100.00");
        sb.AppendLine($"*   **Tamanho do Lote (Position Size):** ${decision.PositionSize:F2} (Peso Alocação: {decision.AllocationWeight * 100m:F2}%)");
        sb.AppendLine();
        sb.AppendLine("## Divisão dos Scores por Estratégia");
        sb.AppendLine();
        sb.AppendLine("| Estratégia | Score Total | Regime Fit | Expectancy | Profit Factor | Drawdown | Custo Execução |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |");
        foreach (var score in decision.StrategyScores)
        {
            sb.AppendLine($"| {score.StrategyName} | {score.Score:F2} | {score.RegimeFitScore:F2} | {score.ExpectancyScore:F2} | {score.ProfitFactorScore:F2} | {score.DrawdownScore:F2} | {score.ExecutionCostScore:F2} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Linhas de Raciocínio (Reasons)");
        sb.AppendLine();
        foreach (var reason in decision.Reasons)
        {
            sb.AppendLine($"*   {reason}");
        }

        return sb.ToString();
    }
}
