# ADR-006 — Binance Adapter e Abstrações de Conexão

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

A conexão com exchanges centralizadas de criptoativos (como a Binance) envolve chamadas de rede HTTP REST complexas, gerenciamento de WebSockets em tempo real de baixa latência e regras físicas rígidas impostas pelo livro de ofertas (order book). Acoplar o código de envio de ordens de estratégias de trading diretamente às APIs da Binance criaria um sistema frágil, difícil de testar localmente (backtests e simulações) e altamente dependente de alterações externas na API da exchange.

## Decisão

Abstrair completamente a camada de comunicação de rede por trás de interfaces limpas e validadores locais estruturados.
*   **Interface Unificada**: Todas as interações de dados e ordens ocorrem através da interface `IMarketDataAdapter`.
*   **Implementações Dedicadas**:
    *   `BinanceMarketDataAdapter`: Comunicação física real com a Binance Spot/Testnet usando resiliência nativa (Polly).
    *   `PaperTradeExecutor` / `BacktestEngine`: Mocks de simulação em tempo de simulação local.
*   **Validação Estrita de Regras de Exchange local (ExchangeRuleValidator)**: Antes de enviar ordens à Binance, o sistema valida localmente os limites operacionais do ativo (tamanho do tick/preço, step size/quantidade mínima e notional mínimo) para evitar rejeições custosas por rede.
*   **Proteção de Segredos**: Mascaramento rígido de API Keys e Secrets nos logs e payloads operacionais.

## Consequências

*   **Testabilidade Isolada**: Possibilidade de rodar backtests completos e paper trading idênticos à produção sem disparar uma única chamada de rede real.
*   **Robustez de Rede**: Tratamento de desconexões de rede e limites de taxa de chamadas (rate limits) isolados na camada de infraestrutura.
*   **Migração Simples**: Substituição ou suporte a novas exchanges (ex: Coinbase, Bybit) exige apenas a escrita de uma nova implementação de `IMarketDataAdapter` sem alterar nenhuma estratégia.
