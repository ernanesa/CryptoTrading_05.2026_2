using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class ModelRegistry : IModelRegistry
{
    private static readonly DateTime RegisteredAt = new(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    public IReadOnlyList<RegisteredModelInfo> GetRegisteredModels()
    {
        return new List<RegisteredModelInfo>
        {
            Create("FeatureExtractor", "feature-vector/v1", "Normaliza indicadores para contexto de inteligencia."),
            Create("RegimeDetectionService", "heuristic-m6-v1", "Detecta regime de mercado sem executar ordens."),
            Create("AnomalyDetectionService", "heuristic-m6-v1", "Pontua anomalias de volume, retorno, spread e imbalance."),
            Create("VolatilityForecastService", "volatility-heuristic-m6-v1", "Projeta risco de volatilidade em horizonte curto."),
            Create("MetaLabelingService", "meta-label-heuristic-m6-v1", "Classifica contexto direcional auxiliar."),
            Create("SentimentRiskService", "sentiment-risk-heuristic-m6-v1", "Converte sentimento proxy em filtro de risco."),
            Create("EventRiskClassifier", "event-risk-heuristic-m6-v1", "Classifica eventos derivados de stress de mercado."),
            Create("RagContextProvider", "rag-context-provider-m6-v1", "Anexa memoria tecnica local ao snapshot."),
            Create("ExplanationService", "explanation-heuristic-m6-v1", "Gera explicacoes deterministicas do snapshot.")
        };
    }

    private static RegisteredModelInfo Create(string name, string version, string purpose)
    {
        return new RegisteredModelInfo
        {
            Name = name,
            Version = version,
            Purpose = purpose,
            Source = "CryptoTrading.Application.Services",
            RegisteredAt = RegisteredAt
        };
    }
}
