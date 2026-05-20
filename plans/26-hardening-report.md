# 26 — Hardening Report

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

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
| IndicatorService.CalculateFeatures | Throughput de calculo de features por lote de candles | BenchmarkDotNet | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *Indicator*` |
| FeatureStore.GetMarketDataPointsAsync | Latencia de leitura Dapper/Npgsql para backtests e orquestracao | BenchmarkDotNet + fixture PostgreSQL | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *FeatureStore*` |
| AdaptiveStrategyOrchestrator.Decide | Latencia do Control Plane adaptativo | BenchmarkDotNet | `dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter *Adaptive*` |
| Api.NativeAot.Publish | Compatibilidade AOT seletiva da API | dotnet publish | `dotnet publish src/Api/CryptoTrading.Api.csproj -c Release -r linux-x64 /p:PublishAot=true` |

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
| Native AOT | Dependencias com reflection podem falhar com `PublishAot`. | Validar API e Worker separadamente apos benchmarks. |
| Trading runtime | Orquestracao adaptativa e inteligencia nao podem executar sem RiskEngine. | Preservar RiskEngine e DecisionAudit nos caminhos de execucao. |

## Resultado

M8 concluida como hardening operacional inicial. Os gates estao expostos em `/api/hardening/report` e visiveis no dashboard.
