# 06 — Stage 02: Backtesting + Strategy Lab

## Objetivo

Criar motor de backtest e laboratório de estratégias.

## Entrega de valor

Comparar estratégias em dados históricos com taxas, slippage, risco e métricas.

## Estratégias iniciais

- EMA Trend Following;
- ATR Breakout;
- Bollinger Mean Reversion;
- RSI Mean Reversion;
- Grid controlado.

## Componentes

- BacktestEngine;
- StrategyRegistry;
- StrategyRunner;
- FeeModel;
- SlippageModel;
- PositionManager;
- PerformanceAnalyzer;
- BacktestReport.

## Métricas

- retorno líquido;
- max drawdown;
- profit factor;
- expectancy;
- win rate;
- average win/loss;
- slippage;
- fee impact;
- performance por regime.

## Critérios de aceite

- [ ] pelo menos 3 estratégias rodam;
- [ ] relatório gerado;
- [ ] resultado reproduzível;
- [ ] taxas e slippage considerados;
- [ ] performance por estratégia registrada.
