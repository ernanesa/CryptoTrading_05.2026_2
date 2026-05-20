# 21 — Planning Changelog

| Data | Alteração |
|---|---|
| 2026-05-20 | Criação inicial do planejamento completo no novo repositório `ernanesa/CryptoTrading_05.2026_2` |
| 2026-05-20 | Definido novo repositório como único alvo de escrita |
| 2026-05-20 | Definida arquitetura .NET-first, Dapper-first, Python fora do MVP e AOT seletivo |
| 2026-05-20 | Incluída etapa oficial de Adaptive Strategy Orchestration |
| 2026-05-20 | Substituição completa do RAG Python por um sistema 100% C# .NET 10 (`CryptoTrading.RagTool`) integrado ao Qdrant nativo executado no host |
| 2026-05-20 | Implementação completa da M1: Market Data & Feature Store — Ingestão de candles da Binance, DataQualityGate, cálculo de indicadores técnicos (EMA, RSI, MACD, ATR, BB, ADX) e persistência em PostgreSQL via Dapper |
| 2026-05-20 | Infraestrutura Docker Compose configurada: PostgreSQL 16 Alpine + Qdrant v1.18 em containers gerenciados |
| 2026-05-20 | Adicionadas e validadas features adicionais de M1: Returns, Volume Z-Score, Spread e Imbalance (Taker Buy) no pipeline e banco de dados |
| 2026-05-20 | Implementação completa da M2: Backtesting + Strategy Lab — Engine de backtest, gerenciador de posições, fee model, slippage model, performance analyzer, 4 estratégias de trading e endpoints HTTP de execução |

