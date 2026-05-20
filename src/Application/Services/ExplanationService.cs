using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class ExplanationService : IExplanationService
{
    public ExplanationSnapshot Explain(IntelligenceSnapshot snapshot)
    {
        var factors = new List<string>
        {
            $"Regime {snapshot.MarketRegime} com confianca {snapshot.RegimeConfidence:F2}%.",
            $"Anomalia {snapshot.AnomalyScore:F2}/100 e volatilidade {snapshot.VolatilityForecast.RiskBand}.",
            $"Meta-label {snapshot.MetaLabel.Label} com qualidade {snapshot.MetaLabel.QualityScore:F2}.",
            $"Sentimento {snapshot.SentimentRisk.RiskBand} e evento {snapshot.EventRisk.Severity}.",
            "Snapshot e apenas contexto; execucao permanece condicionada ao RiskEngine."
        };

        return new ExplanationSnapshot
        {
            Summary = $"{snapshot.Symbol}/{snapshot.Interval}: {snapshot.MarketRegime}, "
                + $"volatilidade {snapshot.VolatilityForecast.RiskBand}, "
                + $"sentimento {snapshot.SentimentRisk.RiskBand}.",
            Factors = factors
        };
    }
}
