using System.Text.Json;
using CryptoTrading.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Application.Services;

/// <summary>
/// Coleta features do CandleFeature e contexto de mercado (IntelligenceSnapshot)
/// para montar um dataset que servirá de treinamento offline para ML.NET / ONNX.
/// </summary>
public class DatasetBuilderService
{
    private readonly ILogger<DatasetBuilderService> _logger;
    private readonly string _datasetDirectory;

    public DatasetBuilderService(ILogger<DatasetBuilderService> logger)
    {
        _logger = logger;
        _datasetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "datasets");
        if (!Directory.Exists(_datasetDirectory))
        {
            Directory.CreateDirectory(_datasetDirectory);
        }
    }

    public async Task AppendToDatasetAsync(MarketDataPoint point, IntelligenceSnapshot intelligence, decimal targetReturn)
    {
        try
        {
            var row = new
            {
                Symbol = point.Candle.Symbol,
                Timestamp = point.Candle.OpenTime,
                Features = point.Feature,
                Regime = intelligence.MarketRegime,
                AnomalyScore = intelligence.AnomalyScore,
                TargetReturn = targetReturn // O que o modelo deveria prever (ex: retorno no próximo candle)
            };

            var json = JsonSerializer.Serialize(row);
            var filePath = Path.Combine(_datasetDirectory, $"{point.Candle.Symbol}_training_data.jsonl");

            await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append data to dataset for future ML training.");
        }
    }
}
