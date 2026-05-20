# 05 — Stage 01: Market Data + Feature Store

## Objetivo

Coletar dados públicos da Binance, normalizar candles/ticks e persistir features reutilizáveis por backtest, estratégias e orquestrador.

## Entrega de valor

Candles e features confiáveis para análise e simulação.

## Componentes

- BinanceMarketDataAdapter;
- CandleNormalizer;
- IndicatorService;
- FeatureStore;
- DataQualityGate inicial;
- MarketDataIngestionWorker.

## Bibliotecas

- Binance.Net ou connector oficial;
- Skender.Stock.Indicators;
- Dapper;
- Npgsql;
- Polly.

## Features iniciais

- OHLCV;
- returns;
- EMA 9/21/50/200;
- RSI 14;
- MACD;
- ATR 14;
- Bollinger Bands;
- ADX;
- volume z-score;
- spread;
- order book imbalance simples.

## Critérios de aceite

- [ ] candles coletados;
- [ ] candles persistidos;
- [ ] indicadores calculados por biblioteca externa;
- [ ] features versionadas;
- [ ] falhas de rede tratadas;
- [ ] dados ruins bloqueados pelo DataQualityGate.
