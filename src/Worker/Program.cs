using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Infrastructure.Adapters;
using CryptoTrading.Infrastructure.Persistence;
using CryptoTrading.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Registrar serviços no container de injeção de dependência
builder.Services.AddSingleton<IMarketDataAdapter, BinanceMarketDataAdapter>();
builder.Services.AddSingleton<IFeatureStore>(sp =>
    new FeatureStore(sp.GetRequiredService<IConfiguration>()));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
