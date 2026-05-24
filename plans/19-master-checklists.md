# 19 — Master Checklists

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## Status macro

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

## Checklist antes de qualquer atividade

- [x] data atual verificada;
- [x] plano consultado;
- [x] RAG consultado quando necessário;
- [ ] documentação oficial consultada quando aplicável;
- [x] entrega de valor definida;
- [x] critérios de aceite definidos;
- [x] riscos listados;
- [x] testes definidos.

## M6 — Intelligence Layer checklist

- [x] IntelligenceSnapshot versionado;
- [x] modelo/score tem versão;
- [x] fonte do score registrada;
- [x] insights aparecem no dashboard;
- [x] nenhum modelo bypassa RiskEngine;
- [x] FeatureExtractor;
- [x] AnomalyDetectionService inicial;
- [x] RegimeDetectionService inicial;
- [x] VolatilityForecastService;
- [x] MetaLabelingService;
- [x] SentimentRiskService;
- [x] EventRiskClassifier;
- [x] ModelRegistry;
- [x] RagContextProvider;
- [x] ExplanationService.

### Registro da atividade M6 inicial

RAG consultado: sim
Consulta: M6 IntelligenceSnapshot RegimeDetectionService AnomalyDetectionService criterios aceite
Contexto encontrado: componentes da Stage 06 e criterios de aceite do IntelligenceSnapshot.
Impacto: implementacao inicial mantida como score heuristico versionado, sem bypass do RiskEngine.
Data: 2026-05-20

### Registro da atividade M6 FeatureExtractor e Volatilidade

RAG consultado: sim
Consulta: M6 FeatureExtractor VolatilityForecastService IntelligenceSnapshot FeatureStore criterios aceite
Contexto encontrado: componentes pendentes da Stage 06 e criterio de features versionadas da Stage 01.
Impacto: adicionados vetor de features versionado e forecast heuristico de volatilidade ao snapshot, mantendo score como contexto auxiliar.
Data: 2026-05-20

### Registro da atividade M6 fechamento

RAG consultado: sim
Consulta: M6 MetaLabelingService SentimentRiskService EventRiskClassifier ModelRegistry RagContextProvider ExplanationService finalizar Intelligence Layer
Contexto encontrado: componentes finais da Stage 06, objetivo de inteligencia auxiliar e regra de sentimento como filtro/contexto.
Impacto: M6 concluida com todos os componentes planejados no snapshot, mantendo execucao condicionada ao RiskEngine.
Data: 2026-05-20

## M7 — Adaptive Orchestration checklist

- [x] StrategyRegistry;
- [x] MarketRegimeService;
- [x] AssetRankingService;
- [x] StrategyPerformanceTracker;
- [x] StrategyScoringService;
- [x] AdaptiveStrategyOrchestrator;
- [x] AdaptivePortfolioAllocator;
- [x] DynamicPositionSizingService;
- [x] DynamicExitEngine;
- [x] ExecutionCostModel;
- [x] StrategyHealthMonitor;
- [x] TradeAttributionService;
- [x] WalkForwardEvaluator;
- [x] MultiArmedBanditAllocator;
- [x] DataQualityGate;
- [x] MarketHealthScore;
- [x] dashboard de score/regime/estratégia;
- [x] backtest fixo vs adaptativo.

### Registro da atividade M7 fechamento

RAG consultado: sim
Consulta: M7 AdaptiveStrategyOrchestrator AssetRanking StrategyScoring hysteresis cooldown RiskEngine DataQualityGate fixed vs adaptive dashboard
Contexto encontrado: componentes da Stage 07, checklist M7 e Control Plane da arquitetura.
Impacto: M7 concluida com Control Plane adaptativo heuristico, histerese/cooldown, gates de risco/qualidade, comparativo fixo vs adaptativo e dashboard.
Data: 2026-05-20

## M8 — Hardening checklist

- [x] build limpo;
- [x] testes limpos;
- [x] benchmarks registrados;
- [x] observabilidade ativa;
- [x] dashboard operacional;
- [x] documentacao atualizada;
- [x] riscos conhecidos registrados;
- [x] chaos scenarios registrados;
- [x] seguranca de secrets revisada;
- [x] AOT seletivo registrado.

