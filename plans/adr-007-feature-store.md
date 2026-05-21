# ADR-007 — Feature Store Centralizada e Versionada

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

As estratégias de trading e algoritmos de aprendizado de máquina dependem do cálculo consistente de indicadores técnicos e features derivadas (como médias móveis, RSI, volatilidade e retornos). Um dos maiores problemas em sistemas de trading é o "vazamento de dados" (data leakage) ou o "desvio de features" (feature drift), onde os cálculos realizados offline em backtesting diferem sutilmente dos cálculos efetuados em tempo real na produção devido a janelas de dados incorretas, bugs de arredondamento ou timeframes dessincronizados.

## Decisão

Centralizar e versionar todos os cálculos de indicadores técnicos na camada de persistência e expô-los por meio de um serviço unificado de armazenamento de features (`FeatureStore`).
*   **Cálculo Unificado**: As features são extraídas utilizando o serviço `IndicatorService` (que encapsula a biblioteca altamente otimizada e madura `Skender.Stock.Indicators`) de forma idêntica tanto para gravação de histórico de backtest quanto para streaming em tempo real.
*   **Persistência Versionada**: O pipeline calcula e armazena os candles junto a seu vetor correspondente de features (`CandleFeature`) no Postgres, associados a uma versão explícita do pipeline de cálculo.
*   **DataQualityGate obrigatório**: Antes de qualquer tomada de decisão, o orquestrador verifica a consistência dos dados (se há candles faltantes, nulos ou valores discrepantes/outliers) bloqueando a operação em caso de incoerência.

## Consequências

*   **Garantia de Reprodutibilidade**: Um backtest executado nos dados históricos salvos produzirá exatamente as mesmas decisões de trading que o robô tomaria rodando ao vivo com as mesmas condições de mercado.
*   **Qualidade e Segurança de Sinais**: Proteção ativa contra falhas de conexão de dados de terceiros ou corrupção de séries temporais por meio do portão de qualidade (`DataQualityGate`).
*   **Facilidade de Análise**: Pesquisadores e analistas de dados podem acessar diretamente a tabela `candle_features` no PostgreSQL para auditar ou treinar novos modelos sabendo que os dados são idênticos ao runtime de trading.
