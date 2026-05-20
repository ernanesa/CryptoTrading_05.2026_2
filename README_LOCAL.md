# Guia de Execução Local — CryptoTrading 05.2026 v2

Este guia fornece as instruções passo a passo para executar localmente a infraestrutura do **RAG Local** e a **Solution .NET 10** do robô de trading.

---

## 1. Pré-requisitos
Certifique-se de ter instalado em sua máquina:
*   **.NET Core SDK 10.0** ou superior
*   **Python 3.10** ou superior
*   **NPM / Node.js** (opcional, para servidores MCP adicionais)

---

## 2. Inicializando a Memória Técnica (RAG Local)
Como alternativa super leve ao Docker, o Qdrant está configurado para executar via executável nativo.

### Passo 2.1: Rodar o Servidor Qdrant
No diretório raiz do projeto, execute:
```bash
# Iniciar o Qdrant Server em segundo plano (porta padrão 6333)
./tools/rag/bin/qdrant
```

Para verificar se o servidor está ativo, abra no navegador:
*   [http://localhost:6333/dashboard](http://localhost:6333/dashboard) (Dashboard Visual do Qdrant)
*   Ou execute: `curl http://localhost:6333/collections`

### Passo 2.2: Ativar o Ambiente Virtual Python
```bash
# Navegar até a pasta do RAG
cd tools/rag

# Ativar ambiente virtual
source .venv/bin/activate
```

### Passo 2.3: Ingestão de Planos
Para carregar e indexar toda a pasta `plans/` na memória semântica do Qdrant:
```bash
# Executar script de ingestão
python ingest.py
```

### Passo 2.4: Otimização de Prompts (Uso Prático)
Para otimizar um prompt que você irá enviar para o Antigravity ou Copilot:
```bash
# Exemplo de otimização de intenção
python prompt_optimizer.py "implementar a fundação da feature store no Postgres com Dapper"
```

---

## 3. Compilação e Execução do Backend (.NET 10)

A partir da raiz do repositório:

### 3.1 Restaurar dependências
```bash
dotnet restore
```

### 3.2 Compilar a solução em modo estrito
```bash
dotnet build
```

### 3.3 Rodar os Testes Unitários
```bash
dotnet test
```

### 3.4 Executar o Worker Principal
```bash
dotnet run --project src/Worker/CryptoTrading.Worker.csproj
```

### 3.5 Executar a API Web (SignalR BFF)
```bash
dotnet run --project src/Api/CryptoTrading.Api.csproj
```
A API ficará disponível em `http://localhost:5000` ou similar (verificar a porta exibida no terminal).