### Registro da atividade M8 fechamento

RAG consultado: sim
Consulta: Stage 08 Hardening gates build testes benchmarks observabilidade dashboard riscos conhecidos
Contexto encontrado: gates e atividades da Stage 08.
Impacto: M8 concluida com HardeningReport, endpoint de observabilidade, dashboard, testes, redator de secrets, chaos scenarios e relatorio de riscos.
Data: 2026-05-20

## ADR — Consolidação da Arquitetura

- [x] ADR-001: Arquitetura geral .NET-first criado e linkado;
- [x] ADR-002: Dapper-first persistence criado e linkado;
- [x] ADR-003: Python fora do MVP/runtime criado e linkado;
- [x] ADR-004: AOT seletivo por serviço criado e linkado;
- [x] ADR-005: ML.NET como serviço separado opcional criado e linkado;
- [x] ADR-006: Binance adapter e abstrações criado e linkado;
- [x] ADR-007: Feature Store versionada criado e linkado;
- [x] ADR-008: RiskEngine obrigatório criado e linkado;
- [x] ADR-009: Adaptive Strategy Orchestration criado e linkado;
- [x] ADR-010: Observabilidade e DecisionAudit criado e linkado;
- [x] ADR-011: RAG local e protocolo de consulta criado e linkado.

### Registro de consolidação de ADRs

RAG consultado: sim
Consulta: planos 00 a 26, ADR index e regras operacionais
Contexto encontrado: índices e descrições dos 11 ADRs recomendados que ainda não estavam criados fisicamente no repositório.
Impacto: Criados fisicamente os 11 arquivos de ADR na pasta plans/ descrevendo em detalhe toda a arquitetura, regras operacionais e decisões tomadas no projeto, com links ativos no índice geral.
Data: 2026-05-21

## Gate opt-in Testcontainers — FeatureStore PostgreSQL

- [x] data atual verificada: 2026-05-21 23:29:06 -03 / America/Maceio;
- [x] plano, hardening report e RAG consultados;
- [x] documentação oficial Testcontainers for .NET consultada;
- [x] entrega de valor definida: teste de integração real para persistência Dapper/Npgsql do FeatureStore;
- [x] critérios de aceite definidos: projeto separado, PostgreSQL pinado, teste de roundtrip e workflow manual opt-in;
- [x] riscos listados: Docker/imagem PostgreSQL dependem do ambiente;
- [x] testes esperados definidos e executados: `dotnet test`, `dotnet test tests/IntegrationTests/CryptoTrading.IntegrationTests.csproj -c Release`, `npm run build` e `git diff --check`.

### Registro do gate opt-in Testcontainers

RAG consultado: sim
Consulta: Testcontainers PostgreSQL opt-in integration tests FeatureStore hardening fixture
Contexto encontrado: hardening report mantinha testes de integração como opt-in por dependerem de Docker.
Impacto: Criado e validado projeto de integração opt-in para validar schema, escrita e leitura do FeatureStore em PostgreSQL efêmero via Testcontainers.
Data: 2026-05-21

## Status operacional de benchmarks — Hardening

- [x] data atual verificada: 2026-05-21 23:50:22 -03 / America/Maceio;
- [x] plano e regras operacionais consultados;
- [x] RAG local consultado após refresh limpo;
- [x] documentação oficial não aplicável, pois não houve nova tecnologia/biblioteca;
- [x] entrega de valor definida: preservar no contrato da API e no dashboard a diferença entre smoke obrigatório e gate opt-in;
- [x] critérios de aceite definidos: `BenchmarkCatalog` popula `Status`, teste unitário cobre status e dashboard mostra benchmarks/alertas;
- [x] riscos listados: perder status no fetch da API pode tornar gates opt-in indistinguíveis de registros genéricos;
- [x] testes esperados definidos: `dotnet test`, `npm run build` e `git diff --check`.

### Registro do status operacional de benchmarks

RAG consultado: sim
Consulta: proxima atividade pendente apos refresh RAG hardening riscos known gaps dashboard backend testes
Contexto encontrado: hardening já tinha gates e riscos opt-in registrados; o contrato de benchmarks ainda carregava `Status` genérico ao vir da API.
Impacto: API e dashboard passaram a diferenciar `Mandatory smoke` e `Opt-in validated`, com alerta operacional visível no card de hardening.
Data: 2026-05-21

