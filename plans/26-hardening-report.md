# 26 — Hardening Report

Data-base: **2026-05-20 UTC-03 / America/Maceio**.
Última revalidação local: **2026-05-21 UTC-03 / America/Maceio**.

## Objetivo

Registrar o fechamento da M8 com gates de qualidade, seguranca, performance, observabilidade, AOT seletivo e riscos conhecidos.

## Gates

- [x] build limpo: `dotnet test` compila todos os projetos antes dos testes.
- [x] testes limpos: suite xUnit validada no fechamento da atividade.
- [x] benchmarks registrados: cenarios documentados para BenchmarkDotNet e AOT seletivo.
- [x] observabilidade ativa: `/health`, `/api/metrics`, `/api/intelligence/snapshot`, `/api/adaptive/recommendation` e `/api/hardening/report`.
- [x] dashboard operacional: `npm run build` validado.
- [x] documentacao atualizada: Stage 08, master checklist, changelog e este relatorio.
- [x] riscos conhecidos registrados: lista abaixo.

## Benchmarks registrados

| Nome | Alvo | Ferramenta | Comando |
|---|---|---|---|
| IndicatorService.CalculateFeatures | Throughput de calculo de features por lote de candles | Local benchmark harness (BenchmarkDotNet-ready) | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *Indicator*` |
| FeatureStore.GetMarketDataPointsAsync | Latencia de leitura Dapper/Npgsql para backtests e orquestracao | Local benchmark harness + fixture PostgreSQL | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *FeatureStore*` |
| AdaptiveStrategyOrchestrator.Decide | Latencia do Control Plane adaptativo | Local benchmark harness (BenchmarkDotNet-ready) | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *Adaptive*` |
| ApiWorker.NativeAot.Publish | Compatibilidade AOT seletiva da API e Worker | Script local opt-in + dotnet publish | `bash tools/validate-native-aot.sh linux-x64` |

## Harness local

O projeto `tools/benchmarks/CryptoTrading.Benchmarks` executa os cenarios locais com `Stopwatch` e saida tabular. Os cenarios `IndicatorService.CalculateFeatures` e `AdaptiveStrategyOrchestrator.Decide` rodam sem dependencias externas. O cenario `FeatureStore.GetMarketDataPointsAsync` sobe PostgreSQL efemero via Testcontainers apenas quando filtrado explicitamente, mantendo compatibilidade com uma migracao futura para BenchmarkDotNet.

## Automacao CI

O workflow `.github/workflows/ci.yml` executa os gates obrigatorios rapidos em `push` e `pull_request` para `main` e `develop`:

- restore/build .NET em Release;
- `dotnet test --no-build --configuration Release`;
- build do dashboard com `npm ci` + `npm run build`;
- `git diff --check`.

O workflow `.github/workflows/hardening-gates.yml` fica restrito a `workflow_dispatch` e concentra gates manuais opt-in:

- smoke Playwright do dashboard (`run_playwright=true`);
- testes PostgreSQL/Testcontainers (`run_integration_tests=true`);
- benchmark PostgreSQL do FeatureStore (`run_featurestore_benchmark=true`);
- publish Native AOT de API/Worker (`run_native_aot=true`).

Os cenarios que dependem de Docker, browsers ou toolchain Native AOT seguem opt-in para manter o CI obrigatorio rapido e reprodutivel.

## Chaos scenarios registrados

- Missing market features: API retorna `NotFound` ou servicos rejeitam colecoes vazias.
- RiskEngine halted: sizing adaptativo retorna zero.
- DataQualityGate blocked: orquestrador mantem estrategia e zera sizing.
- Secret-bearing log payload: valores sensiveis sao mascarados.

## Riscos conhecidos

| Area | Risco | Mitigacao |
|---|---|---|
| Integration tests | Testcontainers depende de Docker disponivel no host/CI. | `dotnet test tests/IntegrationTests/CryptoTrading.IntegrationTests.csproj -c Release` fica gate manual opt-in no workflow de hardening. |
| FeatureStore benchmark | Benchmark PostgreSQL depende de Docker disponivel no host/CI. | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*FeatureStore*' --iterations 3` fica gate manual opt-in no workflow de hardening. |
| E2E tests | Playwright exige browsers e bootstrap de ambiente. | `npm run build` fica gate obrigatorio; `npm run test:e2e` fica gate manual opt-in no workflow de hardening. |
| Native AOT | Dapper e CryptoExchange.Net emitem warnings de trim/AOT durante o publish opt-in. | Manter `bash tools/validate-native-aot.sh linux-x64` como gate manual e acompanhar dependencias antes de tornar AOT obrigatorio no CI. |
| Trading runtime | Orquestracao adaptativa e inteligencia nao podem executar sem RiskEngine. | Preservar RiskEngine e DecisionAudit nos caminhos de execucao. |

