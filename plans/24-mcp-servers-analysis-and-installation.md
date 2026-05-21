# 24 — MCP Servers: análise, utilidade e instalação

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## 1. Objetivo

Definir quais MCPs serão úteis para o desenvolvimento do projeto `CryptoTrading_05.2026_2`, como instalá-los e como usá-los com Antigravity e GitHub Copilot.

MCP significa **Model Context Protocol**. Ele padroniza a conexão entre agentes/modelos e ferramentas externas, como filesystem, GitHub, bancos vetoriais, bancos relacionais, navegadores e memórias locais.

## 2. Regra do projeto

MCPs devem ser usados para melhorar:

- recuperação de contexto;
- análise de código;
- consulta ao RAG;
- organização de tarefas;
- paralelização de atividades;
- criação de planos;
- atualização de documentação;
- revisão de mudanças.

MCPs não devem ser usados para burlar revisão humana, expor secrets, executar comandos destrutivos sem confirmação ou misturar escrita em repositórios antigos.

Repositório de escrita permitido:

```text
ernanesa/CryptoTrading_05.2026_2
```

Repositórios antigos:

```text
read-only/reference only
```

## 3. MCPs recomendados por prioridade

| Prioridade | MCP | Utilidade | Instalar agora? |
|---:|---|---|---|
| P0 | Qdrant MCP | Memória semântica/RAG do projeto | Sim |
| P0 | Filesystem MCP | Ler arquivos locais do workspace | Sim, com escopo restrito |
| P0 | GitHub MCP | Issues, PRs, commits e contexto GitHub | Sim, com token limitado |
| P1 | Memory MCP | Memória simples de curto/médio prazo | Sim |
| P1 | Sequential Thinking MCP | Quebra de problemas e planejamento | Sim, se cliente suportar |
| P1 | Fetch/Web MCP | Consultar documentação externa | Sim, com cautela |
| P1 | PostgreSQL MCP | Consultar banco local de desenvolvimento | Depois da M1/M2 |
| P2 | Playwright MCP | Testes e inspeção do dashboard | Depois do dashboard |
| P2 | Docker MCP | Inspecionar containers locais | Opcional |
| P2 | Time MCP | Padronizar data/hora em prompts | Opcional |

## 4. MCP Qdrant — P0

### Por que usar

É o MCP mais importante para o nosso caso. Ele conecta o agente ao Qdrant e permite consultar a memória semântica do projeto.

Uso esperado:

- buscar decisões em `plans/`;
- recuperar ADRs;
- montar contexto para prompts;
- comparar pedido atual com decisões antigas;
- encontrar arquivos relacionados;
- alimentar Antigravity/Copilot com contexto compacto.

### Instalação

Pré-requisitos:

```bash
sudo pacman -S python uv
```

Se `uv` não estiver disponível via pacman:

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

Rodar Qdrant:

```bash
docker run -d \
  --name qdrant-cryptotrading \
  -p 6333:6333 \
  -p 6334:6334 \
  -v "$PWD/qdrant_storage:/qdrant/storage:z" \
  qdrant/qdrant
```

Configuração MCP:

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

## 5. Filesystem MCP — P0

### Por que usar

Permite que o agente leia arquivos locais do workspace e navegue pela estrutura do projeto.

Uso esperado:

- abrir `plans/`;
- localizar arquivos de código;
- comparar contratos e implementação;
- atualizar documentação com contexto local.

### Instalação

```bash
npm install -g @modelcontextprotocol/server-filesystem
```

Configuração:

```json
{
  "servers": {
    "filesystem-project": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "${workspaceFolder}"
      ]
    }
  }
}
```

### Segurança

Nunca dar acesso ao filesystem inteiro. Usar apenas o workspace do projeto.

## 6. GitHub MCP — P0

### Por que usar

Útil para trabalhar com issues, PRs, branches, commits e histórico GitHub.

Uso esperado:

- criar issues por fase;
- ler PRs;
- revisar mudanças;
- cruzar plano com commits;
- manter rastreabilidade.

### Instalação

Preferir o servidor oficial GitHub MCP quando disponível no ambiente.

Exemplo genérico com Docker:

```json
{
  "servers": {
    "github": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e",
        "GITHUB_PERSONAL_ACCESS_TOKEN",
        "ghcr.io/github/github-mcp-server"
      ],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "${input:github_token}"
      }
    }
  }
}
```

### Token recomendado

Usar token com menor escopo possível:

- leitura de repositório;
- escrita apenas no `CryptoTrading_05.2026_2`, se necessário;
- sem permissões administrativas desnecessárias.

## 7. Memory MCP — P1

### Por que usar

Memória leve e rápida para fatos de trabalho durante uma sessão ou ciclo de desenvolvimento.

Uso esperado:

- lembrar branch atual;
- lembrar fase em execução;
- lembrar decisões curtas temporárias;
- complementar, não substituir, Qdrant.

### Instalação

```bash
npm install -g @modelcontextprotocol/server-memory
```

Configuração:

```json
{
  "servers": {
    "memory-light": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

## 8. Sequential Thinking MCP — P1

### Por que usar

Ajuda a quebrar atividades grandes em etapas menores, com dependências e riscos.

Uso esperado:

- planejar M0 Foundation;
- dividir M1 Market Data em trilhas paralelas;
- revisar arquitetura;
- gerar checklist antes de código;
- evitar que o agente pule direto para implementação.

### Instalação

```bash
npm install -g @modelcontextprotocol/server-sequential-thinking
```

Configuração:

```json
{
  "servers": {
    "sequential-thinking": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-sequential-thinking"]
    }
  }
}
```

Se o pacote não estiver disponível no registry do seu ambiente, manter como opcional e usar o próprio fluxo de prompt estruturado do RAG.

## 9. Fetch/Web MCP — P1

### Por que usar

Consultar documentação oficial atual durante planejamento e desenvolvimento.

Uso esperado:

- verificar documentação Binance;
- verificar versões Vite/React/TypeScript;
- verificar docs .NET;
- consultar changelogs oficiais.

### Instalação genérica

```bash
npm install -g @modelcontextprotocol/server-fetch
```

Configuração:

```json
{
  "servers": {
    "fetch": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch"]
    }
  }
}
```

Se o pacote exato variar no cliente, usar a ferramenta de web/fetch nativa do cliente.

## 10. PostgreSQL MCP — P1/P2

### Por que usar

Depois da M1/M2, será útil consultar dados locais de candles, features, backtests e métricas.

Uso esperado:

- auditar dados de candles;
- consultar performance de estratégias;
- verificar registros de DecisionAudit;
- investigar gargalos.

### Instalação genérica

A instalação varia conforme o servidor MCP escolhido. O padrão esperado será:

```json
{
  "servers": {
    "postgres-dev": {
      "command": "npx",
      "args": ["-y", "<postgres-mcp-server-package>"],
      "env": {
        "DATABASE_URL": "postgresql://user:password@localhost:5432/cryptotrading"
      }
    }
  }
}
```

Regra: nunca usar credenciais de produção em MCP local.

## 11. Playwright MCP — P2

### Por que usar

Útil quando o dashboard existir.

Uso esperado:

- abrir dashboard local;
- testar telas;
- capturar erros visuais;
- validar fluxo de logs, métricas e strategy score.

### Instalação genérica

```bash
npm install -g @playwright/mcp
```

Configuração:

```json
{
  "servers": {
    "playwright": {
      "command": "npx",
      "args": ["-y", "@playwright/mcp"]
    }
  }
}
```

## 12. Docker MCP — P2

### Por que usar

Pode ajudar a inspecionar containers locais, como Qdrant, PostgreSQL, Prometheus e Grafana.

Uso esperado:

- verificar se Qdrant está rodando;
- verificar logs de containers;
- diagnosticar ambiente local.

Regra: usar apenas leitura/diagnóstico no início.

## 13. Configuração recomendada inicial

Arquivo `.vscode/mcp.json` recomendado:

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

## 14. Configuração para Antigravity

Como a interface/configuração exata pode mudar entre versões, usar a seguinte regra:

1. Procurar configuração MCP do Antigravity.
2. Se aceitar `mcp.json`, reaproveitar `.vscode/mcp.json`.
3. Se aceitar SSE, iniciar Qdrant MCP com transporte SSE.
4. Se não aceitar MCP diretamente, usar o `tools/rag` como serviço HTTP intermediário.

Modo HTTP intermediário:

```text
Antigravity → http://localhost:8087/optimize-input → Qdrant → prompt otimizado
```

## 15. Configuração para GitHub Copilot

No VS Code, usar:

- `.github/copilot-instructions.md` para instruções persistentes;
- `.vscode/mcp.json` para MCPs;
- branch/issue por atividade;
- prompts otimizados pelo RAG.

Prompt padrão:

```text
Consulte plans/, RAG e MCPs disponíveis antes de implementar.
Gere plano curto, arquivos a alterar, riscos, testes e critérios de aceite.
Implemente a menor entrega de valor possível.
Atualize documentação/checklists ao final.
```

## 16. MCPs que não entram agora

| MCP | Motivo para adiar |
|---|---|
| Kubernetes | Não há cluster nesta fase |
| Cloud provider | Infra cloud não é prioridade imediata |
| Browser automation pesada | Só após dashboard existir |
| Banco externo gerenciado | Usaremos local/dev primeiro |
| Slack/Discord | Comunicação operacional não é foco agora |
| Email | Não é necessário para M0-M2 |

## 17. Checklist MCP inicial

- [x] Qdrant rodando localmente.
- [x] Qdrant MCP instalado.
- [x] Filesystem MCP restrito ao workspace.
- [x] Memory MCP instalado.
- [x] GitHub MCP avaliado com token mínimo.
- [x] `.github/copilot-instructions.md` criado.
- [x] `.vscode/mcp.json` criado.
- [x] Antigravity testado com prompt otimizado.
- [x] Copilot testado com MCP/RAG.
- [x] Nenhum secret real em arquivos versionados.

## 18. Referências

- MCP docs: https://modelcontextprotocol.io/docs/getting-started/intro
- GitHub Copilot MCP docs: https://docs.github.com/en/copilot/how-tos/provide-context/use-mcp-in-your-ide
- Qdrant MCP Server: https://github.com/qdrant/mcp-server-qdrant
- Qdrant Quickstart: https://qdrant.tech/documentation/quickstart/