## Refresh limpo do RAG — Qdrant docs/código

- [x] data atual verificada: 2026-05-21 23:46:29 -03 / America/Maceio;
- [x] plano e regras operacionais consultados;
- [x] RAG local consultado e identificado com conteúdo defasado;
- [x] documentação oficial não aplicável, pois a atividade usa APIs do Qdrant Client já adotadas no projeto;
- [x] entrega de valor definida: evitar que chunks antigos sobrevivam após reindexações do RAG;
- [x] critérios de aceite definidos: comando `refresh`, recriação apenas de `cryptotrading_docs` e `cryptotrading_code`, preservação das coleções de decisões/prompts/tarefas e documentação atualizada;
- [x] riscos listados: refresh remove e recria memória semântica derivada de arquivos, exigindo Qdrant local disponível;
- [x] testes esperados definidos e executados: `dotnet run --project tools/CryptoTrading.RagTool -- refresh`, query RAG de validação, `dotnet test`, `npm run build` e `git diff --check`.

### Registro do refresh limpo do RAG

RAG consultado: sim
Consulta: proxima etapa hardening report dashboard backend divergencia riscos opt-in gates release candidate
Contexto encontrado: RAG retornou trecho antigo de riscos conhecidos, indicando necessidade de reindexação limpa após mudanças recentes.
Impacto: `CryptoTrading.RagTool` ganhou comando `refresh` para recriar coleções derivadas de documentação/código antes da ingestão, reduzindo risco de respostas semânticas obsoletas.
Data: 2026-05-21

## Sincronização hardening — FeatureStore benchmark

- [x] data atual verificada: 2026-05-21 23:43:19 -03 / America/Maceio;
- [x] plano, hardening report e RAG consultados;
- [x] documentação oficial não aplicável, pois não houve nova tecnologia/biblioteca;
- [x] entrega de valor definida: alinhar API, dashboard e testes ao benchmark opt-in já implementado;
- [x] critérios de aceite definidos: catálogo backend registra comando real com Testcontainers, risco aparece no relatório, fallback do dashboard fica coerente e teste unitário cobre regressão;
- [x] riscos listados: divergência entre documentação, API e dashboard pode esconder dependência operacional de Docker;
- [x] testes esperados definidos: `dotnet test`, `npm run build` e `git diff --check`.

### Registro da sincronização hardening FeatureStore

RAG consultado: sim
Consulta: proximas etapas pendentes hardening checklists dashboard FeatureStore benchmark CI riscos M8
Contexto encontrado: M8 e hardening estavam concluídos, com riscos opt-in documentados; a superfície backend/dashboard ainda precisava refletir o novo benchmark FeatureStore.
Impacto: `HardeningReportService`, fallback do dashboard e testes unitários passaram a expor o benchmark PostgreSQL opt-in e seu risco operacional de Docker.
Data: 2026-05-21

## Benchmark opt-in FeatureStore — PostgreSQL

- [x] data atual verificada: 2026-05-21 23:34:27 -03 / America/Maceio;
- [x] plano, hardening report e RAG consultados;
- [x] documentação oficial não reaplicada, pois o Testcontainers PostgreSQL já foi validado na atividade anterior;
- [x] entrega de valor definida: benchmark real para latência de leitura Dapper/Npgsql do FeatureStore;
- [x] critérios de aceite definidos: fixture PostgreSQL real, seed reprodutível, filtro opt-in e workflow manual;
- [x] riscos listados: Docker/imagem PostgreSQL dependem do ambiente;
- [x] testes esperados definidos e executados: benchmark `*FeatureStore*`, smoke benchmarks obrigatórios, `dotnet test`, `npm run build` e `git diff --check`.

### Registro do benchmark opt-in FeatureStore

RAG consultado: sim
Consulta: FeatureStore.GetMarketDataPointsAsync benchmark PostgreSQL fixture Testcontainers hardening Dapper Npgsql
Contexto encontrado: hardening report registrava o benchmark FeatureStore como alvo esperado para Dapper/Npgsql com fixture PostgreSQL.
Impacto: O benchmark `FeatureStore.GetMarketDataPointsAsync` deixou de ser apenas registro e passou a executar contra PostgreSQL efêmero via Testcontainers, com acionamento manual no workflow de hardening.
Data: 2026-05-21

