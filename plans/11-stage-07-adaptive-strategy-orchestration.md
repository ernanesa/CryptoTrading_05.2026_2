# 11 — Stage 07: Adaptive Strategy Orchestration

## Objetivo

Criar o cérebro adaptativo do robô: escolher dinamicamente estratégias, ativos, timeframes, pesos, tamanho de posição e política de saída conforme mercado, performance, risco e custo de execução.

## Princípio

```text
Não existe a melhor estratégia universal.
Existe a melhor estratégia para aquele ativo, timeframe, regime, liquidez, custo e risco naquele momento.
```

## Componentes

- StrategyRegistry;
- MarketRegimeService;
- AssetRankingService;
- StrategyPerformanceTracker;
- StrategyScoringService;
- AdaptiveStrategyOrchestrator;
- AdaptivePortfolioAllocator;
- DynamicPositionSizingService;
- DynamicExitEngine;
- ExecutionCostModel;
- StrategyHealthMonitor;
- TradeAttributionService;
- WalkForwardEvaluator;
- MultiArmedBanditAllocator;
- DataQualityGate;
- MarketHealthScore.

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

- [ ] escolhe estratégias diferentes em regimes diferentes;
- [ ] registra score e explicação;
- [ ] usa histerese/cooldown;
- [ ] pausa estratégia ruim;
- [ ] compara fixo vs adaptativo;
- [ ] dashboard mostra regime, score, estratégia ativa e motivo.
