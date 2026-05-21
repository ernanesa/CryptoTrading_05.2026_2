# ADR-008 — Centralização Obrigatória e Inviolável de Gestão de Risco (RiskEngine)

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

Algoritmos de trading autônomos, orquestradores dinâmicos e modelos de aprendizado de máquina podem falhar sob condições anômalas de mercado, bugs de lógica interna ou instabilidade de rede. Se uma estratégia com falha puder disparar ordens sem limites de controle, as consequências financeiras podem ser catastróficas para o portfólio. Era imperativo criar uma salvaguarda inquebrável, independente das decisões de inteligência.

## Decisão

Adotar o padrão de **barreira física de risco**. Nenhuma ordem de trading pode ser enviada à exchange real (ou simulada) sem passar obrigatoriamente por um motor de risco centralizado e síncrono (`RiskEngine`).
*   **Controles Rígidos**: O `RiskEngine` aplica regras invioláveis configuradas:
    *   Exposição máxima por ativo e por carteira global.
    *   Número máximo de ordens abertas simultaneamente.
    *   Tamanho máximo permitido por operação individual (position sizing).
    *   Filtro ativo de volatilidade extrema (ATR/spread de segurança).
    *   Bloqueio global automático (circuit breaker) em caso de drawdown intraday excessivo.
*   **Bypass Proibido**: Qualquer tentativa de chamada de execução de ordens direta que ignore a verificação do `RiskEngine` é arquiteturalmente impossível, pois as abstrações de execução (`BinanceTestnetExecutor`, `PaperTradeExecutor`) exigem o token de validação gerado pelo motor de risco.
*   **Auditoria Total de Decisões**: Cada decisão tomada ou rejeitada pelo motor de risco gera um registro detalhado de auditoria (`DecisionAudit`) persistido no PostgreSQL.

## Consequências

*   **Segurança Operacional Absoluta**: Garantia de que bugs em modelos de IA ou estratégias de trading nunca quebrarão os limites de risco definidos para a conta de trading.
*   **Auditoria e Transparência**: Facilidade para rastrear exatamente por que uma estratégia recomendada não foi executada (ex: rejeitada por excesso de correlação, tamanho mínimo ou circuit breaker ativo).
*   **Rigidez no Código**: Exige que qualquer teste ou nova estratégia implemente cenários de validação de risco explícitos, garantindo que a cultura de segurança e gerenciamento de capital permaneça intacta durante o crescimento da Solution.