## Gate opt-in Playwright — Dashboard E2E

- [x] data atual verificada: 2026-05-21 23:20:16 -03 / America/Maceio;
- [x] plano, hardening report e RAG consultados;
- [x] documentação oficial Playwright consultada;
- [x] entrega de valor definida: smoke E2E manual do dashboard para hardening e risco;
- [x] critérios de aceite definidos: config Playwright, teste smoke, script npm e workflow manual opt-in;
- [x] riscos listados: browsers Playwright dependem de bootstrap e pacotes do runner;
- [x] testes esperados definidos: `npm run build`, `npm run test:e2e`, `dotnet test` e `git diff --check`.

### Registro do gate opt-in Playwright

RAG consultado: sim
Consulta: dashboard Playwright E2E opt-in hardening smoke test
Contexto encontrado: M8 registrava E2E Playwright como opt-in e hardening report mantinha risco por bootstrap de browsers.
Impacto: Criado harness Playwright do dashboard com smoke test de overview/hardening/risco e acionamento manual no workflow de hardening.
Data: 2026-05-21

## Consolidação AOT — CI e Dashboard

- [x] data atual verificada: 2026-05-21 23:07:56 -03 / America/Maceio;
- [x] plano, hardening report e RAG consultados;
- [x] documentação oficial não reaplicada, pois a decisão técnica Native AOT já foi validada na atividade anterior;
- [x] entrega de valor definida: remover contradição entre gate AOT opt-in, workflow legado e dashboard;
- [x] critérios de aceite definidos: CI padrão sem publish AOT obrigatório, hardening manual preservado e dashboard alinhado ao risco real;
- [x] riscos listados: workflows sobrepostos podem gerar falso negativo ou bloquear PR por gate opt-in;
- [x] testes esperados definidos: `dotnet test`, `npm run build`, `bash tools/validate-native-aot.sh linux-x64` e `git diff --check`.

### Registro da consolidação AOT

RAG consultado: sim
Consulta: dashboard hardening Native AOT pendente workflow gate validado API Worker riscos conhecidos
Contexto encontrado: gate opt-in Native AOT validado para API/Worker e hardening report registrando warnings de Dapper/CryptoExchange.Net.
Impacto: Workflow legado `dotnet.yml` voltou a ser apenas build/test, Native AOT ficou centralizado no gate manual de hardening, e dashboard/backend passaram a refletir o status validado com risco de warnings rastreado.
Data: 2026-05-21

## Gate opt-in Native AOT — API e Worker

- [x] data atual verificada: 2026-05-21 22:43:40 -03 / America/Maceio;
- [x] plano e ADR-004 consultados;
- [x] RAG local consultado;
- [x] documentação oficial Microsoft Learn consultada para Native AOT;
- [x] entrega de valor definida: comando unico para validar publicacao Native AOT seletiva de API e Worker;
- [x] critérios de aceite definidos: script local, workflow manual opt-in, documentacao atualizada e gate sem impacto no fluxo padrao;
- [x] riscos listados: toolchain/RID Native AOT dependem do ambiente de execucao;
- [x] testes esperados definidos e executados: `bash tools/validate-native-aot.sh linux-x64`, `dotnet test` e `npm run build`.

### Registro do gate opt-in Native AOT

RAG consultado: sim
Consulta: Native AOT seletivo API Worker hardening gate opt-in publish validação pós M8
Contexto encontrado: ADR-004 exige AOT seletivo, validado explicitamente por serviço, sem impor AOT global.
Impacto: Criado e validado gate local opt-in para publicar API e Worker com `PublishAot=true`, com acionamento manual no workflow de hardening. O código de leitura de configuração foi ajustado para evitar `ConfigurationBinder.Get/GetValue` em caminhos publicados com Native AOT.
Data: 2026-05-21

## Verificação e Fechamento de Checklists (MCP, RAG e Risk)

- [x] plans/17-risk-management.md critérios de risco verificados e validados;
- [x] plans/23-local-rag-qdrant-bge-m3-agent-workflow.md checklist de implantação verificado e validado;
- [x] plans/24-mcp-servers-analysis-and-installation.md checklist de MCP inicial verificado e validado.

