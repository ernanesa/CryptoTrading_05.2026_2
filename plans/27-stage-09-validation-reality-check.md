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
*   **Status real**: Functional Prototype com gate de risco estrito concluido
*   **Percentual estimado**: 90%
*   **Evidências**: `BinanceTestnetExecutor` usa `BinanceRestClient`, valida filtros locais da exchange, mascara secrets com `SecretRedactor` e rejeita ordens sem `RiskDecision` aprovado, vigente e compativel com simbolo/lado. A suite `BinanceTestnetTests` cobre ausencia, rejeicao, expiracao e mismatch de `RiskDecision`.
*   **Riscos/Desvios**: O endpoint REST `/api/testnet/order` ainda chama `ExecuteOrderAsync(order)` sem fornecer `RiskDecision`; portanto, pelo estado real do codigo, a rota rejeita ordens pelo gate estrito ate existir a ponte entre RiskEngine/orquestrador e submissao Testnet. Em modo real tambem exige internet estavel, filtros oficiais persistidos e chaves validas.
*   **Próximos gates**: Integrar a geracao/propagacao de `RiskDecision` ao endpoint Testnet, validar fluxo end-to-end opt-in com credenciais Testnet reais e manter a regra de nunca assumir `FILLED` no modo real.

### M5: Dashboard + Observability
*   **Status real**: Functional Prototype
*   **Percentual estimado**: 85%
*   **Evidências**: O Vite-React dashboard compila (`npm run build`) e se conecta via SignalR, plotando gráficos com TradingView Lightweight Charts. O backend ja expoe `/api/runtime/status` via `RuntimeStatusService`.
*   **Riscos/Desvios**: O dashboard ainda deriva modo principalmente do estado de conexao local (`Testnet Real` versus `Simulation`) e nao consome o endpoint `/api/runtime/status` como fonte canonica para Offline, Simulation, Paper, TestnetDryRun e TestnetReal.
*   **Próximos gates**: Consumir `/api/runtime/status`, exibir badge global de `RuntimeMode`, separar claramente Offline/Simulation/Paper/Testnet e expor tracing OpenTelemetry no React.

### M6: Intelligence Layer
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 60%
*   **Evidências**: `IntelligenceSnapshotService` agrega dados e detecta volatilidade/anomalias.
*   **Riscos/Desvios**: A camada possui componentes versionados e `ModelRegistry`, mas os scores seguem heurísticos e nao representam modelos treinados com avaliacao estatistica ou inferencia ML.NET/ONNX.
*   **Próximos gates**: Integrar métricas avançadas de heurística validadas, pavimentar o caminho para inferência via ONNX/ML.NET futuro.

### M7: Adaptive Strategy Orchestration
*   **Status real**: Heuristic Prototype
*   **Percentual estimado**: 60%
*   **Evidências**: O Control Plane decide dinamicamente a alocação e realiza trocas baseadas em escores heurísticos.
*   **Riscos/Desvios**: O Multi-Armed Bandit não usa os dados reais persistidos da Fase M2/M3 para o aprendizado e exploração. O cooldown e histerese operam em memória.
*   **Próximos gates**: Usar persistência de métricas, cooldown no DB, e alimentar o Multi-Armed Bandit com histórico verdadeiro.

### M8: Hardening
*   **Status real**: Completed
*   **Percentual estimado**: 100%
*   **Evidências**: `dotnet test` passa no historico registrado, benchmarks locais estao criados, secrets sao mascarados com `SecretRedactor` em logs/auditorias, o `HardeningReportService` incorpora `RuntimeStatusService`, e os workflows separam gates obrigatorios de gates opt-in.
*   **Riscos/Desvios**: O hardening operacional esta fechado para MVP, mas o readiness final ainda depende de gates opt-in quando o ambiente exigir Docker, browsers Playwright, Native AOT ou credenciais reais da Testnet.
*   **Próximos gates**: Monitoramento continuo de logs, execucao periodica dos gates opt-in e promocao gradual de checks para CI obrigatorio quando o runner estiver padronizado.

## Revalidacao Task A - 2026-05-27

RAG consultado:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- context-pack "M9 reality check Binance RiskDecision RuntimeMode RAG context-pack release readiness"
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Atualizar M9 com estado real do projeto"
```

Arquivos de codigo conferidos: `BinanceTestnetExecutor`, `RuntimeStatusService`, `RuntimeMode`, `HardeningReportService`, endpoint `/api/runtime/status`, endpoint `/api/testnet/order`, `BinanceTestnetTests` e dashboard `App.tsx`/store.

Conclusao: Tasks B, C e D existem no codigo, mas a maturidade M9 deve preservar a diferenca entre componentes implementados e fluxo produto end-to-end. O gate `RiskDecision` da Testnet esta correto e restritivo; a rota REST de submissao ainda precisa receber uma decisao de risco valida para executar. O `RuntimeMode` existe no backend e no store do dashboard, mas o dashboard ainda precisa usar o endpoint canonico como fonte de verdade.

## Próximos Passos (Backlog de Execução)
Para tornar o projeto **Production-Ready** em termos técnicos (sem ainda engajar capital real), seguiremos a trilha:
1.  Implementar State Machine e PnL não realizado no Paper Trading (`4.4`).
2.  Tornar o backtesting persistente com novas métricas avançadas (`4.3`).
3.  Fazer a orquestração adaptativa usar os dados históricos via Multi-Armed Bandit (`4.5`).
4.  Conectar o endpoint Testnet ao `RiskDecision` emitido pelo RiskEngine/orquestrador antes de qualquer submissao real.
5.  Adequar o Dashboard para refletir claramente o modo de operação (`RuntimeMode`) e as métricas expandidas (`4.7`).
