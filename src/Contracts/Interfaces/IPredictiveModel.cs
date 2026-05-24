using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

/// <summary>
/// Contrato unificado para inferência de modelos (ML.NET, ONNX ou Heurísticos).
/// Todos os modelos de ML não executam ações diretamente, apenas retornam predições como parte do IntelligenceSnapshot.
/// </summary>
public interface IPredictiveModel
{
    string ModelName { get; }
    string Version { get; }
    
    /// <summary>
    /// Retorna true se o modelo estiver em Shadow Mode (calcula e loga a predição, mas não afeta decisões do RiskEngine/Orchestrator).
    /// </summary>
    bool IsShadowMode { get; }

    /// <summary>
    /// Realiza inferência usando as features do candle atual e contexto de mercado.
    /// </summary>
    Task<ModelPredictionResult> PredictAsync(MarketDataPoint currentPoint, IntelligenceSnapshot context);
}

public class ModelPredictionResult
{
    public decimal PredictionValue { get; set; }
    public decimal ConfidenceScore { get; set; }
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
}
