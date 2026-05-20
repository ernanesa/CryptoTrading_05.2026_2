# 03 — Architecture

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

## Arquitetura alvo

```text
src/
  Domain/
  Application/
  Contracts/
  Infrastructure/
  MarketData/
  Backtesting/
  Strategies/
  Risk/
  Execution/
  ML.Service/          opcional
  Api/
  Worker/
dashboard/
  web/
plans/
```

## Planos lógicos

### Control Plane

- AdaptiveStrategyOrchestrator;
- StrategyScoringService;
- AssetRankingService;
- MarketRegimeService;
- PortfolioAllocator;
- ParameterOptimizer;
- RiskPolicyManager;
- ExperimentManager.

### Execution Plane

- MarketDataIngestion;
- FeatureStore;
- StrategyRunner;
- RiskEngine;
- PaperTradeExecutor;
- TestnetExecutor;
- PositionManager;
- ExecutionOptimizer.

### Learning Plane

- StrategyPerformanceTracker;
- TradeAttributionService;
- WalkForwardEvaluator;
- BanditAllocator;
- ModelRegistry;
- StrategyHealthMonitor.

### Observability Plane

- DecisionAudit;
- logs;
- metrics;
- traces;
- dashboard.

## Regras de dependência

```text
Domain não depende de infraestrutura.
Application depende de Domain e Contracts.
Infrastructure implementa portas da Application.
Worker/API compõem serviços.
Dashboard consome API/SignalR.
```

## Pipeline de decisão

```text
MarketData
  ↓
FeatureStore
  ↓
Strategies geram sinais
  ↓
AdaptiveOrchestrator pontua e prioriza
  ↓
IntelligenceLayer adiciona contexto
  ↓
RiskEngine decide limites/bloqueios
  ↓
Executor executa no modo permitido
  ↓
DecisionAudit + PerformanceTracker
```
