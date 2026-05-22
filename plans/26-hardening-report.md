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

O projeto `tools/benchmarks/CryptoTrading.Benchmarks` executa os cenarios locais com `Stopwatch` e saida tabular. Ele evita dependencia de rede no ambiente de desenvolvimento atual e mantem comandos compativeis com uma migracao futura para BenchmarkDotNet.

## Automacao CI

O workflow `.github/workflows/hardening-gates.yml` executa os gates de hardening em `push` e `pull_request` para `main` e `develop`:

- build e testes .NET em Release via `CryptoTrading.UnitTests`;
- build do dashboard com `npm ci` + `npm run build`;
- smoke benchmark de `AdaptiveStrategyOrchestrator.Decide`;
- smoke benchmark de `IndicatorService.CalculateFeatures`.

Os cenarios `FeatureStore.GetMarketDataPointsAsync` e `ApiWorker.NativeAot.Publish` seguem opt-in porque dependem, respectivamente, de fixture PostgreSQL e toolchain/AOT especifico.

## Chaos scenarios registrados

- Missing market features: API retorna `NotFound` ou servicos rejeitam colecoes vazias.
- RiskEngine halted: sizing adaptativo retorna zero.
- DataQualityGate blocked: orquestrador mantem estrategia e zera sizing.
- Secret-bearing log payload: valores sensiveis sao mascarados.

## Riscos conhecidos

| Area | Risco | Mitigacao |
|---|---|---|
| Integration tests | Testcontainers depende de Docker disponivel no host/CI. | Manter testes de integracao opt-in ate runner Docker estar garantido. |
| E2E tests | Playwright exige browsers e bootstrap de ambiente. | `npm run build` fica gate obrigatorio; instalar Playwright em imagem CI endurecida. |
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
