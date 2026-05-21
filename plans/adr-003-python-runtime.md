# ADR-003 — Python Fora do MVP / Runtime de Execução

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

Embora a linguagem Python seja o padrão de facto para análise de dados e treinamento de modelos de Machine Learning, sua execução em cenários críticos de trading apresenta desafios significativos relacionados à velocidade de concorrência real (Global Interpreter Lock - GIL), consumo excessivo de memória em processos paralelos e complexidade de distribuição/setup de dependências complexas (ambientes virtuais, versões de bibliotecas e drivers C/C++ vinculados).

## Decisão

Banir formalmente a linguagem **Python** do caminho crítico de runtime e execução contínua do robô.
*   O robô de produção, o orquestrador adaptativo e o motor de risco rodam 100% sob a plataforma .NET.
*   **Exceção de Uso**: Python é permitido apenas em scripts utilitários fora de produção (ex: ingestão inicial ou processamento esporádico do RAG local em ferramentas de suporte, ou notebooks de pesquisa separados).
*   Se modelos de Inteligência Artificial ou heurísticas forem implementados, eles devem rodar via C# nativo (com ONNX Runtime ou ML.NET) para manter a homogeneidade do pipeline.

## Consequências

*   **Simplificação de Infraestrutura**: Evita a necessidade de gerenciar múltiplos ambientes de execução (C# + Python) no servidor de produção, reduzindo os pontos de falha potenciais.
*   **Uniformidade de Desempenho**: Garante que o pipeline completo (do recebimento do candle ao envio da ordem) execute sob o mesmo runtime ultra-otimizado do .NET 10.
*   **Produtividade de Desenvolvimento**: Concentração total das regras na linguagem C#, permitindo que refatorações globais e ferramentas de lint afetem toda a lógica do projeto de forma coesa.
