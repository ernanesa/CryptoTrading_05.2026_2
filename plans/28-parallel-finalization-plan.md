# 28 — Parallel Finalization Plan

Data-base: **2026-05-27 UTC-03**

## Objetivo
Consolidar a execução paralela das trilhas de finalização do CryptoTrading MVP, atualizando o status de maturidade das trilhas de base e definindo as próximas rodadas de engenharia de execução.

## Visão Consolidada do Estado Atual

| Fase | Status Real (M9) | Maturidade Validada |
|---|---|---|
| M0 Foundation | Completed | 100% |
| M1 Market Data + Feature Store | Functional Prototype | 80% |
| M2 Backtesting + Strategy Lab | Functional Prototype | 75% |
| M3 Paper Trading + Risk | Functional Prototype | 70% |
| M4 Binance Spot Testnet | Functional Prototype | 90% (executor strict RiskDecision gated; REST bridge pending) |
| M5 Dashboard + Observability | Functional Prototype | 85% |
| M6 Intelligence Layer | Heuristic Prototype | 60% |
| M7 Adaptive Strategy Orchestration | Heuristic Prototype | 60% |
| M8 Hardening | Completed | 100% (SecretRedacted Logs; mandatory/opt-in gates separated) |
| M9 Validation & Reality Check | Completed | 100% |

## Matriz de Paralelização & Status de Trilhas

| Trilha / Branch | Descrição | Status Atual |
|---|---|---|
| **Task A** - `m9/update-reality-check-v2` | Atualizacao de maturidade de M0-M9 e Master Checklists com realidade de codigo | **Completed** |
| **Task B** - `feature/testnet-risk-decision-gate` | Binance Testnet com strict validation e barreira de `RiskDecision` no executor | **Completed** |
| **Task C** - `feature/runtime-mode-api` | Criacao do `RuntimeMode` global, `RuntimeStatusService` e endpoint REST | **Completed** |
| **Task D** - `feature/rag-context-pack-v2` | Upgrades de barreira de contexto e prompts enriquecidos no RagTool | **Completed** |
| **Task E** | Paper Trading State Machine e acompanhamento de PnL não realizado | *Pendente* |
| **Task F** | Métricas avançadas de Backtesting (Sortino, Calmar, Consecutive Losses) | *Pendente* |
| **Task G** | Persistência de orquestração adaptativa e `trade_attributions` no banco | *Pendente* |
| **Task H** | Ponte REST/orquestrador para submeter Testnet com `RiskDecision` aprovado | *Pendente* |
| **Task I** | Dashboard RuntimeMode Badge usando `/api/runtime/status` e componentização React | *Pendente* |
| **Task J** | Relatório final de Readiness de Produção (`plans/30-release-readiness-report.md`) | **Completed** |
| **Task K** | Fusão geral de tracks e homologação de build final | *Pendente* |

## Notas de realidade pos-paralelizacao

- O gate estrito de `RiskDecision` foi implementado no executor Testnet e validado por testes unitarios.
- A rota `/api/testnet/order` ainda nao recebe `RiskDecision`; no estado atual, ela rejeita por seguranca em vez de executar uma ordem sem autorizacao de risco.
- O endpoint `/api/runtime/status` existe e alimenta o hardening report, mas o dashboard ainda precisa consumi-lo como fonte canonica do modo operacional.
- O RagTool possui `context-pack` e `optimize-input`; o uso continua obrigatorio antes de tarefas que dependam de contexto tecnico acumulado.

## Próxima Rodada: Backtesting, Paper Trading e Ponte Testnet (Tasks E, F e H)
O foco principal após a fundação do runtime mode e segurança da testnet passa a ser o refinamento analítico das engines de simulação e a conexao segura entre risco e submissao Testnet.
1. Desenvolver a State Machine para transições seguras de ordens no Paper Trading.
2. Calcular PnL não realizado dinâmico com reconciliação de book.
3. Computar exposição média e métricas avançadas quantitativas em backtests.
4. Propagar `RiskDecision` aprovado para a rota Testnet sem criar bypass do `RiskEngine`.
