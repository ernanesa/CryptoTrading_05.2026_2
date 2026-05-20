using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Infrastructure.Persistence;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Configuração do ambiente e injeção de dependências do Core
builder.Services.AddSingleton<IFeatureStore, FeatureStore>();
builder.Services.AddSingleton<StrategyRegistry>();
builder.Services.AddTransient<BacktestEngine>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 1. Listar todas as estratégias registradas
app.MapGet("/api/strategies", (StrategyRegistry registry) =>
{
    var strategies = registry.GetAll().Select(s => new { s.Name });
    return Results.Ok(strategies);
})
.WithName("GetStrategies");

// 2. Executar backtest para uma única estratégia
app.MapGet("/api/backtest/run", async (
    string strategyName,
    string symbol,
    string interval,
    DateTime startTime,
    DateTime endTime,
    decimal? initialCapital,
    decimal? slippage,
    decimal? fee,
    IFeatureStore store,
    StrategyRegistry registry,
    BacktestEngine engine) =>
{
    var strategy = registry.Get(strategyName);
    if (strategy == null)
    {
        return Results.NotFound(new { Message = $"Estratégia '{strategyName}' não encontrada no registro." });
    }

    // Converter para UTC pois o Postgres armazena com fuso horário
    var startUtc = startTime.ToUniversalTime();
    var endUtc = endTime.ToUniversalTime();

    var dataPoints = await store.GetMarketDataPointsAsync(symbol, interval, startUtc, endUtc);
    var pointsList = dataPoints.ToList();

    if (!pointsList.Any())
    {
        return Results.BadRequest(new { Message = $"Nenhum dado com features encontrado no banco para {symbol}/{interval} entre {startUtc:yyyy-MM-dd HH:mm} e {endUtc:yyyy-MM-dd HH:mm}." });
    }

    var capital = initialCapital ?? 10000m;
    var slipModel = new PercentageSlippageModel(slippage ?? 0.0005m); // Padrão: 0.05%
    var feeModel = new BinanceSpotFeeModel(fee ?? 0.001m);            // Padrão: 0.1%

    var report = engine.Run(strategy, pointsList, capital, feeModel, slipModel);
    return Results.Ok(report);
})
.WithName("RunBacktest");

// 3. Executar backtest comparativo para todas as estratégias
app.MapGet("/api/backtest/run-all", async (
    string symbol,
    string interval,
    DateTime startTime,
    DateTime endTime,
    decimal? initialCapital,
    decimal? slippage,
    decimal? fee,
    IFeatureStore store,
    StrategyRegistry registry,
    BacktestEngine engine) =>
{
    var startUtc = startTime.ToUniversalTime();
    var endUtc = endTime.ToUniversalTime();

    var dataPoints = await store.GetMarketDataPointsAsync(symbol, interval, startUtc, endUtc);
    var pointsList = dataPoints.ToList();

    if (!pointsList.Any())
    {
        return Results.BadRequest(new { Message = $"Nenhum dado com features encontrado no banco para {symbol}/{interval} entre {startUtc:yyyy-MM-dd HH:mm} e {endUtc:yyyy-MM-dd HH:mm}." });
    }

    var capital = initialCapital ?? 10000m;
    var slipModel = new PercentageSlippageModel(slippage ?? 0.0005m);
    var feeModel = new BinanceSpotFeeModel(fee ?? 0.001m);

    var comparativeReports = new List<object>();

    foreach (var strategy in registry.GetAll())
    {
        var report = engine.Run(strategy, pointsList, capital, feeModel, slipModel);
        comparativeReports.Add(new
        {
            StrategyName = report.StrategyName,
            Symbol = report.Symbol,
            Interval = report.Interval,
            TotalTrades = report.TotalTrades,
            WinningTrades = report.WinningTrades,
            LosingTrades = report.LosingTrades,
            WinRate = report.WinRate,
            MaxDrawdownPercent = report.MaxDrawdownPercent,
            ProfitFactor = report.ProfitFactor,
            Expectancy = report.Expectancy,
            TotalFees = report.TotalFees,
            InitialCapital = report.InitialCapital,
            FinalCapital = report.FinalCapital,
            TotalPnL = report.TotalPnL,
            TotalPnLPercent = report.TotalPnLPercent
        });
    }

    return Results.Ok(comparativeReports);
})
.WithName("RunAllBacktests");

app.Run();
