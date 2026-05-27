using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Infrastructure.Persistence;
using CryptoTrading.Application.Services;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Api.Hubs;
using CryptoTrading.Api.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=cryptotrading;Username=postgres;Password=postgres";
builder.Services.AddSingleton(Npgsql.NpgsqlDataSource.Create(connStr));
builder.Services.AddSingleton<IFeatureStore, FeatureStore>();
builder.Services.AddSingleton<IBacktestRepository, BacktestRepository>();
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
builder.Services.AddSingleton<AdaptiveMetricsAggregator>();
builder.Services.AddSingleton<AdaptiveFeedbackStateProjector>();
builder.Services.AddSingleton<AdaptiveStrategyOrchestrator>();
builder.Services.AddSingleton<DatasetBuilderService>();
builder.Services.AddSingleton<ModelDriftMonitor>();
builder.Services.AddSingleton<SecretRedactor>();
builder.Services.AddSingleton<ChaosScenarioRunner>();
builder.Services.AddSingleton<BenchmarkCatalog>();
builder.Services.AddSingleton<HardeningReportService>();
builder.Services.AddSingleton<RuntimeStatusService>();
builder.Services.AddTransient<PaperTradeExecutor>();
builder.Services.AddSingleton<ExchangeRuleValidator>();
builder.Services.AddTransient<BinanceTestnetExecutor>();
builder.Services.AddTransient<OrderStatusSynchronizer>();

builder.Services.AddSignalR();
builder.Services.AddHostedService<MetricsBroadcaster>();
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CryptoTrading.Api")).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddPrometheusExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.MapPrometheusScrapingEndpoint();

// Mapeamento do Hub de métricas do SignalR
app.MapHub<MetricsHub>("/hubs/metrics");

// Endpoints REST de Observabilidade e Saúde
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/runtime/status", (RuntimeStatusService statusService) => Results.Ok(statusService.GetStatus()))
    .WithName("GetRuntimeStatus");

app.MapGet("/api/metrics", (IMetricsService metrics) => Results.Ok(metrics.GetSnapshot()))
    .WithName("GetMetricsSnapshot");

app.MapGet("/api/hardening/report", (HardeningReportService hardening) => Results.Ok(hardening.Generate()))
    .WithName("GetHardeningReport");

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

app.MapGet("/api/adaptive/metrics/breakdown", async (
    string strategyName,
    string symbol,
    string interval,
    string regime,
    AdaptiveMetricsAggregator aggregator,
    int minimumSamples = 5) =>
{
    var breakdown = await aggregator.BuildBreakdownAsync(
        strategyName,
        symbol,
        interval,
        regime,
        new AdaptiveMetricsAggregationOptions { MinimumEvidenceSamples = minimumSamples });

    return Results.Ok(breakdown);
})
.WithName("GetAdaptiveMetricsBreakdown");

app.MapPost("/api/adaptive/metrics/refresh", async (
    string symbol,
    string interval,
    string regime,
    StrategyRegistry registry,
    AdaptiveMetricsAggregator aggregator,
    string? strategyName,
    int minimumSamples = 5) =>
{
    var strategyNames = string.IsNullOrWhiteSpace(strategyName)
        ? registry.GetAll().Select(s => s.Name).ToList()
        : new List<string> { strategyName };

    var breakdowns = await aggregator.AggregateAndPersistAsync(
        strategyNames,
        symbol,
        interval,
        regime,
        new AdaptiveMetricsAggregationOptions { MinimumEvidenceSamples = minimumSamples });

    return Results.Ok(breakdowns);
})
.WithName("RefreshAdaptiveMetrics");

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
    BacktestEngine engine,
    IBacktestRepository backtestRepo) =>
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
    var slipModel = new VolumeBasedSlippageModel(slippage ?? 0.0005m, 0.0001m);
    var feeModel = new MakerTakerFeeModel(fee ?? 0.001m, fee ?? 0.001m);

    var report = engine.Run(strategy, pointsList, capital, feeModel, slipModel);
    await backtestRepo.SaveReportAsync(report);
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
    BacktestEngine engine,
    IBacktestRepository backtestRepo) =>
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
    var slipModel = new VolumeBasedSlippageModel(slippage ?? 0.0005m, 0.0001m);
    var feeModel = new MakerTakerFeeModel(fee ?? 0.001m, fee ?? 0.001m);

    var comparativeReports = new List<object>();

    foreach (var strategy in registry.GetAll())
    {
        var report = engine.Run(strategy, pointsList, capital, feeModel, slipModel);
        await backtestRepo.SaveReportAsync(report);
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
            TotalPnLPercent = report.TotalPnLPercent,
            SharpeRatio = report.SharpeRatio,
            SortinoRatio = report.SortinoRatio,
            CalmarRatio = report.CalmarRatio,
            ExposureTimePercent = report.ExposureTimePercent,
            AvgHoldingTimeHours = report.AvgHoldingTimeHours,
            MaxConsecutiveLosses = report.MaxConsecutiveLosses,
            FeeImpactPercent = report.FeeImpactPercent,
            SlippageImpactPercent = report.SlippageImpactPercent,
            RegimeBreakdown = report.RegimeBreakdown
        });
    }

    return Results.Ok(comparativeReports);
})
.WithName("RunBacktestAllStrategies");

