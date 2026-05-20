# 07 — Stage 03: Paper Trading + Risk

## Objetivo

Simular execução em tempo quase real com carteira virtual, posições, PnL, risco e auditoria.

## Entrega de valor

Validar decisões em fluxo contínuo antes da Testnet.

## Componentes

- VirtualWallet;
- PaperTradeExecutor;
- RiskEngine;
- Sentinel;
- PositionManager;
- TradeLedger;
- DecisionAudit;
- StrategyPerformanceTracker inicial.

## Regras de risco mínimas

- max drawdown;
- perda máxima diária simulada;
- exposição por ativo;
- exposição total;
- máximo de ordens abertas;
- spread máximo;
- liquidez mínima;
- cooldown após sequência de perdas;
- circuit breaker;
- halted mode.

## Critérios de aceite

- [x] sinais passam pelo RiskEngine;
- [x] ordens simuladas registradas;
- [x] PnL simulado calculado;
- [x] rejeições explicadas;
- [x] halted mode bloqueia novas ações;
- [x] dashboard/logs mostram status.