## Resultado

M8 concluida como hardening operacional inicial. Os gates estao expostos em `/api/hardening/report` e visiveis no dashboard.

## Revalidação pós-M8

Consulta RAG: `proxima etapa apos M8 Hardening checklist pendente validacao build testes documentacao`.

Contexto recuperado: M8 concluida, hardening checklist completo e necessidade de manter registro de data, criterios de aceite, riscos e testes antes de novas atividades.

Evidencias locais em 2026-05-21:

- `dotnet test`: 47 testes passaram, 0 falhas, 0 ignorados.
- `npm run build` em `dashboard/`: TypeScript e Vite build passaram.
- `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Adaptive*'`: `AdaptiveStrategyOrchestrator.Decide` passou com media local de 0.1855 ms.

Escopo: checkpoint documental e de readiness; sem alteracao comportamental no runtime.

Riscos remanescentes: testes com Testcontainers/PostgreSQL, E2E Playwright e publish Native AOT continuam opt-in por dependerem de ambiente especifico.

## Gate opt-in de Native AOT

Data: 2026-05-21.

Consulta RAG: `Native AOT seletivo API Worker hardening gate opt-in publish validação pós M8`.

Fonte oficial consultada: Microsoft Learn, Native AOT deployment overview e ASP.NET Core support for Native AOT. Contexto aplicado: Native AOT deve usar `PublishAot=true` e publish para RID especifico; a analise de compatibilidade ocorre no publish e deve ser repetida cedo no ciclo de desenvolvimento.

Entrega de valor: `tools/validate-native-aot.sh` centraliza a publicacao Native AOT opt-in de API e Worker em um unico comando reprodutivel.

Critérios de aceite:

- script publica API e Worker com `PublishAot=true` para o RID informado;
- workflow `hardening-gates.yml` permite executar o gate manualmente por `workflow_dispatch`;
- gate permanece opt-in e nao bloqueia `push`/`pull_request` padrao;
- documentacao e catalogo de benchmarks refletem o comando unico.

Riscos: toolchain Native AOT e pacotes do RID podem variar por runner; por isso o gate segue manual/opt-in ate estabilizacao em imagem CI endurecida.

Evidencia local:

- `bash tools/validate-native-aot.sh linux-x64`: API e Worker publicados com sucesso em `/tmp/cryptotrading-native-aot`.
- Ajuste aplicado: leituras de configuracao que usavam `ConfigurationBinder.Get/GetValue` foram trocadas por indexer/children para evitar warnings de trimming e falhas de Native AOT.
- Observacao: o script preserva warnings AOT no output, mas desativa `TreatWarningsAsErrors` apenas no publish opt-in para permitir smoke de geracao de binario enquanto dependencias como Dapper e CryptoExchange.Net seguem sob revisao de compatibilidade.

## Consolidação do gate AOT no CI e dashboard

Data: 2026-05-21.

Consulta RAG: `dashboard hardening Native AOT pendente workflow gate validado API Worker riscos conhecidos`.

Entrega de valor: remover a publicacao Native AOT obrigatoria do workflow legado e alinhar dashboard/backend ao status real do gate opt-in.