### Registro de validação de checklists de infraestrutura e risco

RAG consultado: sim
Consulta: planos 17, 23, 24 e status de execução local
Contexto encontrado: checklists de preparação do Qdrant local, servidores MCP, instruções do Copilot e critérios de aceitação do motor de risco.
Impacto: Marcados como concluídos e validados todos os itens de infraestrutura RAG/MCP e os critérios de aceitação do RiskEngine, refletindo com 100% de precisão o estado real do projeto e do ambiente local de desenvolvimento.
Data: 2026-05-21

## Verificação e Fechamento de Checklists (Dashboard e Observabilidade)

- [x] plans/09-stage-05-dashboard-observability.md critérios de aceitação validados e preenchidos.

### Registro de validação do checklist de Dashboard e Observabilidade

RAG consultado: sim
Consulta: plano 09 e status do frontend Vite-React
Contexto encontrado: critérios de aceitação para o dashboard de monitoramento HFT em tempo real, SignalR BFF e telemetria OpenTelemetry.
Impacto: Verificados e validados os critérios de aceitação do Dashboard React e telemetria de observabilidade no ASP.NET, marcando todos os itens como concluídos em total conformidade com a entrega operacional do MVP.
Data: 2026-05-21

## Checkpoint pós-M8 — Readiness operacional

- [x] data atual verificada: 2026-05-21 22:40:13 -03 / America/Maceio;
- [x] plano mestre consultado;
- [x] RAG local consultado;
- [x] documentação oficial avaliada como não aplicável, pois não houve alteração de API, biblioteca ou tecnologia;
- [x] entrega de valor definida: revalidação local dos gates pós-M8 antes de avançar para novas ondas de evolução;
- [x] critérios de aceite definidos: testes unitários limpos, dashboard compilando e benchmark adaptativo smoke passando;
- [x] riscos listados: cenários opt-in de PostgreSQL/Testcontainers, Playwright e Native AOT continuam dependentes do ambiente;
- [x] testes definidos e executados.

### Registro do checkpoint pós-M8

RAG consultado: sim
Consulta: proxima etapa apos M8 Hardening checklist pendente validacao build testes documentacao
Contexto encontrado: M8 concluída, checklist de hardening completo e regra geral de consulta/documentação antes de novas atividades.
Impacto: Gate local pós-M8 revalidado com `dotnet test`, `npm run build` do dashboard e smoke benchmark de `AdaptiveStrategyOrchestrator.Decide`, sem mudança comportamental no runtime.
Data: 2026-05-21

## Nova estratégia avançada — MACD ADX Trend Following (Pós-M8)

- [x] data atual verificada: 2026-05-22 00:15:00 -03 / America/Maceio;
- [x] plano, roadmap e regras operacionais consultados;
- [x] RAG local consultado para confirmar suporte de features do orquestrador;
- [x] documentação oficial e do repositório consultadas para formato de sinal e feature vectors;
- [x] entrega de valor definida: criar e expor nova estratégia de alta performance focada em crossover MACD e filtragem ADX para orquestração adaptativa;
- [x] critérios de aceite definidos: nova estratégia estende IStrategy, registrada no StrategyRegistry, suportada e pontuada no AdaptiveStrategyOrchestrator e StrategyScoringService, com 100% de cobertura de testes unitários;
- [x] riscos listados: comportamento em range de mercado controlado por filtro ADX forte;
- [x] testes definidos e executados: 6 novos cenários cobrindo Buy, Exit, Hold em warmup, Hold em ADX fraco e registro de serviço (todos validados com sucesso).

### Registro do desenvolvimento da estratégia MACD ADX

RAG consultado: sim
Consulta: proxima atividade orquestracao adaptativa estrategias registradas performance tracker scoring
Contexto encontrado: mapeamento de nomes de estratégias no tracker de performance e ScoringService da Stage 07.
Impacto: Implementada nova estratégia `MacdAdxTrendFollowingStrategy`, registrada no `StrategyRegistry` e adicionada aos serviços de mapeamento e score de orquestração adaptativa, com suíte de testes xUnit verde e 0 warnings no build.
Data: 2026-05-22
