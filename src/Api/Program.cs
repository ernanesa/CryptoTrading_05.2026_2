using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Infrastructure.Persistence;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Api.Hubs;
using CryptoTrading.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuração de CORS para o painel frontend se conectar via WebSocket/SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Requerido pelo SignalR para transporte de websockets
    });
});

// Configuração do ambiente e injeção de dependências do Core
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<IFeatureStore, FeatureStore>();
builder.Services.AddSingleton<StrategyRegistry>();
builder.Services.AddTransient<BacktestEngine>();
builder.Services.AddSingleton<IRiskEngine, RiskEngine>();
builder.Services.AddSingleton<IRegimeDetectionService, RegimeDetectionService>();
builder.Services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();
builder.Services.AddSingleton<IFeatureExtractor, FeatureExtractor>();
builder.Services.AddSingleton<IVolatilityForecastService, VolatilityForecastService>();
builder.Services.AddSingleton<IMetaLabelingService, MetaLabelingService>();
builder.Services.AddSingleton<IEventRiskClassifier, EventRiskClassifier>();
builder.Services.AddSingleton<ISentimentRiskService, SentimentRiskService>();
builder.Services.AddSingleton<IModelRegistry, ModelRegistry>();
builder.Services.AddSingleton<IRagContextProvider, RagContextProvider>();
builder.Services.AddSingleton<IExplanationService, ExplanationService>();
builder.Services.AddSingleton<IIntelligenceSnapshotService, IntelligenceSnapshotService>();
builder.Services.AddSingleton<MarketRegimeService>();
builder.Services.AddSingleton<MarketHealthScore>();
builder.Services.AddSingleton<ExecutionCostModel>();
builder.Services.AddSingleton<AssetRankingService>();
builder.Services.AddSingleton<StrategyPerformanceTracker>();
builder.Services.AddSingleton<StrategyScoringService>();
builder.Services.AddSingleton<StrategyHealthMonitor>();
builder.Services.AddSingleton<MultiArmedBanditAllocator>();
builder.Services.AddSingleton<AdaptivePortfolioAllocator>();
builder.Services.AddSingleton<DynamicPositionSizingService>();
builder.Services.AddSingleton<DynamicExitEngine>();
builder.Services.AddSingleton<TradeAttributionService>();
builder.Services.AddSingleton<WalkForwardEvaluator>();
builder.Services.AddSingleton<AdaptiveStrategyOrchestrator>();
builder.Services.AddTransient<PaperTradeExecutor>();
builder.Services.AddSingleton<ExchangeRuleValidator>();
builder.Services.AddTransient<BinanceTestnetExecutor>();
builder.Services.AddTransient<OrderStatusSynchronizer>();

builder.Services.AddSignalR();
builder.Services.AddHostedService<MetricsBroadcaster>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Mapeamento do Hub de métricas do SignalR
app.MapHub<MetricsHub>("/hubs/metrics");

// Endpoints REST de Observabilidade e Saúde
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/metrics", (IMetricsService metrics) => Results.Ok(metrics.GetSnapshot()))
    .WithName("GetMetricsSnapshot");

app.MapGet("/api/intelligence/snapshot", async (
    string symbol,
    string interval,
    IFeatureStore store,
    IIntelligenceSnapshotService intelligence,
    int windowHours = 48) =>
{
    var endUtc = DateTime.UtcNow;
    var startUtc = endUtc.AddHours(-Math.Clamp(windowHours, 1, 720));
    var points = await store.GetMarketDataPointsAsync(symbol, interval, startUtc, endUtc);
    var features = points
        .Select(p => p.Feature)
        .Where(f => f != null)
        .OrderBy(f => f.OpenTime)
        .ToList();

    if (features.Count == 0)
    {
        return Results.NotFound(new { Message = $"Nenhuma feature encontrada para {symbol}/{interval} nas ultimas {windowHours}h." });
    }

    var snapshot = intelligence.CreateSnapshot(symbol, interval, features);
    return Results.Ok(snapshot);
})
.WithName("GetIntelligenceSnapshot");

app.MapGet("/api/adaptive/recommendation", async (
    string symbol,
    string interval,
    IFeatureStore store,
    StrategyRegistry registry,
    IIntelligenceSnapshotService intelligence,
    AdaptiveStrategyOrchestrator orchestrator,
    string? currentStrategyName,
    int persistentAdvantageCycles = 2,
    int windowHours = 48,
    decimal portfolioValue = 10000m) =>
{
    var endUtc = DateTime.UtcNow;
    var startUtc = endUtc.AddHours(-Math.Clamp(windowHours, 1, 720));
    var points = await store.GetMarketDataPointsAsync(symbol, interval, startUtc, endUtc);
    var features = points
        .Select(p => p.Feature)
        .Where(f => f != null)
        .OrderBy(f => f.OpenTime)
        .ToList();

    if (features.Count == 0)
    {
        return Results.NotFound(new { Message = $"Nenhuma feature encontrada para orquestracao adaptativa de {symbol}/{interval}." });
    }

    var snapshot = intelligence.CreateSnapshot(symbol, interval, features);
    var request = new AdaptiveOrchestrationRequest
    {
        Symbol = symbol,
        Interval = interval,
        Intelligence = snapshot,
        StrategyNames = registry.GetAll().Select(s => s.Name).ToList(),
        CurrentStrategyName = currentStrategyName,
        PersistentAdvantageCycles = persistentAdvantageCycles,
        PortfolioValue = portfolioValue,
        RiskStatus = RiskStatus.Normal,
        DataQualityPassed = true
    };

    return Results.Ok(orchestrator.Decide(request));
})
.WithName("GetAdaptiveRecommendation");

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

