# 15 — AOT and ML Service Strategy

## Decisão

AOT será seletivo por serviço. ML.NET poderá ser serviço separado.

## AOT recomendado para avaliar

- MarketData.Worker;
- Risk.Worker;
- PaperTrading.Worker;
- pequenos serviços stateless.

## Evitar AOT inicialmente em

- ML.Service;
- Dashboard/API;
- serviços com muita reflexão;
- serviços com dependências não comprovadas.

## ML.NET como serviço separado

Criar `ML.Service` se:

- ML.NET dificultar AOT;
- dependências ficarem pesadas;
- inferência precisar de ciclo próprio;
- modelo precisar de hot reload controlado;
- serviço precisar ser desligado sem afetar o core.

## Contrato sugerido

```csharp
public sealed record IntelligenceRequest(
    string Symbol,
    string Timeframe,
    DateTimeOffset Timestamp,
    decimal[] Features);

public sealed record IntelligenceResponse(
    string Symbol,
    string Regime,
    decimal RegimeConfidence,
    decimal AnomalyScore,
    decimal VolatilityScore,
    decimal SentimentScore,
    string ModelVersion);
```

## Regra

Se ML.Service cair, o core deve degradar de forma controlada.
