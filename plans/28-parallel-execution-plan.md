# 28 — Parallel Execution Plan

Data-base: **2026-05-27 UTC-03**

## Objetivo
Organizar a execução paralela das trilhas de consolidação do projeto até transformar o protótipo atual em MVP robusto validável, evitando colisões de commits e garantindo a ordem lógica das dependências.

## Visão Consolidada do Estado Atual
O projeto passou pelas fases M0 a M8 com entregas marcadas como concluídas, mas muitas delas operam como protótipos funcionais, esqueletos em dry-run ou baseadas em heurísticas, sem validação com dados reais. A Fase M9 visa expor essa realidade e elevar o nível de maturidade de simulações para um modo validado e real.

| Fase | Status Real (M9) |
|---|---|
| M0 Foundation | Completed |
| M1 Market Data + Feature Store | Functional Prototype |
| M2 Backtesting + Strategy Lab | Functional Prototype |
| M3 Paper Trading + Risk | Functional Prototype |
| M4 Binance Spot Testnet | Skeleton/Dry-run |
| M5 Dashboard + Observability | Functional Prototype |
| M6 Intelligence Layer | Heuristic Prototype |
| M7 Adaptive Strategy Orchestration | Heuristic Prototype |
| M8 Hardening | Initial Hardening |
| M9 Validation & Reality Check | In Progress |

## Matriz de Paralelização

| Trilha / Branch | Responsável Lógico | Arquivos Prováveis | Dependências |
|---|---|---|---|
| **A** - `m9/validation-reality-check` | Dev sênior / Auditor Técnico | `plans/*.md` | Nenhuma |
| **C** - `feature/persistence-featurestore-hardening` | Dev sênior .NET / PostgreSQL | `src/Infrastructure/Data/*`, `tests/IntegrationTests/*` | Nenhuma |
| **H** - `feature/rag-context-optimizer` | Eng. Prompt / RAG | `tools/rag/*`, `.github/copilot-instructions.md`, `.vscode/mcp.example.json` | Nenhuma |
| **D** - `feature/backtesting-reports-walkforward` | Quant dev / Eng. .NET | `src/Core/Backtesting/*`, `src/Infrastructure/Data/*` | Parcialmente de **C** |
| **E** - `feature/paper-trading-realistic-engine` | Dev execução / simulação | `src/Core/Execution/*`, `src/Core/Risk/*` | Parcialmente de **C** |
| **B** - `feature/binance-testnet-real` | Dev .NET / Integração Binance | `src/Infrastructure/Exchange/*`, `tests/*` | Nenhuma (Merge após C/E) |
| **G** - `feature/dashboard-observability-real-state`| Dev Frontend / Backend | `dashboard/src/*`, `src/Api/*` | APIs estabilizadas |
| **F** - `feature/adaptive-real-metrics` | Quant / Arquiteto Orquestração | `src/Core/Adaptive/*`, `src/Infrastructure/Data/*` | **C**, **D**, **E** |
| **I** - `ci/release-readiness-hardening` | DevOps / Qualidade | `.github/workflows/*`, `plans/*` | Fechamento após demais |
| **J** - `integration/final-mvp-consolidation` | Release Engineer | Todos | **A** a **I** |

## Ordem de Execução e Merge Recomendada

**Rodada 1 (Fundação e Realidade):**
*   Trilha A (M9)
*   Trilha C (Persistência)
*   Trilha H (RAG)

**Rodada 2 (Execução e Integração):**
*   Trilha B (Testnet)
*   Trilha D (Backtesting)
*   Trilha E (Paper Trading)
*   Trilha G (Dashboard)

**Rodada 3 (Inteligência e Fechamento):**
*   Trilha F (Orquestração Adaptativa)
*   Trilha I (Hardening Final)

**Rodada 4 (Integração):**
*   Trilha J (Consolidação)

Ordem de Merge: `A → C → D → E → F → B → G → H → I`

## Riscos
1. **Colisão de Banco de Dados**: A Trilha C altera a persistência e schemas, afetando as Trilhas D, E e F. É crucial que a DDL e os repositórios base fiquem prontos logo.
2. **APIs Quebradas no Frontend**: O Dashboard (G) depende das respostas do Backend que serão alteradas por Backtesting e Orquestração (D e F).
3. **Falso Positivo de Testnet**: A Trilha B precisa garantir que credenciais nunca escapem em logs e que o dry-run funcione por padrão se as credenciais faltarem.