// 4. Executar backtest para múltiplos pares (Multi-pair)
app.MapGet("/api/backtest/run-multipair", async (
    string strategyName,
    string symbols, // comma separated
    string interval,
    DateTime startTime,
    DateTime endTime,
    decimal? initialCapital,
    decimal? slippage,
    decimal? fee,
    IFeatureStore store,
    StrategyRegistry registry,
    BacktestEngine engine,
    IBacktestRepository backtestRepo) =>
{
    var strategy = registry.Get(strategyName);
    if (strategy == null)
    {
        return Results.NotFound(new { Message = $"Estratégia '{strategyName}' não encontrada." });
    }

    var startUtc = startTime.ToUniversalTime();
    var endUtc = endTime.ToUniversalTime();

    var capital = initialCapital ?? 10000m;
    var slipModel = new VolumeBasedSlippageModel(slippage ?? 0.0005m, 0.0001m);
    var feeModel = new MakerTakerFeeModel(fee ?? 0.001m, fee ?? 0.001m);

    var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var reports = new List<object>();

    foreach (var sym in symbolList)
    {
        var dataPoints = await store.GetMarketDataPointsAsync(sym, interval, startUtc, endUtc);
        var pointsList = dataPoints.ToList();

        if (pointsList.Any())
        {
            var report = engine.Run(strategy, pointsList, capital, feeModel, slipModel);
            await backtestRepo.SaveReportAsync(report);
            reports.Add(report);
        }
    }

    return Results.Ok(reports);
})
.WithName("RunBacktestMultiPair");

// 4. Paper Trading — Estado da carteira virtual

// 5. Listar reports
app.MapGet("/api/backtest/reports", async (IBacktestRepository backtestRepo, int limit = 50) =>
{
    var reports = await backtestRepo.GetReportsAsync(limit);
    return Results.Ok(reports);
})
.WithName("GetBacktestReports");

app.MapGet("/api/backtest/reports/latest", async (IBacktestRepository backtestRepo, string strategy, string symbol) =>
{
    var report = await backtestRepo.GetLatestReportAsync(strategy, symbol);
    return report != null ? Results.Ok(report) : Results.NotFound();
})
.WithName("GetLatestBacktestReport");

app.MapGet("/api/backtest/reports/latest/export", async (
    IBacktestRepository backtestRepo,
    string strategy,
    string symbol,
    string format = "json") =>
{
    var report = await backtestRepo.GetLatestReportAsync(strategy, symbol);
    if (report == null)
    {
        return Results.NotFound();
    }

    return format.ToLowerInvariant() switch
    {
        "md" or "markdown" => Results.Text(ReportExporter.ToMarkdown(report), "text/markdown; charset=utf-8"),
        "json" => Results.Text(ReportExporter.ToJson(report), "application/json; charset=utf-8"),
        _ => Results.BadRequest(new { Message = "Formato inválido. Use 'json' ou 'markdown'." })
    };
})
.WithName("ExportLatestBacktestReport");

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

