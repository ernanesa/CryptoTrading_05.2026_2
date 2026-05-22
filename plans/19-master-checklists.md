# 19 — Master Checklists

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## Status macro

| Fase | Status |
|---|---|
| M0 Foundation | Completed |
| M1 Market Data + Feature Store | Completed |
| M2 Backtesting + Strategy Lab | Completed |
| M3 Paper Trading + Risk | Completed |
| M4 Binance Spot Testnet | Completed |
| M5 Dashboard + Observability | Completed |
| M6 Intelligence Layer | Completed |
| M7 Adaptive Strategy Orchestration | Completed |
| M8 Hardening | Completed |

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
