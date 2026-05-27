using System.Text.Json;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class ModelRegistry : IModelRegistry
{
    private static readonly DateTime RegisteredAt = new(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _registryPath;
    private readonly IReadOnlyList<RegisteredModelInfo> _models;

    public ModelRegistry()
        : this(Path.Combine(AppContext.BaseDirectory, "model-registry.json"))
    {
    }

    public ModelRegistry(string registryPath)
    {
        if (string.IsNullOrWhiteSpace(registryPath))
        {
            throw new ArgumentException("Registry path is required.", nameof(registryPath));
        }

        _registryPath = registryPath;
        _models = (LoadModels() ?? CreateDefaultModels())
            .Select(EnsureShadowMode)
            .ToList();

        if (!File.Exists(_registryPath))
        {
            PersistModels(_models);
        }
    }

    public IReadOnlyList<RegisteredModelInfo> GetRegisteredModels()
    {
        return _models.Select(Clone).ToList();
    }

    private List<RegisteredModelInfo>? LoadModels()
    {
        try
        {
            if (!File.Exists(_registryPath))
            {
                return null;
            }

            var json = File.ReadAllText(_registryPath);
            var models = JsonSerializer.Deserialize<List<RegisteredModelInfo>>(json, JsonOptions);
            return models is { Count: > 0 }
                ? models
                : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private void PersistModels(IReadOnlyList<RegisteredModelInfo> models)
    {
        try
        {
            var directory = Path.GetDirectoryName(_registryPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(models, JsonOptions);
            File.WriteAllText(_registryPath, json);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static List<RegisteredModelInfo> CreateDefaultModels()
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
            RegisteredAt = RegisteredAt,
            IsShadowMode = true,
            IsActive = true
        };
    }

    private static RegisteredModelInfo Clone(RegisteredModelInfo model)
    {
        return new RegisteredModelInfo
        {
            Name = model.Name,
            Version = model.Version,
            Purpose = model.Purpose,
            Source = model.Source,
            RegisteredAt = model.RegisteredAt,
            IsShadowMode = model.IsShadowMode,
            IsActive = model.IsActive
        };
    }

    private static RegisteredModelInfo EnsureShadowMode(RegisteredModelInfo model)
    {
        var normalized = Clone(model);
        normalized.IsShadowMode = true;
        return normalized;
    }
}
