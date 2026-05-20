# 01 — Technology Baseline

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

## Stack principal

| Área | Decisão |
|---|---|
| Backend | .NET 10 + C# 14 |
| Frontend | React + TypeScript + Vite |
| API/BFF | ASP.NET Core + SignalR |
| Exchange inicial | Binance Spot/Testnet |
| Persistência | PostgreSQL + Dapper-first |
| ORM | Evitar EF Core no caminho crítico; opcional para migrations/admin se justificar |
| Indicadores | Skender.Stock.Indicators |
| Estatística | Math.NET Numerics |
| Resiliência | Polly / Microsoft.Extensions.Resilience |
| Observabilidade | OpenTelemetry + Serilog + Prometheus + Grafana |
| ML | ML.NET e/ou ONNX Runtime, preferencialmente isolados |
| Python | Fora do MVP e fora do runtime |
| AOT | Seletivo por serviço, após benchmark |
| Testes | xUnit, FluentAssertions, Testcontainers, BenchmarkDotNet, Playwright |

## Decisões

### .NET-first

O projeto será .NET-first. A lógica central, workers, API, serviços de orquestração, risco, backtesting e execução devem priorizar .NET.

### Dapper-first

Caminhos de alta leitura/escrita devem usar Dapper/Npgsql:

- candles;
- features;
- backtest reads;
- métricas;
- auditoria;
- relatórios.

### Python fora do MVP/runtime

Python não será dependência do MVP nem do runtime. Pode ser avaliado futuramente apenas como laboratório isolado, caso haja justificativa técnica.

### AOT seletivo

Native AOT será avaliado por serviço, nunca aplicado globalmente antes de benchmark e validação de compatibilidade.

### ML isolável

ML.NET pode rodar como serviço separado para evitar acoplamento, dependências pesadas e conflitos com AOT.
