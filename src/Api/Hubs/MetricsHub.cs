using CryptoTrading.Contracts.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CryptoTrading.Api.Hubs;

public class MetricsHub : Hub
{
    private readonly IMetricsService _metrics;

    public MetricsHub(IMetricsService metrics)
    {
        _metrics = metrics;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        // Envia o snapshot inicial assim que o cliente conecta
        await Clients.Caller.SendAsync("ReceiveMetricsSnapshot", _metrics.GetSnapshot());
    }
}
