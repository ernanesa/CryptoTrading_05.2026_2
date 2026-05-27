using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Binance.Net.Clients;
using CryptoTrading.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.Application.Services;

public class OrderStatusSynchronizer
{
    private readonly IFeatureStore _store;
    private readonly bool _isEnabled;
    private readonly SecretRedactor _secretRedactor;

    public OrderStatusSynchronizer(IFeatureStore store, IConfiguration configuration, SecretRedactor? secretRedactor = null)
    {
        _store = store;
        _isEnabled = bool.TryParse(configuration["Binance:Testnet:Enabled"], out var enabled) && enabled;
        _secretRedactor = secretRedactor ?? new SecretRedactor();
    }

    public async Task<int> SynchronizeActiveOrdersAsync()
    {
        var activeOrders = await _store.GetActiveTestnetOrdersAsync();
        var orderList = activeOrders.ToList();
        
        if (!orderList.Any())
        {
            return 0;
        }

        int updatedCount = 0;

        foreach (var order in orderList)
        {
            if (!_isEnabled)
            {
                // Em Dry-Run, ordens pendentes são preenchidas instantaneamente na próxima sincronização.
                order.Status = TestnetOrderStatus.Filled.ToString();
                order.OriginalExchangeStatus = "DRY_RUN_SIMULATED_FILL";
                order.UpdatedAt = DateTime.UtcNow;
                await _store.SaveTestnetOrderAsync(order);

                await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                {
                    Symbol = order.Symbol,
                    Action = "SYNC_ORDER_DRY_RUN",
                    Status = "SUCCESS",
                    Details = $"Ordem {order.ClientOrderId} sincronizada e preenchida no modo Dry-Run."
                });

                updatedCount++;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(order.BinanceOrderId) || !long.TryParse(order.BinanceOrderId, out var binanceOrderId))
                {
                    await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                    {
                        Symbol = order.Symbol,
                        Action = "SYNC_ORDER_BINANCE_SKIPPED",
                        Status = "FAILED",
                        Details = $"Ordem {order.ClientOrderId} nao sincronizada: BinanceOrderId ausente ou invalido."
                    });

                    continue;
                }

                try
                {
                    using var client = new BinanceRestClient();
                    var result = await client.SpotApi.Trading.GetOrderAsync(order.Symbol, binanceOrderId);

                    if (!result.Success)
                    {
                        await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                        {
                            Symbol = order.Symbol,
                            Action = "SYNC_ORDER_BINANCE_FAILED",
                            Status = "FAILED",
                            Details = $"Ordem {order.ClientOrderId} nao sincronizada: {_secretRedactor.Redact(result.Error?.Message ?? "erro desconhecido da Binance")}"
                        });

                        continue;
                    }

                    var originalStatus = result.Data.Status.ToString();
                    order.Status = BinanceTestnetExecutor.MapExchangeStatus(originalStatus).ToString();
                    order.OriginalExchangeStatus = originalStatus;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _store.SaveTestnetOrderAsync(order);

                    await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                    {
                        Symbol = order.Symbol,
                        Action = "SYNC_ORDER_BINANCE",
                        Status = "SUCCESS",
                        Details = $"Ordem {order.ClientOrderId} (Binance ID: {order.BinanceOrderId}) sincronizada com status {order.Status}."
                    });

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                    {
                        Symbol = order.Symbol,
                        Action = "SYNC_ORDER_BINANCE_FAILED",
                        Status = "FAILED",
                        Details = $"Ordem {order.ClientOrderId} nao sincronizada: {_secretRedactor.Redact(ex.Message)}"
                    });
                }
            }
        }

        return updatedCount;
    }

}
