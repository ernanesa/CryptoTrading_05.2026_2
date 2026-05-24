using Binance.Net;
using Binance.Net.Objects;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Application.Services;

public class BinanceTestnetExecutor
{
    private readonly IFeatureStore _store;
    private readonly ExchangeRuleValidator _validator;
    private readonly ILogger<BinanceTestnetExecutor> _logger;
    private readonly IMetricsService? _metrics;
    private readonly bool _isEnabled;
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public BinanceTestnetExecutor(
        IFeatureStore store,
        ExchangeRuleValidator validator,
        IConfiguration configuration,
        ILogger<BinanceTestnetExecutor> _loggerInst,
        IMetricsService? metrics = null)
    {
        _store = store;
        _validator = validator;
        _logger = _loggerInst;
        _metrics = metrics;

        // Configuracoes lidas sem ConfigurationBinder para manter compatibilidade Native AOT.
        _isEnabled = bool.TryParse(configuration["Binance:Testnet:Enabled"], out var enabled) && enabled;
        _apiKey = configuration["Binance:Testnet:ApiKey"] ?? "placeholder_key";
        _apiSecret = configuration["Binance:Testnet:ApiSecret"] ?? "placeholder_secret";

        if (_isEnabled && !_apiKey.Contains("placeholder") && !string.IsNullOrWhiteSpace(_apiKey))
        {
            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.Environment = BinanceEnvironment.Testnet;
                options.ApiCredentials = new Binance.Net.BinanceCredentials(_apiKey, _apiSecret);
            });
        }
    }

    public bool IsEnabled => _isEnabled;

    public async Task<TestnetOrder> ExecuteOrderAsync(TestnetOrder order)
    {
        _metrics?.IncrementTestnetRequests();

        // 1. Gravar auditoria inicial
        var auditInit = new TestnetAuditLog
        {
            Symbol = order.Symbol,
            Action = $"SUBMIT_ORDER_{order.Side}_{order.Type}",
            Status = "NEW",
            Details = $"Processando ordem de {order.Quantity} {order.Symbol} a ${order.Price}. Modo Real: {_isEnabled}"
        };
        await _store.SaveTestnetAuditLogAsync(auditInit);

        // 2. Obter regras de filtro para validação local
        var filters = await _store.GetExchangeFilterInfoAsync(order.Symbol);
        if (filters == null)
        {
            // Criar filtros padrão robustos caso não existam no banco de dados para evitar travar a execução
            filters = new ExchangeFilterInfo
            {
                Symbol = order.Symbol,
                TickSize = 0.01m,
                StepSize = 0.0001m,
                MinQty = 0.0001m,
                MaxQty = 1000m,
                MinNotional = 5.0m,
                PricePrecision = 2,
                QuantityPrecision = 4
            };
            await _store.SaveExchangeFilterInfoAsync(filters);
        }

        // 3. Validação local antes do envio
        var validation = _validator.ValidateOrder(order, filters);
        if (!validation.IsValid)
        {
            order.Status = "REJECTED";
            order.UpdatedAt = DateTime.UtcNow;
            await _store.SaveTestnetOrderAsync(order);

            await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
            {
                Symbol = order.Symbol,
                Action = "VALIDATION_FAILED",
                Status = "FAILED",
                Details = $"Ordem rejeitada localmente: {validation.Message}"
            });

            return order;
        }

        // 4. Fluxo Simulado / Dry-Run (se Testnet estiver desabilitada)
        if (!_isEnabled)
        {
            order.Status = "FILLED"; // Em simulação de dry-run assume preenchimento instantâneo
            order.BinanceOrderId = $"MOCK_BINANCE_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            order.UpdatedAt = DateTime.UtcNow;
            await _store.SaveTestnetOrderAsync(order);

            await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
            {
                Symbol = order.Symbol,
                Action = "DRY_RUN_EXECUTION",
                Status = "SUCCESS",
                Details = $"Ordem executada em modo Dry-Run. ID Binance fictício: {order.BinanceOrderId}"
            });

            return order;
        }

        // 5. Fluxo Real via API Binance Testnet
        try
        {
            // Proteção estrita de secrets: mascarar credenciais nos logs
            var maskedKey = _apiKey.Length > 6 ? $"{_apiKey.Substring(0, 4)}...{_apiKey.Substring(_apiKey.Length - 2)}" : "***";
            _logger.LogInformation("Conectando à Binance Spot Testnet com API Key {ApiKey}", maskedKey);

            if (_apiKey.Contains("placeholder") || string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception("Credenciais da Binance Testnet são fictícias ou inválidas.");
            }

            using var client = new BinanceRestClient();
            var side = order.Side.Equals("BUY", StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell;
            var orderType = order.Type.Equals("MARKET", StringComparison.OrdinalIgnoreCase) ? SpotOrderType.Market : SpotOrderType.Limit;
            
            var result = await client.SpotApi.Trading.PlaceOrderAsync(
                symbol: order.Symbol,
                side: side,
                type: orderType,
                quantity: order.Quantity,
                price: orderType == SpotOrderType.Limit ? order.Price : null,
                timeInForce: orderType == SpotOrderType.Limit ? TimeInForce.GoodTillCanceled : null
            );

            if (!result.Success)
            {
                throw new Exception($"Erro da API Binance: {result.Error?.Message}");
            }

            order.Status = "FILLED"; // Para simplificar o teste, assumimos preenchido ou atualizar via status check dps
            order.BinanceOrderId = result.Data.Id.ToString();
            order.UpdatedAt = DateTime.UtcNow;
            await _store.SaveTestnetOrderAsync(order);

            await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
            {
                Symbol = order.Symbol,
                Action = "BINANCE_TESTNET_EXECUTION",
                Status = "SUCCESS",
                Details = $"Ordem executada com sucesso na Binance Testnet. ID: {order.BinanceOrderId}"
            });
        }
        catch (Exception ex)
        {
            order.Status = "REJECTED";
            order.UpdatedAt = DateTime.UtcNow;
            await _store.SaveTestnetOrderAsync(order);

            // Garantimos que a mensagem de erro não exponha o segredo bruto caso esteja em stacktraces
            var safeMessage = ex.Message.Replace(_apiSecret, "******").Replace(_apiKey, "******");

            await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
            {
                Symbol = order.Symbol,
                Action = "BINANCE_TESTNET_FAILED",
                Status = "FAILED",
                Details = $"Falha de execução na Binance Testnet: {safeMessage}"
            });

            _logger.LogError("Erro de comunicação com a Binance Testnet: {Message}", safeMessage);
        }

        return order;
    }

    public async Task<TestnetOrder?> GetOrderStatusAsync(string symbol, string binanceOrderId)
    {
        if (!_isEnabled) return null;
        using var client = new BinanceRestClient();
        var result = await client.SpotApi.Trading.GetOrderAsync(symbol, long.Parse(binanceOrderId));
        if (!result.Success) return null;

        return new TestnetOrder
        {
            Symbol = result.Data.Symbol,
            BinanceOrderId = result.Data.Id.ToString(),
            Status = result.Data.Status.ToString().ToUpper(),
            Price = result.Data.Price,
            Quantity = result.Data.Quantity,
            Side = result.Data.Side.ToString().ToUpper(),
            Type = result.Data.Type.ToString().ToUpper()
        };
    }

    public async Task SyncOpenOrdersAsync(string symbol)
    {
        if (!_isEnabled) return;
        using var client = new BinanceRestClient();
        var result = await client.SpotApi.Trading.GetOpenOrdersAsync(symbol);
        if (!result.Success) return;

        foreach (var openOrder in result.Data)
        {
            // Na vida real, devemos buscar a ordem local pelo BinanceOrderId e atualizá-la
            // Simulação de audit log para sincronização:
            await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
            {
                Symbol = symbol,
                Action = "SYNC_OPEN_ORDER",
                Status = "SUCCESS",
                Details = $"Ordem Aberta na Exchange: {openOrder.Id} Status: {openOrder.Status}"
            });
        }
    }

    public async Task<IEnumerable<WalletBalance>> GetAccountSnapshotAsync()
    {
        if (!_isEnabled) return [];
        using var client = new BinanceRestClient();
        var result = await client.SpotApi.Account.GetAccountInfoAsync();
        if (!result.Success) return [];

        return result.Data.Balances.Select(b => new WalletBalance
        {
            Symbol = b.Asset,
            Free = b.Available,
            Locked = b.Locked,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
