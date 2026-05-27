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

builder.Services.AddSingleton<CryptoTrading.Application.Services.StrategyRegistry>();
builder.Services.AddSingleton<CryptoTrading.Contracts.Interfaces.IRiskEngine, CryptoTrading.Application.Services.RiskEngine>();
builder.Services.AddSingleton<CryptoTrading.Application.Services.PaperTradeExecutor>();
builder.Services.AddHostedService<StrategyRunnerWorker>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
