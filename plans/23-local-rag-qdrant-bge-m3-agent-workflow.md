# 23 — Local RAG com Qdrant + BGE-M3 para Antigravity e GitHub Copilot

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## 1. Objetivo

Criar um RAG local para otimizar contexto, prompts, planejamento, recuperação de decisões técnicas, paralelização de tarefas e consistência entre agentes de desenvolvimento.

O RAG será usado como **memória técnica do projeto**, não como parte obrigatória do runtime do robô.

Ele deve ajudar a responder:

- qual decisão arquitetural já foi tomada;
- qual etapa do roadmap estamos executando;
- quais arquivos devem ser alterados;
- quais critérios de aceite precisam ser respeitados;
- quais padrões devem ser seguidos;
- quais riscos existem;
- como dividir uma atividade em tarefas paralelas;
- qual contexto enviar para Antigravity ou GitHub Copilot;
- quais documentos antigos podem ser usados como referência somente leitura.

## 2. Hardware disponível

Máquina informada:

| Item | Valor |
|---|---|
| CPU | Intel Xeon E5-2650 v4, 24 threads aparentes |
| Memória | 32 GB RAM |
| GPU | AMD Radeon RX 580 2048SP |
| Disco | 1,5 TB |
| SO | BigCommunity baseado em BigLinux, 64 bits |
| Kernel | Linux 7.1.0-rc4-1-MANJARO |

## 3. Viabilidade

Sim, é possível rodar Qdrant e BGE-M3 nessa máquina.

Recomendação prática:

- rodar Qdrant via Docker ou Podman;
- rodar embeddings em CPU inicialmente;
- usar batch pequeno no BGE-M3;
- não depender da RX 580 no início;
- manter o RAG como ferramenta local de desenvolvimento;
- separar o RAG do runtime principal do robô.

A RX 580 não deve ser tratada como GPU principal para inferência moderna. O caminho mais previsível é CPU + ONNX/PyTorch com lotes pequenos.

## 4. Decisão sobre Python

O projeto principal continua **.NET-first** e **sem Python no runtime**.

Para o RAG local, Python pode ser usado como ferramenta de desenvolvimento isolada porque:

- BGE-M3 tem ecossistema mais maduro em Python;
- o RAG não faz parte do runtime do robô;
- o RAG é ferramenta auxiliar para indexar documentos, código e decisões;
- o core do projeto continua em .NET.

Regra:

```text
Python permitido em tools/rag apenas para tooling local.
Python proibido como dependência do runtime principal do robô, salvo nova ADR.
```

Se quisermos um caminho 100% .NET no futuro, podemos avaliar ONNX Runtime C# com modelo exportado, mas isso fica para uma fase posterior.

## 5. Modelo de embeddings

O nome correto mais provável para o que chamamos de `bge-3` é **BGE-M3**.

BGE-M3 é interessante porque suporta:

- múltiplos idiomas;
- diferentes granularidades de entrada;
- recuperação densa;
- recuperação esparsa;
- multi-vector retrieval;
- textos longos de até 8192 tokens, dependendo do pipeline.

## 6. Arquitetura do RAG

```text
Documentos do projeto
  ↓
Ingestion Pipeline
  ↓
Chunking + Metadata
  ↓
Embedding Service BGE-M3
  ↓
Qdrant Collections
  ↓
Retriever
  ↓
Reranker opcional
  ↓
Context Pack Builder
  ↓
Prompt Optimizer
  ↓
Antigravity / GitHub Copilot / CLI
```

## 7. Coleções Qdrant recomendadas

| Collection | Conteúdo | Uso |
|---|---|---|
| `cryptotrading_docs` | README, plans, ADRs, docs | decisões e planejamento |
| `cryptotrading_code` | código fonte, testes, configs | busca semântica de implementação |
| `cryptotrading_decisions` | ADRs, decisões, trade-offs | memória arquitetural |
| `cryptotrading_prompts` | prompts bons, ruins e versões | otimização de prompts |
| `cryptotrading_tasks` | tarefas, checklists e entregas | paralelização e execução |
| `cryptotrading_external_refs` | resumos de docs externas | referência versionada |

## 8. Metadados obrigatórios por chunk

Cada chunk deve guardar:

```json
{
  "source": "plans/11-stage-07-adaptive-strategy-orchestration.md",
  "repository": "ernanesa/CryptoTrading_05.2026_2",
  "source_type": "plan|adr|code|test|config|external_ref|prompt|task",
  "title": "Stage 07: Adaptive Strategy Orchestration",
  "section": "StrategyScoringService",
  "language": "pt-BR",
  "created_at": "2026-05-20",
  "indexed_at": "2026-05-20T00:00:00-03:00",
  "version": "v1",
  "tags": ["orchestration", "strategy", "risk"],
  "readonly_reference": false
}
```

## 9. Chunking

### Documentos Markdown

