# 27 — Stage 09: Validation & Reality Check

Data-base: **2026-05-24 UTC-03 / America/Maceio**

## Objetivo
Corrigir o descompasso entre os checklists marcados como concluídos (Fases M0 a M8) e a maturidade real do projeto. Estabelecemos a Fase M9 para auditar e rastrear o que é código de produção real versus o que é um protótipo, mock ou heurística simulada.

## Relatório de Maturidade

Classificação das Fases (M0 a M8):
*   **Completed**: Totalmente entregue.
*   **Functional Prototype**: Funciona ponta a ponta, mas carece de robustez (ex: falta paginação, retry avançado, migrações de BD).
*   **Skeleton/Dry-run**: Código existe, compila, mas não executa integrações reais (ex: mocks de API).
*   **Heuristic Prototype**: Toma decisões baseadas em regras hardcoded ou heurísticas fixas em vez de dados históricos reais e Machine Learning.
*   **Initial Hardening**: Possui CI, build e testes básicos, mas faltam gates pesados (E2E, Testcontainers opt-in).

### M0: Foundation
*   **Status real**: Completed
*   **Percentual estimado**: 100%
*   **Evidências**: Estrutura da solução limpa (.NET 10, C# 14), injeção de dependência implementada, Serilog configurado, Native AOT opt-in estruturado.
*   **Riscos/Desvios**: Nenhum.
*   **Próximos gates**: Manter compatibilidade com upgrades de pacotes.

### M1: Market Data & Feature Store
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 80%
*   **Evidências**: O `Worker` efetivamente baixa candles reais da Binance, calcula 10+ features e os insere no PostgreSQL via Dapper.
*   **Riscos/Desvios**: O `FeatureStore` usa um DDL de `CREATE TABLE IF NOT EXISTS` codificado no C#. Falta um mecanismo real de migração (DbUp/FluentMigrator). As inserções não usam `COPY` para eficiência extrema.
*   **Próximos gates**: Implementar NpgsqlDataSource, migrações versionadas e PostgreSQL COPY.

### M2: Backtesting + Strategy Lab
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 75%
*   **Evidências**: A `BacktestEngine` suporta taxa de corretagem e slippage. Os endpoints executam simulações rápidas.
*   **Riscos/Desvios**: Faltam métricas essenciais como Sharpe e Sortino. O sistema não persiste `backtest_runs` e `backtest_trades` em banco, dificultando comparações no futuro.
*   **Próximos gates**: Persistir reports de testes, adicionar métricas avançadas e walk-forward.

### M3: Paper Trading + Risk
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 70%
*   **Evidências**: O `RiskEngine` bloqueia ordens e gera `DecisionAudit`. Existe uma `paper_wallet` e tabela `paper_trades`.
*   **Riscos/Desvios**: Não existe um State Machine rígido para evolução das ordens. Falta o reconciliation loop contínuo e acompanhamento de PnL não-realizado.
*   **Próximos gates**: Implementar ordens parciais, State Machine rigoroso e controle de margem/exposure.

### M4: Binance Spot Testnet
*   **Status real**: Skeleton/Dry-run
*   **Percentual estimado**: 25%
*   **Evidências**: `BinanceTestnetExecutor` processa regras e mascaramento de logs.
*   **Riscos/Desvios**: O caminho real (`if (IsEnabled)`) contém apenas comentários. Não há conexão HTTP real via `Binance.Net` implementada no momento.
*   **Próximos gates**: Implementar chamada HTTP real para Testnet protegida por chaves e dry-run em fallback.

### M5: Dashboard + Observability
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 85%
*   **Evidências**: O Vite-React dashboard compila (`npm run build`) e se conecta via SignalR, plotando gráficos com TradingView Lightweight Charts.
*   **Riscos/Desvios**: Falta separação clara visual no painel entre "Simulação" e "Modo Real".
*   **Próximos gates**: Separar claramente visões Offline, Simulation, Paper e Testnet; expor tracing OpenTelemetry no React.

### M6: Intelligence Layer
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 60%
*   **Evidências**: `IntelligenceSnapshotService` agrega dados e detecta volatilidade/anomalias.
*   **Riscos/Desvios**: Tudo é baseado em heurísticas matemáticas hardcoded. Falta a infraestrutura para ML.NET e o `ModelRegistry` para controle de versões dos scores.
*   **Próximos gates**: Integrar métricas avançadas de heurística validadas, pavimentar o caminho para inferência via ONNX/ML.NET futuro.

### M7: Adaptive Strategy Orchestration
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 60%
*   **Evidências**: O Control Plane decide dinamicamente a alocação e realiza trocas baseadas em escores heurísticos.
*   **Riscos/Desvios**: O Multi-Armed Bandit não usa os dados reais persistidos da Fase M2/M3 para o aprendizado e exploração. O cooldown e histerese operam em memória.
*   **Próximos gates**: Usar persistência de métricas, cooldown no DB, e alimentar o Multi-Armed Bandit com histórico verdadeiro.

### M8: Hardening
*   **Status real**: Initial Hardening
*   **Percentual estimado**: 80%
*   **Evidências**: `dotnet test` passa, benchmarks locais estão criados, secrets são mascarados (SecretRedactor), workflow do GitHub Actions existe.
*   **Riscos/Desvios**: Integração real usando Testcontainers para banco de dados ainda é incipiente. Faltam CI checks pesados opt-in rodando via Dependabot/Renovate.
*   **Próximos gates**: Relatório de readiness, secret scanning, checklists consolidados finais e gates manuais bem documentados.

## Próximos Passos (Backlog de Execução)
Para tornar o projeto **Production-Ready** em termos técnicos (sem ainda engajar capital real), seguiremos a trilha:
1.  Implementar integração **real** na Binance Spot Testnet (`4.1`).
2.  Refatorar `FeatureStore` com migrações maduras e inserção em lote (`4.2`).
3.  Tornar o backtesting persistente com novas métricas (`4.3`).
4.  Implementar State Machine e PnL não realizado no Paper Trading (`4.4`).
5.  Fazer a orquestração adaptativa usar os dados históricos via Multi-Armed Bandit (`4.5`).
6.  Adequar o Dashboard para refletir claramente o modo de operação e métricas expandidas (`4.7`).
