# ADR-009 — Orquestração Adaptativa de Portfólio e Estratégias

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

Estratégias de trading estáticas individuais (ex: rastreamento de tendência com médias móveis ou reversão à média com RSI) têm desempenhos cíclicos: lucram significativamente em regimes de forte tendência, mas sofrem perdas constantes (drawdowns) em regimes de mercado lateralizado, ou vice-versa. Escolher uma única estratégia ou manter pesos fixos de alocação de ativos independentemente das condições atuais de volatilidade expõe o capital a riscos de cauda e drawdowns desnecessários.

## Decisão

Implementar o componente de **Orquestração Adaptativa** (`AdaptiveStrategyOrchestrator`) como o cérebro central da tomada de decisões operacionais.
*   **Decisão Multidimensional**: Em vez de executar uma única estratégia estática, o orquestrador analisa dinamicamente:
    *   **Ativos Recomendados**: Classificados pelo `AssetRankingService`.
    *   **Pontuação de Estratégia**: O `StrategyScoringService` avalia a performance recente de cada estratégia em tempo real.
    *   **Alocação e Dimensionamento Dinâmico**: Calculados pelo `AdaptivePortfolioAllocator` e `DynamicPositionSizingService` integrando volatilidade (ATR) e regimes identificados.
    *   **Multi-Armed Bandit (MAB)**: Algoritmo dinâmico de exploração e aproveitamento (exploration/exploitation) que aloca capital de forma adaptativa para as estratégias com maior probabilidade de retorno ajustado ao risco recente.
*   **Mecanismo de Histerese e Cooldown**: Evita "ruído de alternância rápida" de estratégias em timeframes curtos por meio de limites mínimos de score e tempos de descanso obrigatórios.
*   **Comparativo Fixo vs Adaptativo**: O sistema rastreia e compara o desempenho de portfólios estáticos paralelos contra a orquestração dinâmica no dashboard para validação estatística da entrega de valor.

## Consequências

*   **Maior Resiliência**: O robô se adapta automaticamente às transições de ciclos de mercado (ex: reduzindo a exposição a reversão à média quando a tendência acelera ou saindo de ativos em regimes altamente caóticos).
*   **Eficiência de Capital**: Foco do capital nos ativos e estratégias que estão demonstrando maior vantagem competitiva real (edge) no regime recente.
*   **Complexidade Monitorada**: Introduz a necessidade de rastreamento visual avançado (exposto no dashboard operacional em tempo real) para que o operador compreenda exatamente por que os pesos de alocação e as estratégias foram alterados.
