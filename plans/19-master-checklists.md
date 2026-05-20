# 19 — Master Checklists

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

## Status macro

| Fase | Status |
|---|---|
| M0 Foundation | Completed |
| M1 Market Data + Feature Store | Completed |
| M2 Backtesting + Strategy Lab | Completed |
| M3 Paper Trading + Risk | Completed |
| M4 Binance Spot Testnet | Completed |
| M5 Dashboard + Observability | Completed |
| M6 Intelligence Layer | In progress |
| M7 Adaptive Strategy Orchestration | Not started |
| M8 Hardening | Not started |

## Checklist antes de qualquer atividade

- [x] data atual verificada;
- [x] plano consultado;
- [x] RAG consultado quando necessário;
- [ ] documentação oficial consultada quando aplicável;
- [x] entrega de valor definida;
- [x] critérios de aceite definidos;
- [x] riscos listados;
- [x] testes definidos.

## M6 — Intelligence Layer checklist

- [x] IntelligenceSnapshot versionado;
- [x] modelo/score tem versão;
- [x] fonte do score registrada;
- [x] insights aparecem no dashboard;
- [x] nenhum modelo bypassa RiskEngine;
- [ ] FeatureExtractor;
- [x] AnomalyDetectionService inicial;
- [x] RegimeDetectionService inicial;
- [ ] VolatilityForecastService;
- [ ] MetaLabelingService;
- [ ] SentimentRiskService;
- [ ] EventRiskClassifier;
- [ ] ModelRegistry;
- [ ] RagContextProvider;
- [ ] ExplanationService.

### Registro da atividade M6 inicial

RAG consultado: sim
Consulta: M6 IntelligenceSnapshot RegimeDetectionService AnomalyDetectionService criterios aceite
Contexto encontrado: componentes da Stage 06 e criterios de aceite do IntelligenceSnapshot.
Impacto: implementacao inicial mantida como score heuristico versionado, sem bypass do RiskEngine.
Data: 2026-05-20

## M7 — Adaptive Orchestration checklist

- [ ] StrategyRegistry;
- [ ] MarketRegimeService;
- [ ] AssetRankingService;
- [ ] StrategyPerformanceTracker;
- [ ] StrategyScoringService;
- [ ] AdaptiveStrategyOrchestrator;
- [ ] AdaptivePortfolioAllocator;
- [ ] DynamicPositionSizingService;
- [ ] DynamicExitEngine;
- [ ] ExecutionCostModel;
- [ ] StrategyHealthMonitor;
- [ ] TradeAttributionService;
- [ ] WalkForwardEvaluator;
- [ ] MultiArmedBanditAllocator;
- [ ] DataQualityGate;
- [ ] MarketHealthScore;
- [ ] dashboard de score/regime/estratégia;
- [ ] backtest fixo vs adaptativo.