// 4. Paper Trading — Estado da carteira virtual
app.MapGet("/api/paper/wallet", async (IFeatureStore store) =>
{
    var balances = await store.GetWalletBalancesAsync();
    return Results.Ok(balances);
})
.WithName("GetPaperWallet");

// 5. Paper Trading — Histórico de trades simulados
app.MapGet("/api/paper/trades", async (string symbol, IFeatureStore store, int limit = 50) =>
{
    var trades = await store.GetPaperTradesAsync(symbol, limit);
    return Results.Ok(trades);
})
.WithName("GetPaperTrades");

// 6. Paper Trading — Histórico de auditorias do RiskEngine
app.MapGet("/api/paper/audits", async (IFeatureStore store, int limit = 100) =>
{
    var audits = await store.GetDecisionAuditsAsync(limit);
    return Results.Ok(audits);
})
.WithName("GetDecisionAudits");

// 7. Paper Trading — Resetar simulação (volta ao estado inicial com $10.000 USDT)
app.MapDelete("/api/paper/reset", async (IFeatureStore store) =>
{
    await store.ClearPaperTradingDataAsync();
    return Results.Ok(new { Message = "Simulação de Paper Trading reinicializada. Carteira: $10.000 USDT." });
})
.WithName("ResetPaperTrading");

// 8. Paper Trading — Disparar um sinal manualmente para uma estratégia em tempo real
app.MapPost("/api/paper/process-signal", async (
    string strategyName,
    string symbol,
    string interval,
    IFeatureStore store,
    StrategyRegistry registry,
    PaperTradeExecutor executor) =>
{
    var strategy = registry.Get(strategyName);
    if (strategy == null)
    {
        return Results.NotFound(new { Message = $"Estratégia '{strategyName}' não encontrada." });
    }

    var now = DateTime.UtcNow;
    var dataPoints = await store.GetMarketDataPointsAsync(symbol, interval, now.AddHours(-2), now);
    var latest = dataPoints.MaxBy(d => d.Candle.OpenTime);

    if (latest == null)
    {
        return Results.BadRequest(new { Message = $"Nenhum dado recente encontrado no banco para {symbol}/{interval}." });
    }

    var audit = await executor.ProcessSignalAsync(strategy, latest);
    return Results.Ok(audit);
})
.WithName("ProcessPaperSignal");

// 9. Binance Testnet — Criar ou atualizar filtros da exchange para validação local
app.MapPost("/api/testnet/filters", async (ExchangeFilterInfo filter, IFeatureStore store) =>
{
    await store.SaveExchangeFilterInfoAsync(filter);
    return Results.Ok(new { Message = $"Regras de filtro para {filter.Symbol} atualizadas com sucesso.", Filter = filter });
})
.WithName("SaveExchangeFilters");

// 10. Binance Testnet — Obter regras de filtro de um par
app.MapGet("/api/testnet/filters/{symbol}", async (string symbol, IFeatureStore store) =>
{
    var filter = await store.GetExchangeFilterInfoAsync(symbol);
    return filter != null ? Results.Ok(filter) : Results.NotFound(new { Message = $"Filtro para {symbol} não encontrado." });
})
.WithName("GetExchangeFilters");

// 11. Binance Testnet — Enviar uma ordem de trade em sandbox (ou real se habilitado)
app.MapPost("/api/testnet/order", async (TestnetOrder order, BinanceTestnetExecutor executor) =>
{
    order.ClientOrderId = $"CLIENT_{Guid.NewGuid().ToString().Substring(0, 10).ToUpper()}";
    order.CreatedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;

    var result = await executor.ExecuteOrderAsync(order);
    return result.Status == "REJECTED" 
        ? Results.BadRequest(new { Message = "Ordem rejeitada.", Result = result })
        : Results.Ok(result);
})
.WithName("SubmitTestnetOrder");

// 12. Binance Testnet — Sincronizar ordens abertas (NEW ou PARTIALLY_FILLED)
app.MapPost("/api/testnet/sync", async (OrderStatusSynchronizer synchronizer) =>
{
    int updatedCount = await synchronizer.SynchronizeActiveOrdersAsync();
    return Results.Ok(new { Message = $"Sincronização concluída com sucesso.", OrdersUpdated = updatedCount });
})
.WithName("SyncTestnetOrders");

// 13. Binance Testnet — Obter histórico de auditoria de conexões/ordens da Testnet
app.MapGet("/api/testnet/audits", async (IFeatureStore store, int limit = 100) =>
{
    var logs = await store.GetTestnetAuditLogsAsync(limit);
    return Results.Ok(logs);
})
.WithName("GetTestnetAudits");

app.Run();
