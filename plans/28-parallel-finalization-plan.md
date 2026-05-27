# 28 — Parallel Finalization Plan

Data-base: **2026-05-27 UTC-03**

## Objetivo
Consolidar a execução paralela das trilhas de finalização do CryptoTrading MVP, atualizando o status de maturidade das trilhas de base e definindo as próximas rodadas de engenharia de execução.

## Visão Consolidada do Estado Atual

| Fase | Status Real (M9) | Maturidade Validada |
|---|---|---|
| M0 Foundation | Completed | 100% |
| M1 Market Data + Feature Store | Functional Prototype | 80% |
| M2 Backtesting + Strategy Lab | Functional Prototype | 80% (repository/schema exist; product-flow persistence evidence pending) |
| M3 Paper Trading + Risk | Functional Prototype | 78% (state/PnL foundation exists; Paper events pending) |
| M4 Binance Spot Testnet | Functional Prototype | 95% (executor strict RiskDecision gated; REST bridge done; real Testnet opt-in pending) |
| M5 Dashboard + Observability | Functional Prototype | 90% (Dashboard RuntimeMode badge using `/api/runtime/status` done) |
| M6 Intelligence Layer | Heuristic Prototype | 60% |
| M7 Adaptive Strategy Orchestration | Heuristic Prototype | 70% |
| M8 Hardening | Completed | 100% (SecretRedacted Logs; mandatory/opt-in gates separated) |
| M9 Validation & Reality Check | Completed | 100% |

## Matriz de Paralelização & Status de Trilhas

| Trilha / Branch | Descrição | Status Atual |
|---|---|---|
| **Task A** - `m9/update-reality-check-v2` | Atualizacao de maturidade de M0-M9 e Master Checklists com realidade de codigo | **Completed** |
| **Task B** - `feature/testnet-risk-decision-gate` | Binance Testnet com strict validation e barreira de `RiskDecision` no executor | **Completed** |
| **Task C** - `feature/runtime-mode-api` | Criacao do `RuntimeMode` global, `RuntimeStatusService` e endpoint REST | **Completed** |
| **Task D** - `feature/rag-context-pack-v2` | Upgrades de barreira de contexto e prompts enriquecidos no RagTool | **Completed** |
| **Task E** | Eventos auditaveis de Paper Trading sobre a base de State Machine/PnL ja criada | *Pendente* |
| **Task F** | Persistência de backtesting homologada no fluxo produto e comparacao historica | *Pendente* |
| **Task G** | Agregador adaptativo persistido para eventos, metricas e atribuições | *Pendente* |
| **Task H** | Ponte REST/orquestrador para submeter Testnet com `RiskDecision` aprovado | **Completed** |
| **Task I** | Dashboard RuntimeMode Badge usando `/api/runtime/status` e componentização React | **Completed** |
| **Task J** | Relatório final de Readiness de Produção (`plans/30-release-readiness-report.md`) | *Pendente de evidencias finais* |
| **Task K** | Fusão geral de tracks e homologação de build final | *Pendente* |
| **Task L** | Execucao registrada dos opt-ins reais: Testnet real, Playwright, Testcontainers, FeatureStore benchmark e Native AOT quando aplicavel | *Pendente* |

## Notas de realidade pos-paralelizacao

- O gate estrito de `RiskDecision` foi implementado no executor Testnet e validado por testes unitarios.
- A rota `/api/testnet/order` recebe `RiskDecision`, pre-valida com `TestnetOrderSubmissionGuard`, grava `DecisionAudit` e rejeita antes do executor quando a decisao esta ausente, expirada, reprovada ou incompativel.
- O endpoint `/api/runtime/status` existe, alimenta o hardening report e e consumido pelo dashboard como fonte canonica do modo operacional.
- O RagTool possui `context-pack` e `optimize-input`; o uso continua obrigatorio antes de tarefas que dependam de contexto tecnico acumulado.
- Paper Trading, Adaptive Orchestration e Backtesting possuem fundacoes implementadas, mas ainda faltam eventos de Paper, agregador adaptativo, persistencia de backtest homologada no fluxo produto, relatorio final com evidencias e opt-ins reais executados.

## Próxima Rodada: Paper Events, Backtest Persistence, Adaptive Aggregator e Evidencias Reais
O foco principal apos a fundacao do runtime mode e seguranca da Testnet passa a ser a homologacao operacional dos fluxos que ainda podem parecer completos apenas por terem base de codigo.
1. Implementar eventos auditaveis para o ciclo de vida do Paper Trading.
2. Homologar persistencia de backtests nos endpoints/relatorios e leitura comparativa historica.
3. Implementar agregador adaptativo que consuma eventos, metricas e atribuições persistidas.
4. Finalizar o readiness report com evidencias dos opt-ins reais executados.
