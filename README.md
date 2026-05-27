# CryptoTrading 05.2026 v2

Data-base do planejamento: **20/05/2026 UTC-03 / America/Maceio**.

## Visão

Construir um **robô de trading autônomo para criptoativos**, com arquitetura .NET-first, validação progressiva, gestão de risco, auditoria completa, backtesting, paper trading, Binance Spot Testnet, dashboard operacional, inteligência auxiliar e orquestração adaptativa de estratégias.

O objetivo técnico é criar um sistema que consiga:

- coletar dados de mercado com confiabilidade;
- calcular indicadores e features versionadas;
- testar estratégias com métricas objetivas;
- simular decisões em paper trading;
- validar fluxos na Binance Spot Testnet;
- controlar risco de forma centralizada;
- explicar todas as decisões;
- selecionar dinamicamente estratégia, ativo, timeframe, peso e tamanho;
- comparar estratégias fixas contra orquestração adaptativa;
- evoluir com testes, métricas e documentação.

## Stack inicial

| Área | Decisão |
|---|---|
| Backend | .NET 10 + C# 14 |
| Frontend | React + TypeScript + Vite |
| API/BFF | ASP.NET Core + SignalR |
| Exchange inicial | Binance Spot/Testnet |
| Persistência | PostgreSQL + Dapper-first |
| Indicadores | Skender.Stock.Indicators |
| Resiliência | Polly / Microsoft.Extensions.Resilience |
| Observabilidade | OpenTelemetry + Serilog + Prometheus + Grafana |
| ML | ML.NET e/ou ONNX Runtime, preferencialmente isolados |
| AOT | Seletivo por serviço, após benchmark |
| Testes | xUnit, FluentAssertions, Testcontainers, BenchmarkDotNet, Playwright |

## Decisões importantes

- **.NET-first**: o core do sistema será em .NET.
- **Dapper-first**: caminhos críticos de dados usam SQL explícito e Dapper/Npgsql.
- **Python fora do MVP/runtime**: só poderá ser avaliado futuramente como laboratório separado.
- **AOT seletivo**: Native AOT não será imposto globalmente.
- **ML isolável**: ML.NET pode virar serviço separado se necessário.
- **Orquestração adaptativa**: o robô deve evoluir para escolher dinamicamente estratégia, ativo, timeframe e alocação.
- **Risco centralizado**: nenhuma decisão operacional deve ignorar o RiskEngine.
- **Auditoria total**: toda decisão importante deve gerar DecisionAudit.

## Roadmap resumido

```text
M0 Foundation
  ↓
M1 Market Data + Feature Store
  ↓
M2 Backtesting + Strategy Lab
  ↓
M3 Paper Trading + Risk
  ↓
M4 Binance Spot Testnet
  ↓
M5 Dashboard + Observability
  ↓
M6 Intelligence Layer
  ↓
M7 Adaptive Strategy Orchestration
  ↓
M8 Hardening
  ↓
M9 Validation & Reality Check
```

Status atual: **M0 a M8 reavaliados** na nova fase de refinamento M9 iniciada em 2026-05-24, com o objetivo de elevar o grau de prontidão de produção e substituir heurísticas por integrações reais. Detalhes em [`plans/27-stage-09-validation-reality-check.md`](./plans/27-stage-09-validation-reality-check.md).

Benchmarks locais:

```bash
dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Adaptive*'
dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Indicator*'
dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*FeatureStore*' --iterations 3
```

O benchmark `*FeatureStore*` e opt-in e exige Docker disponivel, pois sobe PostgreSQL efemero via Testcontainers.

Gate opt-in de Native AOT:

```bash
bash tools/validate-native-aot.sh linux-x64
```

Smoke E2E opt-in do dashboard:

```bash
cd dashboard
npx playwright install chromium
npm run test:e2e
```

Testes de integração opt-in com PostgreSQL/Testcontainers:

```bash
dotnet test tests/IntegrationTests/CryptoTrading.IntegrationTests.csproj -c Release
```

CI de hardening:

- `.github/workflows/hardening-gates.yml` valida build, testes, dashboard e smoke benchmarks.
- O mesmo workflow permite rodar manualmente o gate Native AOT com `workflow_dispatch` e `run_native_aot=true`.
- O mesmo workflow permite rodar manualmente o smoke Playwright com `workflow_dispatch` e `run_playwright=true`.
- O mesmo workflow permite rodar manualmente os testes PostgreSQL/Testcontainers com `workflow_dispatch` e `run_integration_tests=true`.
- O mesmo workflow permite rodar manualmente o benchmark PostgreSQL do FeatureStore com `workflow_dispatch` e `run_featurestore_benchmark=true`.
- `.github/workflows/dotnet.yml` fica restrito ao build/test padrão; Native AOT permanece no gate manual opt-in.

## Documentação

Toda a documentação inicial está em [`plans/`](./plans/README.md).

## Regra de trabalho

Antes de qualquer atividade:

1. Verificar a data atual.
2. Pesquisar se houver dúvida.
3. Consultar o RAG local quando houver contexto técnico relevante.
4. Planejar antes de implementar.
5. Criar ou atualizar checklist.
6. Executar testes.
7. Atualizar documentação ao final.

## Repositórios anteriores

- `ernanesa/CryptoTrading_v5.0`: referência de padrões, serviços e ideias adaptativas.
- `ernanesa/CryptoTrading_05.2026`: referência de arquitetura e planejamento anterior.
- `ernanesa/Bettina`: referência histórica de pesquisa.

Esses repositórios são apenas leitura para este ciclo.
- Adicionado integração Real com Binance Testnet (opcional, protegido por RiskEngine)
