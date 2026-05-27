using System;
using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Application.Services;

public class RuntimeStatusService
{
    private readonly bool _binanceEnabled;
    private readonly bool _paperEnabled;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public RuntimeStatusService(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _binanceEnabled = bool.TryParse(configuration["Binance:Testnet:Enabled"], out var enabled) && enabled;
        _paperEnabled = bool.TryParse(configuration["PaperTrading:Enabled"], out var paper) || paper; // Default paper is usually true or configurable
        _apiKey = configuration["Binance:Testnet:ApiKey"] ?? string.Empty;
        _apiSecret = configuration["Binance:Testnet:ApiSecret"] ?? string.Empty;
    }

    public RuntimeStatusDto GetStatus()
    {
        var isPlaceholder = string.IsNullOrWhiteSpace(_apiKey) || 
                            _apiKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrWhiteSpace(_apiSecret) || 
                            _apiSecret.Contains("placeholder", StringComparison.OrdinalIgnoreCase);

        RuntimeMode mode;
        var warnings = new System.Collections.Generic.List<string>();

        if (_binanceEnabled)
        {
            if (isPlaceholder)
            {
                mode = RuntimeMode.TestnetDryRun;
                warnings.Add("Binance Testnet is enabled but API credentials are empty or placeholders. Falling back to TestnetDryRun.");
            }
            else
            {
                mode = RuntimeMode.TestnetReal;
            }
        }
        else if (_paperEnabled)
        {
            mode = RuntimeMode.Paper;
        }
        else
        {
            mode = RuntimeMode.Simulation;
        }

        return new RuntimeStatusDto(
            Mode: mode.ToString(),
            IsSimulation: mode == RuntimeMode.Simulation,
            IsPaper: mode == RuntimeMode.Paper,
            IsTestnet: mode == RuntimeMode.TestnetDryRun || mode == RuntimeMode.TestnetReal,
            IsRealTestnet: mode == RuntimeMode.TestnetReal,
            Warnings: warnings,
            Timestamp: DateTime.UtcNow
        );
    }
}

public record RuntimeStatusDto(
    string Mode,
    bool IsSimulation,
    bool IsPaper,
    bool IsTestnet,
    bool IsRealTestnet,
    System.Collections.Generic.List<string> Warnings,
    DateTime Timestamp
);
