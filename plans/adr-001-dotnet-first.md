# ADR-001 — Arquitetura Geral .NET-First

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

O desenvolvimento de um robô de trading de alta frequência (HFT) e orquestração de portfólio exige uma plataforma extremamente rápida, tipada, resiliente e segura para concorrência de CPU e rede. Queríamos evitar os problemas comuns de linguagens dinâmicas no runtime crítico de execução (lentidão do garbage collector, GIL, tipagem dinâmica fraca) e, ao mesmo tempo, queríamos uma estrutura organizacional limpa que seguisse os princípios da Clean Architecture.

## Decisão

Adotar o ecossistema **.NET 10 + C# 14** como tecnologia central do backend. A solução é estruturada usando o padrão de arquitetura cebola (Clean Architecture) dividida em camadas lógicas puras:

1.  **Domain**: Regras de negócio puras, entidades (Candle, Position, DecisionAudit), enums e exceções. Totalmente isolada e sem dependências externas de infraestrutura.
2.  **Contracts**: Abstrações, contratos de API e interfaces (IRiskEngine, IFeatureStore, IMarketDataAdapter) para desacoplamento total.
3.  **Application**: Casos de uso do sistema, implementação de estratégias (AtrBreakout, BollingerMeanReversion, etc.), backtesting engine e o orquestrador adaptativo.
4.  **Infrastructure**: Implementações físicas de IO, persistência Postgres via Dapper e adaptadores de rede (como o conector da Binance).
5.  **Worker**: Serviço contínuo (BackgroundService) encarregado da ingestão de candles e execução contínua.
6.  **Api**: Camada ASP.NET Core que expõe endpoints REST (/api/metrics, /api/hardening/report) e canais em tempo real via SignalR (MetricsHub).

## Consequências

*   **Segurança de Tipos**: Redução massiva de bugs em runtime decorrentes de tipos incorretos.
*   **Concorrência de Alta Performance**: Uso pleno das capacidades assíncronas assíncronas do C# (async/await) com concorrência segura por multithreading e canais.
*   **Facilidade de Manutenção**: Limites arquiteturais claros onde a infraestrutura pode ser completamente alterada ou mockada sem afetar a lógica central das estratégias ou do motor de risco.
*   **Portabilidade**: Execução nativa e leve em sistemas operacionais baseados em Linux (utilizados na infraestrutura do usuário).
