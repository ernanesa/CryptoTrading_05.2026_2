# 09 — Stage 05: Dashboard + Observability

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## Objetivo

Criar painel e observabilidade para acompanhar estado, métricas, decisões, risco e estratégias.

## Stack

- ASP.NET Core API/BFF;
- SignalR;
- React + TypeScript + Vite;
- TanStack Query;
- TradingView Lightweight Charts;
- OpenTelemetry;
- Serilog;
- Prometheus;
- Grafana.

## Telas

- Overview;
- Market Data;
- Feature Store;
- Backtests;
- Paper Trading;
- Risk;
- Testnet;
- Strategies;
- Adaptive Orchestrator;
- ML/Intelligence;
- Logs;
- Settings.

## Métricas

- uptime;
- candles received;
- db write latency;
- signals generated;
- risk rejections;
- paper PnL;
- testnet requests;
- strategy score;
- asset score;
- regime;
- execution cost;
- drawdown.

## Critérios de aceite

- [x] dashboard mostra status;
- [x] métricas em tempo real;
- [x] logs estruturados;
- [x] traces com correlation id;
- [x] health checks;
- [x] tela de estratégia/regime/score.