// 7.5. Paper Trading — Popular dados de teste com histórico realista para demonstrar no Dashboard
app.MapPost("/api/paper/seed", async (IFeatureStore store) =>
{
    // Limpa dados anteriores
    await store.ClearPaperTradingDataAsync();

    // 1. Salvar Balanço da Carteira USDT e BTC
    await store.SaveWalletBalanceAsync(new WalletBalance { Symbol = "USDT", Free = 9450.50m, Locked = 0.0m, UpdatedAt = DateTime.UtcNow });
    await store.SaveWalletBalanceAsync(new WalletBalance { Symbol = "BTC", Free = 0.0082m, Locked = 0.001m, UpdatedAt = DateTime.UtcNow });

    // 2. Inserir Trades de Teste Realistas
    var baseTime = DateTime.UtcNow.AddHours(-12);
    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "BTCUSDT", Type = "BUY", Price = 67200.0m, Quantity = 0.05m, Fee = 3.36m, PnL = 0.0m, ExecutedAt = baseTime });
    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "BTCUSDT", Type = "SELL", Price = 67550.0m, Quantity = 0.05m, Fee = 3.37m, PnL = 17.50m, ExecutedAt = baseTime.AddMinutes(45) });

    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "ETHUSDT", Type = "BUY", Price = 3450.0m, Quantity = 1.0m, Fee = 3.45m, PnL = 0.0m, ExecutedAt = baseTime.AddHours(2) });
    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "ETHUSDT", Type = "SELL", Price = 3420.0m, Quantity = 1.0m, Fee = 3.42m, PnL = -30.0m, ExecutedAt = baseTime.AddHours(2).AddMinutes(30) });

    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "BTCUSDT", Type = "BUY", Price = 66800.0m, Quantity = 0.05m, Fee = 3.34m, PnL = 0.0m, ExecutedAt = baseTime.AddHours(5) });
    await store.SavePaperTradeAsync(new PaperTrade { Symbol = "BTCUSDT", Type = "SELL", Price = 67250.0m, Quantity = 0.05m, Fee = 3.36m, PnL = 22.50m, ExecutedAt = baseTime.AddHours(5).AddMinutes(90) });

    // 3. Inserir Auditorias de Decisão (APPROVED / REJECTED)
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", SignalType = "BUY", Price = 67200.0m, Timestamp = baseTime, Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: todos os limites de Drawdown e Perda Diária estão normais." });
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", SignalType = "SELL", Price = 67550.0m, Timestamp = baseTime.AddMinutes(45), Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: sinal de saída de tendência acionado." });

    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "ETHUSDT", StrategyName = "RSI Mean Reversion", SignalType = "BUY", Price = 3450.0m, Timestamp = baseTime.AddHours(2), Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: RSI em nível extremo de sobrevenda (24.5)." });
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "ETHUSDT", StrategyName = "RSI Mean Reversion", SignalType = "SELL", Price = 3420.0m, Timestamp = baseTime.AddHours(2).AddMinutes(30), Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: stop-loss dinâmico acionado pelo motor de saída." });

    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "Bollinger Mean Reversion", SignalType = "BUY", Price = 67800.0m, Timestamp = baseTime.AddHours(3), Decision = "REJECTED", Reason = "Rejeitado pelo RiskEngine: volatilidade instantânea (ATR=150) excede o limite máximo permitido." });
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "ATR Breakout", SignalType = "BUY", Price = 67900.0m, Timestamp = baseTime.AddHours(4), Decision = "REJECTED", Reason = "Rejeitado pelo RiskEngine: spread do book de ofertas (0.15%) excede o spread máximo permitido para Spot." });

    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", SignalType = "BUY", Price = 66800.0m, Timestamp = baseTime.AddHours(5), Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: rompimento de média móvel com volume z-score favorável." });
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "EMA Trend Following", SignalType = "SELL", Price = 67250.0m, Timestamp = baseTime.AddHours(5).AddMinutes(90), Decision = "APPROVED", Reason = "Aprovado pelo RiskEngine: sinal de saída de tendência acionado." });

    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "ETHUSDT", StrategyName = "RSI Mean Reversion", SignalType = "BUY", Price = 3410.0m, Timestamp = baseTime.AddHours(6), Decision = "REJECTED", Reason = "Rejeitado pelo RiskEngine: score de anomalia de mercado (0.87) está acima do limite de tolerância." });
    await store.SaveDecisionAuditAsync(new DecisionAudit { Symbol = "BTCUSDT", StrategyName = "Bollinger Mean Reversion", SignalType = "BUY", Price = 66950.0m, Timestamp = baseTime.AddHours(7), Decision = "REJECTED", Reason = "Rejeitado pelo RiskEngine: limite diário máximo de 5 posições abertas já foi atingido para BTC." });

    return Results.Ok(new { Message = "Dados fictícios de simulação inseridos com sucesso para demonstração visual." });
})
.WithName("SeedPaperTrading");

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
app.MapPost("/api/testnet/order", async (TestnetOrderSubmission request, BinanceTestnetExecutor executor, IFeatureStore store) =>
{
    if (request.Order == null)
    {
        return Results.BadRequest(new { Message = "Payload invalido: informe order e riskDecision." });
    }

    var order = request.Order;
    order.ClientOrderId = $"CLIENT_{Guid.NewGuid().ToString().Substring(0, 10).ToUpper()}";
    order.CreatedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;

    var nowUtc = DateTime.UtcNow;
    var validation = TestnetOrderSubmissionGuard.Validate(order, request.RiskDecision, nowUtc);
    await store.SaveDecisionAuditAsync(TestnetOrderSubmissionGuard.CreateDecisionAudit(order, request.RiskDecision, validation, nowUtc));

    if (!validation.IsApproved)
    {
        await store.SaveTestnetAuditLogAsync(new TestnetAuditLog
        {
            Symbol = order.Symbol,
            Action = "TESTNET_REST_RISK_DECISION_REJECTED",
            Status = "FAILED",
            Details = validation.Reason
        });

        return Results.BadRequest(new { Message = "Ordem bloqueada antes da Testnet.", Reason = validation.Reason });
    }

    var result = await executor.ExecuteOrderAsync(order, request.RiskDecision);
    return result.Status == TestnetOrderStatus.Rejected.ToString()
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



// 14. Adaptive Orchestration API
app.MapPost("/api/orchestration/decide", async (AdaptiveOrchestrationRequest request, AdaptiveStrategyOrchestrator orchestrator, AdaptiveFeedbackStateProjector feedbackState, AdaptiveMetricsAggregator metricsAggregator, IFeatureStore store) =>
{
    var metricSymbol = request.Symbol.ToUpperInvariant();
    var metricInterval = string.IsNullOrWhiteSpace(request.Interval) ? "unknown" : request.Interval;
    var metricRegime = string.IsNullOrWhiteSpace(request.Intelligence.MarketRegime) ? "Unknown" : request.Intelligence.MarketRegime;

    await metricsAggregator.AggregateAndPersistAsync(
        request.StrategyNames,
        metricSymbol,
        metricInterval,
        metricRegime,
        new AdaptiveMetricsAggregationOptions());

    // Load real metrics
    foreach (var strategy in request.StrategyNames)
    {
        var metric = await store.GetStrategyPerformanceMetricAsync(strategy, metricSymbol, metricInterval, metricRegime);
        if (metric != null) request.RealMetrics[strategy] = metric;
    }

    // Load state
    var state = await store.GetStrategyStateAsync(request.CurrentStrategyName ?? "None", request.Symbol);
    if (state != null)
    {
        request.LastSwitchAt = state.CooldownUntil; // Note: simplified
        request.PersistentAdvantageCycles = state.AdvantageCycles;
    }

    var decision = orchestrator.Decide(request);

    // Persist adaptive feedback after every decision so hysteresis, pauses and scores survive restarts.
    await store.SaveStrategyStateAsync(feedbackState.Project(request, decision, state, DateTime.UtcNow));

    return Results.Ok(new
    {
        ActiveStrategy = decision.ActiveStrategyName,
        CandidateStrategy = decision.CandidateStrategyName,
        StrategyScores = decision.StrategyScores,
        Reasons = decision.Reasons,
        Decision = decision
    });
})
.WithName("AdaptiveOrchestrationDecide");
app.Run();

public record TestnetOrderSubmission(TestnetOrder Order, RiskDecision? RiskDecision);
