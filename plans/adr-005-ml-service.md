# ADR-005 — Inteligência e ML.NET Integrados por Snapshots Assíncronos

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

A camada de inteligência (classificação de regimes de mercado, detecção de anomalias de volatilidade, processamento de sentimento e modelos ONNX/ML.NET) envolve processamento matemático contínuo que consome recursos de CPU/GPU e pode introduzir latências variáveis. Acoplar essa execução diretamente na thread síncrona de ingestão de dados e execução de ordens viola as restrições de latência estrita e pode interromper o fluxo operacional do robô em caso de lentidão ou falha no carregamento dos modelos.

## Decisão

Isolar o processamento de Machine Learning e Inteligência da linha síncrona de trading.
*   **Comunicação Assíncrona via Snapshots**: Os serviços da camada de inteligência (AnomalyDetectionService, RegimeDetectionService, SentimentRiskService, etc.) trabalham de forma assíncrona, publicando suas previsões e insights dentro de uma entidade unificada chamada `IntelligenceSnapshot` versionada.
*   **Consumo pelo Orquestrador**: O `AdaptiveStrategyOrchestrator` consome o snapshot de inteligência ativo mais recente em cache de forma não bloqueante (O(1)). Se a camada de ML falhar ou estiver desativada, o sistema usa valores padrão seguros sem interromper o loop de trading.
*   **Uso de ONNX e ML.NET**: Os modelos matemáticos rodam nativamente em C# usando a especificação ONNX ou pacotes maduros do ML.NET, eliminando a dependência de interpretadores Python.

## Consequências

*   **Tolerância a Falhas**: Falhas críticas nos modelos de aprendizado de máquina não causam o congelamento do loop de trading principal.
*   **Desempenho Estável**: A thread de tomada de decisão operacional permanece rápida e determinística (sub-milissegundo), consumindo previsões pré-calculadas.
*   **Manutenibilidade de Modelos**: Permite atualizar ou treinar novamente os modelos em background sem a necessidade de reiniciar ou interromper o serviço Worker de trading.