- chunk por heading;
- alvo: 800 a 1200 tokens;
- overlap: 100 a 150 tokens;
- preservar título e hierarquia;
- guardar caminho do arquivo e seção.

### Código C#

- chunk por namespace, classe, record, interface, método importante;
- preservar assinatura;
- incluir comentários XML;
- guardar caminho e símbolo.

### Frontend

- chunk por componente, hook, store, service ou arquivo de rota;
- guardar props, imports relevantes e responsabilidade.

### ADRs

- chunk por contexto, decisão, alternativas e consequências;
- indexar com peso alto em `cryptotrading_decisions`.

## 10. Instalação base no BigLinux/Manjaro

### 10.1 Pacotes do sistema

```bash
sudo pacman -Syu
sudo pacman -S git curl jq docker docker-compose python python-pip nodejs npm
```

Ativar Docker:

```bash
sudo systemctl enable --now docker
sudo usermod -aG docker $USER
newgrp docker
```

Alternativa com Podman:

```bash
sudo pacman -S podman podman-compose
```

### 10.2 Rodar Qdrant local

```bash
mkdir -p ~/qdrant/cryptotrading
cd ~/qdrant/cryptotrading

docker run -d \
  --name qdrant-cryptotrading \
  -p 6333:6333 \
  -p 6334:6334 \
  -v "$PWD/qdrant_storage:/qdrant/storage:z" \
  qdrant/qdrant
```

Verificar:

```bash
curl http://localhost:6333/collections
```

Dashboard:

```text
http://localhost:6333/dashboard
```

## 11. Estrutura sugerida no repositório

```text
tools/
  rag/
    README.md
    requirements.txt
    ingest.py
    query.py
    prompt_optimizer.py
    chunking.py
    qdrant_schema.py
    config.example.json
    data/
      external_refs/
      prompt_history/
```

## 12. Dependências sugeridas para tools/rag

Arquivo `tools/rag/requirements.txt`:

```text
qdrant-client
FlagEmbedding
sentence-transformers
torch
transformers
fastapi
uvicorn
pydantic
python-dotenv
rich
tiktoken
markdown-it-py
```

Instalação isolada:

```bash
cd tools/rag
python -m venv .venv
source .venv/bin/activate
pip install -U pip
pip install -r requirements.txt
```

## 13. Serviço local de RAG

Criar uma API local simples:

```text
POST /optimize-input
POST /retrieve-context
POST /build-context-pack
POST /store-decision
POST /store-prompt
GET  /health
```

### Fluxo de input

Todo input deve virar um pacote estruturado:

```json
{
  "raw_input": "implemente a feature X",
  "intent": "implementation",
  "goal": "...",
  "constraints": [".NET-first", "Dapper-first", "RiskEngine obrigatório"],
  "required_context_queries": [
    "roadmap atual M0 M1",
    "decisões Dapper-first",
    "arquitetura AdaptiveStrategyOrchestrator"
  ],
  "parallelization_plan": [
    "branch A: contratos",
    "branch B: testes",
    "branch C: documentação"
  ],
  "acceptance_criteria": ["build passa", "testes passam", "checklist atualizado"]
}
```

## 14. Prompt Optimizer

O `prompt_optimizer.py` deve transformar qualquer pedido em uma especificação para agente.

Template:

```text
Você está trabalhando no repositório ernanesa/CryptoTrading_05.2026_2.

Objetivo:
{goal}

Contexto recuperado do RAG:
{context_pack}

Regras obrigatórias:
- escrever somente neste repositório;
- consultar planos relevantes;
- seguir .NET-first;
- seguir Dapper-first;
- não bypassar RiskEngine;
- atualizar checklists ao final.

Tarefa:
{task}

Entregáveis:
{deliverables}

Critérios de aceite:
{acceptance_criteria}

Paralelização sugerida:
{parallelization_plan}

Antes de codar, gere um plano curto e diga quais arquivos serão alterados.
```

## 15. Uso com Antigravity

### 15.1 Princípio

Antigravity deve receber inputs já otimizados pelo RAG.

Fluxo:

```text
Pedido bruto
  ↓
RAG /optimize-input
  ↓
Prompt estruturado
  ↓
Antigravity Agent
  ↓
Plano + execução
  ↓
Resultado indexado novamente no RAG
```

### 15.2 Como usar manualmente

1. Escrever a solicitação bruta.
2. Rodar o otimizador:

```bash
curl -X POST http://localhost:8087/optimize-input \
  -H 'Content-Type: application/json' \
  -d '{"input":"criar a M0 Foundation do projeto"}'
```

3. Copiar o prompt otimizado para o Antigravity.
4. Pedir para o agente primeiro planejar, depois executar.
5. Ao final, indexar resumo da decisão no RAG.

### 15.3 Configuração MCP para Antigravity

Se a instalação do Antigravity suportar `.vscode/mcp.json` ou configuração MCP compatível com VS Code, usar:

