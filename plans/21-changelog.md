# 21 — Planning Changelog

| Data | Alteração |
|---|---|
| 2026-05-21 | Criação do gate opt-in de Native AOT para API e Worker com script local e acionamento manual no workflow de hardening |
| 2026-05-21 | Revalidação pós-M8 dos gates locais com RAG consultado, testes unitários, build do dashboard e smoke benchmark adaptativo registrados |
| 2026-05-21 | Ingestão e persistência bem-sucedidas de dados históricos e em tempo real (candles e features técnicos) no PostgreSQL local para BTCUSDT e ETHUSDT |
| 2026-05-21 | Implementação do endpoint de seed `/api/paper/seed` para popular carteiras virtuais, trades reais e auditorias de risco (APPROVED/REJECTED) para demonstração no painel |
| 2026-05-21 | Revisão e atualização do `README_LOCAL.md` com instruções corretas sobre o ecossistema C# RagTool e Docker-compose do PostgreSQL/Qdrant |
| 2026-05-21 | Verificação, validação e preenchimento dos checklists e critérios de aceitação do Dashboard e Observabilidade (Stage 05) |
| 2026-05-21 | Verificação, validação e preenchimento dos checklists de infraestrutura RAG local, servidores MCP/VSCode e critérios de aceitação do motor de risco (RiskEngine) |
| 2026-05-21 | Criação e consolidação física dos 11 ADRs recomendados (ADR-001 ao ADR-011) documentando em detalhes todas as decisões de arquitetura e tecnologia adotadas no projeto |
| 2026-05-20 | Criação inicial do planejamento completo no novo repositório `ernanesa/CryptoTrading_05.2026_2` |
| 2026-05-20 | Definido novo repositório como único alvo de escrita |
| 2026-05-20 | Definida arquitetura .NET-first, Dapper-first, Python fora do MVP e AOT seletivo |
| 2026-05-20 | Incluída etapa oficial de Adaptive Strategy Orchestration |
| 2026-05-20 | Substituição completa do RAG Python por um sistema 100% C# .NET 10 (`CryptoTrading.RagTool`) integrado ao Qdrant nativo executado no host |
| 2026-05-20 | Implementação completa da M1: Market Data & Feature Store — Ingestão de candles da Binance, DataQualityGate, cálculo de indicadores técnicos (EMA, RSI, MACD, ATR, BB, ADX) e persistência em PostgreSQL via Dapper |
| 2026-05-20 | Infraestrutura Docker Compose configurada: PostgreSQL 16 Alpine + Qdrant v1.18 em containers gerenciados |
| 2026-05-20 | Adicionadas e validadas features adicionais de M1: Returns, Volume Z-Score, Spread e Imbalance (Taker Buy) no pipeline e banco de dados |
| 2026-05-20 | Implementação completa da M2: Backtesting + Strategy Lab — Engine de backtest, gerenciador de posições, fee model, slippage model, performance analyzer, 4 estratégias de trading e endpoints HTTP de execução |
| 2026-05-20 | Implementação completa da M3: Paper Trading + Risk — Carteira virtual persistente, simulador de execuções em tempo real (PaperTradingExecutor), motor de regras de risco (RiskEngine), tabelas PostgreSQL dedicadas e auditoria de decisões completas |
| 2026-05-20 | Implementação completa da M4: Binance Spot Testnet — Integração sandbox/real, ExchangeRuleValidator local (Tick/Step size, Min Qty/Notional), BinanceTestnetExecutor com suporte dry-run, OrderStatusSynchronizer de ordens abertas, mascaramento estrito de segredos e testes unitários robustos |
| 2026-05-20 | Iniciada M6: Intelligence Layer — IntelligenceSnapshot versionado, RegimeDetectionService, AnomalyDetectionService, endpoint de snapshot e visualização no dashboard |
| 2026-05-20 | Evolução da M6: FeatureExtractor versionado e VolatilityForecastService heurístico adicionados ao IntelligenceSnapshot e ao dashboard |
| 2026-05-20 | Implementação completa da M6: Intelligence Layer — MetaLabeling, SentimentRisk, EventRisk, ModelRegistry, RagContext e Explanation integrados ao snapshot e dashboard |
| 2026-05-20 | Implementação completa da M7: Adaptive Strategy Orchestration — Control Plane adaptativo com ranking de ativo, scoring de estratégia, histerese/cooldown, alocação, sizing, exit policy, health monitor, bandit e dashboard |
| 2026-05-20 | Implementação completa da M8: Hardening — gates de qualidade, endpoint `/api/hardening/report`, redator de secrets, chaos scenarios, benchmarks registrados, riscos conhecidos e dashboard |
| 2026-05-20 | Adicionado harness local de benchmarks em `tools/benchmarks/CryptoTrading.Benchmarks` para IndicatorService e AdaptiveStrategyOrchestrator, com cenários opt-in de FeatureStore e AOT |
| 2026-05-20 | Adicionado workflow `hardening-gates.yml` para validar build, testes, dashboard e smoke benchmarks no CI |
