# 29 — Finalization Backlog

Data-base: **2026-05-27 UTC-03**

## Objetivo
Listar o backlog priorizado de pendências reveladas pela Fase M9 (Validation & Reality Check) para transformar os protótipos funcionais e heurísticas em código de produção validado.

## Backlog Priorizado

### Prioridade Alta (Fundação de Dados e Risco)
- **C.1** Introduzir NpgsqlDataSource e separar DDL/migrations do código.
- **C.2** Implementar inserção em lote otimizada (COPY) para `candles` e `features`.
- **E.1** Criar State Machine rigorosa de ordens (New, Open, PartiallyFilled, Filled, Rejected, Cancelled).
- **E.2** Implementar simulação de preenchimento parcial e reconciliation loop para Paper Trading.

### Prioridade Média (Validação de Mercado e Backtest)
- **B.1** Implementar `PlaceOrderAsync` HTTP real na Binance Spot Testnet protegida por flag de configuração explícita.
- **D.1** Persistir `backtest_runs` e `backtest_trades` no banco de dados.
- **D.2** Implementar validação walk-forward e gerar relatórios avançados em JSON/Markdown.
- **F.1** Persistir métricas de estratégia por ativo/timeframe.
- **F.2** Alimentar Multi-Armed Bandit com histórico real em vez de scores hipotéticos em memória.

### Prioridade Baixa/Fechamento (UI e Qualidade)
- **G.1** Separar modo Offline, Simulation, Paper e Testnet claramente no Dashboard.
- **H.1** Isolar e refinar o RAG local para ser usado como reflexo natural e contexto para as demais tarefas.
- **I.1** Estabelecer o gate opt-in pesado definitivo de E2E e Integração com DB e secret scanning.
