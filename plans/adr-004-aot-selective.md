# ADR-004 — Native AOT Seletivo por Serviço

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

A compilação nativa antecipada (.NET Native AOT - Ahead-Of-Time) remove a necessidade de um compilador JIT em tempo de execução, resultando em binários nativos autônomos com tempo de inicialização instantâneo (sub-milissegundo), consumo de memória extremamente reduzido e melhor desempenho geral em loops intensivos. No entanto, o AOT impõe restrições severas: proíbe geração dinâmica de código em runtime, exige substitutos para reflection dinâmica pesada e limita a compatibilidade com diversas bibliotecas populares que não foram otimizadas para AOT.

## Decisão

Adotar o **Native AOT de forma seletiva**, em vez de impô-lo globalmente a todos os serviços da Solution.
*   **Caminho Padrão**: Os projetos usam compilação JIT tradicional otimizada em Release.
*   **Opt-in Seletivo**: Os serviços podem ativar `PublishAot=true` em seus arquivos `.csproj` individualmente, desde que passem por validações de smoke test locais e benchmarks específicos da plataforma.
*   **Mapeamento de Restrições**: Toda biblioteca de terceiros integrada (como conector da Binance ou serializadores JSON) deve ser avaliada quanto à sua compatibilidade AOT antes de ser introduzida em projetos visados para AOT nativo.

## Consequências

*   **Segurança Arquitetural**: Evita falhas silenciosas de reflection em tempo de execução de produção, permitindo o uso de bibliotecas de mercado maduras que não oferecem suporte AOT integral.
*   **Portabilidade Seletiva**: Permite criar microsserviços ultra-leves e eficientes (ex: APIs REST de consulta rápida de métricas ou orquestradores de decisão puros) compilados nativamente se a demanda computacional justificar.
*   **Garantia de Qualidade**: Exige validação explícita de compilação e execução de testes em ambiente AOT no ciclo de Hardening (M8) para os componentes selecionados.
