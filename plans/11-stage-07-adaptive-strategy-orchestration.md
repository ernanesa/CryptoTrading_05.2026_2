# 11 — Stage 07: Adaptive Strategy Orchestration

## Objetivo

Criar o cérebro adaptativo do robô: escolher dinamicamente estratégias, ativos, timeframes, pesos, tamanho de posição e política de saída conforme mercado, performance, risco e custo de execução.

## Princípio

```text
Não existe a melhor estratégia universal.
Existe a melhor estratégia para aquele ativo, timeframe, regime, liquidez, custo e risco naquele momento.
```

## Componentes

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
- [x] MarketHealthScore.

## Score de ativo

```text
AssetScore =
  LiquidityScore * 0.20 +
  SpreadScore * 0.15 +
  VolatilityScore * 0.15 +
  TrendScore * 0.15 +
  MomentumScore * 0.15 +
  StrategyFitScore * 0.10 +
  CorrelationPenalty * 0.05 +
  RiskPenalty * 0.05
```

## Score de estratégia

```text
StrategyScore =
  RegimeFitScore * 0.25 +
  RecentExpectancyScore * 0.20 +
  ProfitFactorScore * 0.15 +
  DrawdownPenalty * 0.15 +
  ExecutionCostScore * 0.10 +
  SignalQualityScore * 0.10 +
  StabilityScore * 0.05
```

## Anti-troca frenética

Só trocar estratégia se:

- nova estratégia tiver score mínimo superior;
- vantagem persistir por N ciclos;
- cooldown mínimo tiver passado;
- DataQualityGate estiver OK;
- RiskEngine não estiver restritivo.

## Experimentos obrigatórios

- estratégia fixa vs adaptativa;
- com/sem AssetRanking;
- com/sem MarketRegime;
- position sizing fixo vs dinâmico;
- stop fixo vs dinâmico;
- com/sem ExecutionCostModel;
- com/sem StrategyHealthMonitor.

## Critérios de aceite

- [x] escolhe estratégias diferentes em regimes diferentes;
- [x] registra score e explicação;
- [x] usa histerese/cooldown;
- [x] pausa estratégia ruim;
- [x] compara fixo vs adaptativo;
- [x] dashboard mostra regime, score, estratégia ativa e motivo.