Critérios de aceite:

- workflow `dotnet.yml` executa somente build/test padrao em push e pull request;
- workflow `hardening-gates.yml` permanece como ponto unico para Native AOT manual;
- dashboard deixa de reportar AOT como validacao pendente e passa a expor o risco real de warnings Dapper/CryptoExchange.Net;
- `HardeningReportService` registra a mesma mitigacao exibida no dashboard.

Riscos: manter dois workflows com responsabilidades sobrepostas pode gerar falso negativo no CI; por isso o publish AOT fica centralizado no workflow de hardening manual.

## Gate opt-in de Playwright

Data: 2026-05-21.

Consulta RAG: `dashboard Playwright E2E opt-in hardening smoke test`.

Fonte oficial consultada: Playwright Installation e Setting up CI. Contexto aplicado: Playwright Test é o runner E2E oficial, roda via `npx playwright test`, e em CI requer instalação dos browsers com `npx playwright install --with-deps`.

Entrega de valor: smoke E2E do dashboard validando renderização do overview, exposição dos gates de hardening e navegação para a tela de risco.

Critérios de aceite:

- `dashboard/playwright.config.ts` usa Vite preview como `webServer`;
- `dashboard/tests/e2e/dashboard-smoke.spec.ts` cobre overview, hardening e RiskEngine;
- `npm run test:e2e` executa o smoke local quando browsers Playwright estiverem instalados;
- workflow `hardening-gates.yml` oferece execução manual com `run_playwright=true`, sem bloquear push/pull request padrão.

Riscos: browsers Playwright aumentam tempo de bootstrap e dependem de pacotes do sistema no runner; por isso o gate permanece manual/opt-in.

## Gate opt-in de Testcontainers/PostgreSQL

Data: 2026-05-21.

Consulta RAG: `Testcontainers PostgreSQL opt-in integration tests FeatureStore hardening fixture`.

Fonte oficial consultada: Testcontainers for .NET PostgreSQL module e xUnit.net integration. Contexto aplicado: usar `Testcontainers.PostgreSql`, imagem PostgreSQL versionada e fixture xUnit para ciclo de vida do container.

Entrega de valor: teste de integração real do `FeatureStore` contra PostgreSQL efêmero, cobrindo schema, persistência de candles/features e leitura via `GetMarketDataPointsAsync`.

Critérios de aceite:

- projeto `tests/IntegrationTests` separado da solution principal para manter `dotnet test` leve;
- teste usa `postgres:16-alpine` pinado;
- workflow `hardening-gates.yml` oferece execução manual com `run_integration_tests=true`;
- documentação registra comando opt-in e risco de dependência de Docker.

Riscos: exige Docker disponível no host/runner e pode baixar imagem PostgreSQL; por isso não bloqueia push/pull request padrão.

Evidencia local:

- `dotnet test tests/IntegrationTests/CryptoTrading.IntegrationTests.csproj -c Release`: 1 teste passou contra PostgreSQL efêmero via Testcontainers.

## Benchmark opt-in FeatureStore/PostgreSQL

Data: 2026-05-21.

Consulta RAG: `FeatureStore.GetMarketDataPointsAsync benchmark PostgreSQL fixture Testcontainers hardening Dapper Npgsql`.

Contexto recuperado: o hardening report já registrava `FeatureStore.GetMarketDataPointsAsync` como benchmark esperado para latência Dapper/Npgsql, mas a execução ainda dependia de fixture PostgreSQL.

Entrega de valor: o harness local agora executa o benchmark de leitura do FeatureStore contra PostgreSQL efêmero via Testcontainers, com seed de 300 candles/features e validação de contagem retornada antes e durante a medição.

Critérios de aceite:

- benchmark `--filter '*FeatureStore*'` executa fixture PostgreSQL real;
- cenário permanece opt-in e não bloqueia `push`/`pull_request` padrão;
- workflow `hardening-gates.yml` permite execução manual por `run_featurestore_benchmark=true`;
- documentação e changelog refletem o novo gate.

