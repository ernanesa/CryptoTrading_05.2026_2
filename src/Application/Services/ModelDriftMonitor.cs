using CryptoTrading.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Application.Services;

/// <summary>
/// Monitora a qualidade das predições de modelos ML em tempo real e detecta concept drift (mudança de comportamento do mercado em relação ao treino).
/// </summary>
public class ModelDriftMonitor
{
    private readonly ILogger<ModelDriftMonitor> _logger;
    private readonly List<DriftRecord> _history = new();

    public ModelDriftMonitor(ILogger<ModelDriftMonitor> logger)
    {
        _logger = logger;
    }

    public void RecordPrediction(string modelName, decimal prediction, decimal actualValue)
    {
        var error = Math.Abs(prediction - actualValue);
        _history.Add(new DriftRecord
        {
            ModelName = modelName,
            Error = error,
            RecordedAt = DateTime.UtcNow
        });

        // Simulação simples de drift detection: se a média do erro na janela recente for muito alta
        var recentErrors = _history.Where(h => h.ModelName == modelName).TakeLast(50).Select(h => h.Error).ToList();
        if (recentErrors.Count >= 50)
        {
            var avgError = recentErrors.Average();
            if (avgError > 5.0m) // Limite arbitrário de degradação
            {
                _logger.LogWarning($"[DRIFT DETECTED] Model {modelName} is showing degraded performance. Average error: {avgError:F2}. Consider retraining or switching to shadow mode.");
            }
        }
    }

    private class DriftRecord
    {
        public string ModelName { get; set; } = string.Empty;
        public decimal Error { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
