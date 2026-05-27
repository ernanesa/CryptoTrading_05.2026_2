using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Infrastructure.Adapters;
using CryptoTrading.Infrastructure.Persistence;
using CryptoTrading.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Registrar serviços no container de injeção de dependência
builder.Services.AddSingleton<IMarketDataAdapter, BinanceMarketDataAdapter>();
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=cryptotrading;Username=postgres;Password=postgres";
builder.Services.AddSingleton(Npgsql.NpgsqlDataSource.Create(connStr));
builder.Services.AddSingleton<IFeatureStore, FeatureStore>();
//
//

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