Riscos: exige Docker disponível no host/runner e pode baixar imagem PostgreSQL; por isso continua opt-in.

Evidencia local:

- `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*FeatureStore*' --iterations 3`: benchmark passou com 300 pontos de mercado e média local de 9.5164 ms.
- `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Adaptive*' --iterations 3`: benchmark passou com média local de 0.2053 ms.
- `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Indicator*' --iterations 2`: benchmark passou com média local de 17.9755 ms.
- `dotnet test`: 47 testes passaram.
- `npm run build`: dashboard compilou em produção.
- `git diff --check`: sem problemas de whitespace.

## Sincronização hardening FeatureStore

Data: 2026-05-21.

Consulta RAG: `proximas etapas pendentes hardening checklists dashboard FeatureStore benchmark CI riscos M8`.

Entrega de valor: alinhar `HardeningReportService`, fallback do dashboard e testes unitários ao benchmark opt-in do FeatureStore, evitando divergência entre documentação, API e UI quando a API ainda não carregou.

Critérios de aceite:

- catálogo backend registra `FeatureStore.GetMarketDataPointsAsync` com fixture PostgreSQL/Testcontainers e `--iterations 3`;
- riscos conhecidos incluem a dependência operacional de Docker do benchmark;
- fallback inicial do dashboard exibe o benchmark e alerta correspondente;
- teste unitário cobre o risco e o comando do benchmark.

Riscos: manter a UI ou o relatório fora de sincronia pode mascarar gates opt-in que dependem de infraestrutura específica.

## Refresh limpo do RAG

Data: 2026-05-21.

Consulta RAG: `proxima etapa hardening report dashboard backend divergencia riscos opt-in gates release candidate`.

Contexto recuperado: a consulta retornou trecho antigo de riscos conhecidos, sinalizando que upserts sucessivos poderiam manter chunks obsoletos no Qdrant.

Entrega de valor: `CryptoTrading.RagTool` agora oferece o comando `refresh`, que recria apenas as coleções derivadas de docs/código (`cryptotrading_docs` e `cryptotrading_code`) antes de executar a ingestão completa.

Critérios de aceite:

- `ingest` continua disponível para upsert incremental;
- `refresh` recria docs/código e preserva coleções de decisões, prompts, tarefas e referências externas;
- README local documenta quando usar refresh;
- validação inclui query RAG após refresh.

Riscos: exige Qdrant local disponível; por recriar docs/código, deve ser usado como operação explícita após mudanças relevantes de documentação ou código.

Evidencia local:

- `dotnet run --project tools/CryptoTrading.RagTool -- refresh`: recriou `cryptotrading_docs` e `cryptotrading_code`, indexando 322 chunks de documentação e 300 chunks de código.
- `dotnet run --project tools/CryptoTrading.RagTool -- query "Refresh limpo do RAG Qdrant docs codigo chunks obsoletos"`: retornou o novo registro do checklist como primeiro resultado.
- `dotnet test`: 47 testes passaram.
- `npm run build`: dashboard compilou em produção.
- `git diff --check`: sem problemas de whitespace.

## Status operacional dos benchmarks

Data: 2026-05-21.

Consulta RAG: `proxima atividade pendente apos refresh RAG hardening riscos known gaps dashboard backend testes`.

Entrega de valor: o contrato `/api/hardening/report` agora diferencia benchmarks obrigatórios de smoke (`Mandatory smoke`) e validações manuais opt-in (`Opt-in validated`), e o dashboard mostra esses status junto com alertas operacionais.

Critérios de aceite:

- `BenchmarkCatalog` preenche `Status` para todos os benchmarks registrados;
- testes unitários impedem regressão para status vazio ou genérico;
- dashboard exibe benchmarks e alertas no card de hardening após carregar a API;
- documentação registra o risco de apagar a distinção entre smoke obrigatório e gate opt-in.

Riscos: se o status não for preservado no backend, a UI pode fazer gates manuais parecerem checks obrigatórios ou meramente registrados.
