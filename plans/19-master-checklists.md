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
| M6 Intelligence Layer | Completed |
| M7 Adaptive Strategy Orchestration | Completed |
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
- [x] FeatureExtractor;
- [x] AnomalyDetectionService inicial;
- [x] RegimeDetectionService inicial;
- [x] VolatilityForecastService;
- [x] MetaLabelingService;
- [x] SentimentRiskService;
- [x] EventRiskClassifier;
- [x] ModelRegistry;
- [x] RagContextProvider;
- [x] ExplanationService.

### Registro da atividade M6 inicial

RAG consultado: sim
Consulta: M6 IntelligenceSnapshot RegimeDetectionService AnomalyDetectionService criterios aceite
Contexto encontrado: componentes da Stage 06 e criterios de aceite do IntelligenceSnapshot.
Impacto: implementacao inicial mantida como score heuristico versionado, sem bypass do RiskEngine.
Data: 2026-05-20

### Registro da atividade M6 FeatureExtractor e Volatilidade

RAG consultado: sim
Consulta: M6 FeatureExtractor VolatilityForecastService IntelligenceSnapshot FeatureStore criterios aceite
Contexto encontrado: componentes pendentes da Stage 06 e criterio de features versionadas da Stage 01.
Impacto: adicionados vetor de features versionado e forecast heuristico de volatilidade ao snapshot, mantendo score como contexto auxiliar.
Data: 2026-05-20

### Registro da atividade M6 fechamento

RAG consultado: sim
Consulta: M6 MetaLabelingService SentimentRiskService EventRiskClassifier ModelRegistry RagContextProvider ExplanationService finalizar Intelligence Layer
Contexto encontrado: componentes finais da Stage 06, objetivo de inteligencia auxiliar e regra de sentimento como filtro/contexto.
Impacto: M6 concluida com todos os componentes planejados no snapshot, mantendo execucao condicionada ao RiskEngine.
Data: 2026-05-20

## M7 — Adaptive Orchestration checklist

- [x] StrategyRegistry;
- [x] MarketRegimeService;
- [x] AssetRankingService;
- [x] StrategyPerformanceTracker;
- [x] StrategyScoringService;
- [x] AdaptiveStrategyOrchestrator;
- [x] AdaptivePortfolioAllocator;
- [x] DynamicPositionSizingService;
- [x] DynamicExitEngine;
- [x] ExecutionCostModel;
- [x] StrategyHealthMonitor;
- [x] TradeAttributionService;
- [x] WalkForwardEvaluator;
- [x] MultiArmedBanditAllocator;
- [x] DataQualityGate;
- [x] MarketHealthScore;
- [x] dashboard de score/regime/estratégia;
- [x] backtest fixo vs adaptativo.

### Registro da atividade M7 fechamento

RAG consultado: sim
Consulta: M7 AdaptiveStrategyOrchestrator AssetRanking StrategyScoring hysteresis cooldown RiskEngine DataQualityGate fixed vs adaptive dashboard
Contexto encontrado: componentes da Stage 07, checklist M7 e Control Plane da arquitetura.
Impacto: M7 concluida com Control Plane adaptativo heuristico, histerese/cooldown, gates de risco/qualidade, comparativo fixo vs adaptativo e dashboard.
Data: 2026-05-20
