# 13 — Library Selection

## Critérios

Antes de adicionar biblioteca:

- [ ] manutenção ativa;
- [ ] licença adequada;
- [ ] documentação;
- [ ] compatibilidade .NET 10;
- [ ] suporte Linux;
- [ ] risco AOT;
- [ ] facilidade de teste;
- [ ] segurança.

## Candidatas principais

| Categoria | Biblioteca |
|---|---|
| Binance | Binance.Net / CryptoExchange.Net / connector oficial |
| Indicadores | Skender.Stock.Indicators |
| Persistência | Dapper + Npgsql |
| Estatística | Math.NET Numerics |
| Resiliência | Polly / Microsoft.Extensions.Resilience |
| Observabilidade | OpenTelemetry, Serilog, Prometheus, Grafana |
| ML | ML.NET, ONNX Runtime |
| Testes | xUnit, FluentAssertions, Testcontainers, BenchmarkDotNet |
| Frontend | React, TypeScript, Vite, TanStack Query, SignalR, Zod, TradingView Lightweight Charts |

## Decisões iniciais

- Dapper-first.
- Sem Python no runtime.
- ML.NET opcional e isolável.
- AOT seletivo.
- Evitar Node BFF no MVP.
