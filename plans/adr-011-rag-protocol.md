# ADR-011 — RAG Local Offline como Memória Técnica do Projeto

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

O desenvolvimento acelerado e iterativo de uma solução complexa por múltiplos agentes de Inteligência Artificial (como o Antigravity e o GitHub Copilot) e desenvolvedores humanos pode facilmente levar a um "drift arquitetural" ou "alucinações técnicas". Padrões estruturais estabelecidos nas primeiras semanas de planejamento podem ser esquecidos em etapas posteriores, gerando inconsistências no código, duplicação de funções ou quebra de restrições críticas (como o uso indevido de Python no runtime).

## Decisão

Adotar e estruturar um **RAG local offline** (Retrieval-Augmented Generation) integrado ao banco vetorial **Qdrant** rodando em container Docker no próprio ambiente do usuário.
*   **Armazenamento Vetorial**: Coleções dedicadas no Qdrant para planos (`cryptotrading_docs`), código-fonte (`cryptotrading_code`) e decisões históricas.
*   **Processador Nativo em C# (CryptoTrading.RagTool)**: Desenvolvemos uma ferramenta interna 100% C# .NET 10 em `tools/CryptoTrading.RagTool/` que lê o repositório, gera embeddings matemáticos localmente via CPU (utilizando um modelo leve e otimizado MiniLM ONNX) e envia em lote ao Qdrant.
*   **Protocolo de Consulta Rigoroso**: Obrigatoriedade de consulta ao RAG antes de planejar grandes alterações ou em caso de dúvida técnica, registrando a consulta de forma legível no cabeçalho do checklist do card de desenvolvimento correspondente.

## Consequências

*   **100% Offline e Privado**: Nenhuma informação corporativa ou código confidencial do usuário é vazado para serviços de nuvem de terceiros durante a recuperação de contexto.
*   **Consistência Absoluta**: Os agentes mantêm o alinhamento arquitetural estrito e respeitam as decisões tecnológicas originais de forma consistente entre diferentes conversas.
*   **Facilidade de Integração**: Conexão simples com IDEs e editores de código por meio do Model Context Protocol (MCP) restrito ao escopo do workspace.
