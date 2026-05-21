# ADR-010 — Observabilidade Integrada e Auditoria de Decisões (DecisionAudit)

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

Sistemas de trading autônomos operam de forma silenciosa e contínua. Sem uma observabilidade robusta e detalhada, é quase impossível depurar por que uma ordem específica foi rejeitada, qual modelo de inteligência artificial influenciou o dimensionamento de uma posição, ou qual foi a latência física real entre o recebimento de um sinal de preço e a execução física na exchange. A transparência operacional é um requisito indispensável de segurança e controle de qualidade.

## Decisão

Implementar um plano integrado de observabilidade em tempo real e auditoria persistente histórica em toda a solução.
*   **Decisão Auditável (DecisionAudit)**: Toda e qualquer decisão significativa — geração de sinal por estratégia, reclassificação de ranking, veto pelo motor de risco, envio de ordem ou cálculo de alocação — gera um registro imutável da entidade `DecisionAudit` salvo no banco de dados Postgres contendo os parâmetros de entrada, o resultado da ação, o tempo de execução e a justificativa.
*   **Endpoints de Telemetria e Saúde**: Exposição de endpoints dedicados na API REST:
    *   `/health` para integridade básica e status de serviços internos.
    *   `/api/metrics` para throughput, latências médias e taxas de rejeição.
    *   `/api/hardening/report` para auditoria ativa dos portões de hardening locais.
*   **Visualização Avançada (SignalR Hub)**: Conectar o dashboard React ao BFF (Backend-For-Frontend) por meio do `MetricsHub` (SignalR), transmitindo atualizações em tempo real sem a necessidade de requisições contínuas de polling HTTP.

## Consequências

*   **Rastreabilidade Total**: Depuração rápida e indolor de incidentes operacionais por meio de buscas simples de auditoria no banco de dados.
*   **Dashboard premium interativo**: O usuário tem uma visão rica e dinâmica do comportamento do robô, reforçando a confiabilidade operacional e a facilidade de intervenção manual se necessário.
*   **Manutenção de Desempenho**: Exige cuidados com o volume de dados gerado por auditorias de alto throughput, mitigável por políticas de limpeza ou agregação de tabelas históricas no Postgres.