```json
{
  "servers": {
    "qdrant-project-memory": {
      "command": "uvx",
      "args": ["mcp-server-qdrant"],
      "env": {
        "QDRANT_URL": "http://localhost:6333",
        "COLLECTION_NAME": "cryptotrading_docs",
        "EMBEDDING_MODEL": "BAAI/bge-m3"
      }
    }
  }
}
```

Se o cliente não aceitar stdio e exigir SSE, iniciar:

```bash
QDRANT_URL="http://localhost:6333" \
COLLECTION_NAME="cryptotrading_docs" \
EMBEDDING_MODEL="BAAI/bge-m3" \
FASTMCP_SERVER_PORT=8000 \
uvx mcp-server-qdrant --transport sse
```

Endpoint:

```text
http://localhost:8000/sse
```

Observação: validar na UI do Antigravity qual formato de MCP está disponível na sua versão instalada.

## 16. Uso com GitHub Copilot

### 16.1 Custom instructions

Criar `.github/copilot-instructions.md` com regras do projeto:

```md
# Copilot Instructions — CryptoTrading 05.2026 v2

- Trabalhar apenas no repositório ernanesa/CryptoTrading_05.2026_2.
- Consultar plans/ antes de implementar.
- Seguir .NET 10, C# 14, Dapper-first e .NET-first.
- Não introduzir Python no runtime.
- Estratégias não devem bypassar RiskEngine.
- Toda decisão relevante deve gerar DecisionAudit.
- Ao final, atualizar checklists e documentação.
```

### 16.2 MCP no VS Code/Copilot

Criar `.vscode/mcp.json`:

```json
{
  "servers": {
    "qdrant-project-memory": {
      "command": "uvx",
      "args": ["mcp-server-qdrant"],
      "env": {
        "QDRANT_URL": "http://localhost:6333",
        "COLLECTION_NAME": "cryptotrading_docs",
        "EMBEDDING_MODEL": "BAAI/bge-m3"
      }
    },
    "filesystem-project": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "${workspaceFolder}"
      ]
    },
    "memory-light": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

### 16.3 Prompt padrão para Copilot Agent

```text
Antes de implementar, consulte o RAG/MCP e os arquivos em plans/.
Transforme este pedido em um plano com:
1. objetivo;
2. arquivos a alterar;
3. riscos;
4. testes;
5. critérios de aceite;
6. checklist de atualização.

Depois implemente apenas a menor entrega de valor possível.
```

## 17. Paralelização de atividades

O RAG deve ajudar a dividir cada input em trilhas independentes.

Exemplo:

```text
Feature: M1 Market Data + Feature Store

Trilha A — Contratos
- criar Candle, FeatureVector, MarketData abstractions

Trilha B — Persistência
- schema SQL, Dapper repositories, migrations

Trilha C — Ingestão
- Binance adapter, worker, retry

Trilha D — Testes
- fixtures, parser tests, repository tests

Trilha E — Documentação
- atualizar plans/checklists/README
```

Regra:

- cada trilha deve ter branch própria;
- evitar duas trilhas alterando o mesmo arquivo central;
- merges pequenos e frequentes;
- RAG registra resumo após cada merge.

## 18. Segurança do RAG e MCP

- nunca indexar secrets;
- nunca indexar `.env` real;
- nunca dar acesso amplo ao filesystem sem necessidade;
- preferir diretórios permitidos explicitamente;
- usar tokens GitHub com escopo mínimo;
- revisar tool calls antes de permitir ações destrutivas;
- não expor Qdrant para rede externa sem autenticação;
- manter Qdrant local em `localhost`.

## 19. Checklist de implantação do RAG

- [x] Docker/Podman instalado.
- [x] Qdrant rodando em `localhost:6333`.
- [x] Dashboard Qdrant acessível.
- [x] `tools/rag` criado.
- [x] Ambiente virtual criado.
- [x] BGE-M3 testado em CPU.
- [x] Coleções criadas.
- [x] `plans/` indexado.
- [x] README indexado.
- [x] ADRs indexados quando existirem.
- [x] Antigravity configurado com prompt otimizado.
- [x] Copilot configurado com custom instructions.
- [x] MCP Qdrant testado.
- [x] MCP filesystem restrito ao workspace.
- [x] Protocolo de atualização definido.

## 20. Referências

- Qdrant Local Quickstart: https://qdrant.tech/documentation/quickstart/
- Qdrant FastEmbed: https://qdrant.tech/documentation/fastembed/
- Qdrant MCP Server: https://github.com/qdrant/mcp-server-qdrant
- BGE-M3 paper: https://arxiv.org/abs/2402.03216
- MCP docs: https://modelcontextprotocol.io/docs/getting-started/intro
- GitHub Copilot MCP docs: https://docs.github.com/en/copilot/how-tos/provide-context/use-mcp-in-your-ide
