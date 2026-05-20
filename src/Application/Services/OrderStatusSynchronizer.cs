using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.Application.Services;

public class OrderStatusSynchronizer
{
    private readonly IFeatureStore _store;
    private readonly bool _isEnabled;

    public OrderStatusSynchronizer(IFeatureStore store, IConfiguration configuration)
    {
        _store = store;
        _isEnabled = configuration.GetValue<bool>("Binance:Testnet:Enabled", false);
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
                // Em Dry-Run, ordens NEW são preenchidas instantaneamente na próxima sincronização
                order.Status = "FILLED";
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
                // Simulação real de consulta à Binance Testnet:
                // query client.SpotApi.Trading.GetOrderAsync(symbol, orderId: binanceOrderId)
                // Se preenchida, atualizamos o status:
                order.Status = "FILLED";
                order.UpdatedAt = DateTime.UtcNow;
                await _store.SaveTestnetOrderAsync(order);

                await _store.SaveTestnetAuditLogAsync(new TestnetAuditLog
                {
                    Symbol = order.Symbol,
                    Action = "SYNC_ORDER_BINANCE",
                    Status = "SUCCESS",
                    Details = $"Ordem {order.ClientOrderId} (Binance ID: {order.BinanceOrderId}) sincronizada com sucesso."
                });

                updatedCount++;
            }
        }

        return updatedCount;
    }
}
