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

- [ ] sinais passam pelo RiskEngine;
- [ ] ordens simuladas registradas;
- [ ] PnL simulado calculado;
- [ ] rejeições explicadas;
- [ ] halted mode bloqueia novas ações;
- [ ] dashboard/logs mostram status.
