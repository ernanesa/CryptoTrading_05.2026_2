# 27 — Stage 09: Validation & Reality Check

Data-base: **2026-05-24 UTC-03 / America/Maceio**.
Revalidacao Task A: **2026-05-27 UTC-03 / America/Maceio**.

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
*   **Percentual estimado**: 80%
*   **Evidências**: A `BacktestEngine` suporta taxa de corretagem, slippage e métricas avançadas; existe base de persistência com `BacktestRepository` e tabelas `backtest_runs`/`backtest_trades`.
*   **Riscos/Desvios**: A persistência de backtests ainda precisa ser comprovada como fluxo produto completo nos endpoints/relatórios e em gate de integração, evitando que o repositório exista sem trilha operacional homologada.
*   **Próximos gates**: Homologar persistência ponta a ponta dos reports de backtest, validar leitura histórica/comparativa e adicionar walk-forward.

### M3: Paper Trading + Risk
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 78%
*   **Evidências**: O `RiskEngine` bloqueia ordens e gera `DecisionAudit`; existem `paper_wallet`, `paper_trades`, `paper_orders`, `paper_positions`, reconciliação `New -> Open`, PnL incremental de venda e PnL não-realizado em posição.
*   **Riscos/Desvios**: Ainda falta uma trilha explícita de eventos de Paper Trading para auditar o ciclo de vida inteiro das ordens e publicar/reconciliar mudanças de estado como stream operacional.
*   **Próximos gates**: Implementar eventos de Paper Trading, ordens parciais completas, State Machine rigoroso e controle de margem/exposure.

### M4: Binance Spot Testnet
*   **Status real**: Functional Prototype com gate de risco estrito concluido
*   **Percentual estimado**: 95%
*   **Evidências**: `BinanceTestnetExecutor` usa `BinanceRestClient`, valida filtros locais da exchange, mascara secrets com `SecretRedactor` e rejeita ordens sem `RiskDecision` aprovado, vigente e compativel com simbolo/lado. O endpoint REST `/api/testnet/order` recebe `RiskDecision`, pre-valida via `TestnetOrderSubmissionGuard`, registra `DecisionAudit` antes do executor e bloqueia submissao invalida.
*   **Riscos/Desvios**: A ponte REST Testnet esta implementada, mas o fluxo end-to-end com credenciais reais da Binance Spot Testnet continua opt-in e dependente de internet estavel, filtros oficiais persistidos, chaves validas e auditoria de status real sem assumir `FILLED`.
*   **Próximos gates**: Validar fluxo end-to-end opt-in com credenciais Testnet reais, registrar evidencias de sincronizacao de status e manter a regra de nunca assumir `FILLED` no modo real.

### M5: Dashboard + Observability
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 90%
*   **Evidências**: O Vite-React dashboard compila (`npm run build`), se conecta via SignalR, plota gráficos com TradingView Lightweight Charts e consome `/api/runtime/status` via `apiService.fetchRuntimeStatus()`. O badge global de `RuntimeMode` reflete Offline, Simulation, Paper, TestnetDryRun e TestnetReal com fallback seguro para Simulation.
*   **Riscos/Desvios**: O consumo canônico de `RuntimeMode` esta implementado, mas ainda faltam tracing OpenTelemetry no React e evidencias E2E reais em ambiente com backend ativo.
*   **Próximos gates**: Rodar smoke Playwright opt-in contra backend real, reforcar tracing OpenTelemetry no React e revisar estados visuais de erro/latencia.

### M6: Intelligence Layer
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 60%
*   **Evidências**: `IntelligenceSnapshotService` agrega dados e detecta volatilidade/anomalias.
*   **Riscos/Desvios**: A camada possui componentes versionados e `ModelRegistry`, mas os scores seguem heurísticos e nao representam modelos treinados com avaliacao estatistica ou inferencia ML.NET/ONNX.
*   **Próximos gates**: Integrar métricas avançadas de heurística validadas, pavimentar o caminho para inferência via ONNX/ML.NET futuro.

### M7: Adaptive Strategy Orchestration
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 70%
*   **Evidências**: O Control Plane decide dinamicamente a alocação, persiste estado/cooldown de estratégia e expõe breakdown estruturado do score.
*   **Riscos/Desvios**: Ainda falta um agregador adaptativo real que consolide eventos de trades, atribuições, métricas persistidas de M2/M3 e exploração do Multi-Armed Bandit em histórico verdadeiro.
*   **Próximos gates**: Implementar agregador adaptativo, alimentar o Multi-Armed Bandit com histórico verdadeiro e validar aprendizado usando eventos persistidos.

### M8: Hardening
*   **Status real**: Completed
*   **Percentual estimado**: 100%
*   **Evidências**: `dotnet test` passa no historico registrado, benchmarks locais estao criados, secrets sao mascarados com `SecretRedactor` em logs/auditorias, o `HardeningReportService` incorpora `RuntimeStatusService`, e os workflows separam gates obrigatorios de gates opt-in.
*   **Riscos/Desvios**: O hardening operacional esta fechado para MVP, mas o readiness final ainda depende de gates opt-in quando o ambiente exigir Docker, browsers Playwright, Native AOT ou credenciais reais da Testnet.
*   **Próximos gates**: Monitoramento continuo de logs, execucao periodica dos gates opt-in e promocao gradual de checks para CI obrigatorio quando o runner estiver padronizado.

## Revalidacao Task A - 2026-05-27

RAG consultado:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- context-pack "M9 reality check RuntimeMode Testnet REST bridge Paper Adaptive Readiness"
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Sincronizar M9 e plano paralelo com estado real" --profile code-review
```

Arquivos de codigo conferidos: `src/Api/Program.cs`, `dashboard/src/App.tsx`, `dashboard/src/services.ts`, `src/Application/Services/TestnetOrderSubmissionGuard.cs`, `BacktestRepository`, migracoes de Paper/Adaptive e `plans/30-release-readiness-report.md`.

Conclusao: Testnet REST bridge e Dashboard RuntimeMode estao implementados no codigo e devem ser marcados como feitos. A maturidade M9 ainda deve preservar a diferenca entre componentes implementados e fluxo produto end-to-end: Paper events, agregador adaptativo, persistencia de backtest homologada, relatorio final de readiness e opt-ins reais continuam pendentes ou pendentes de evidencia operacional.

## Próximos Passos (Backlog de Execução)
Para tornar o projeto **Production-Ready** em termos técnicos (sem ainda engajar capital real), seguiremos a trilha:
1.  Consolidar State Machine e PnL não realizado com eventos auditaveis no ciclo de vida do Paper Trading (`4.4`).
2.  Publicar/reconciliar eventos de ordens Paper para auditoria operacional.
3.  Homologar a persistencia de backtests com fluxo produto e comparacao historica (`4.3`).
4.  Fazer a orquestração adaptativa usar agregador persistido e dados históricos via Multi-Armed Bandit (`4.5`).
5.  Finalizar o relatório de readiness com evidencias de opt-ins reais: Testnet real, Playwright, Testcontainers, FeatureStore benchmark e Native AOT quando aplicavel.
