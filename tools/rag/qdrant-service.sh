#!/usr/bin/env bash

# Script de gerenciamento do Qdrant Server Nativo (Rust)
# Localizado em: tools/rag/qdrant-service.sh

# Resolve o caminho absoluto do diretório do script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

BIN_PATH="$SCRIPT_DIR/bin/qdrant"
STORAGE_PATH="$SCRIPT_DIR/qdrant_storage"
LOG_FILE="$SCRIPT_DIR/qdrant.log"
PID_FILE="$SCRIPT_DIR/qdrant.pid"

mkdir -p "$STORAGE_PATH"

get_pid() {
    if [ -f "$PID_FILE" ]; then
        pid=$(cat "$PID_FILE")
        if ps -p "$pid" > /dev/null 2>&1; then
            echo "$pid"
            return 0
        fi
    fi
    # Fallback para busca de processo
    pid=$(pgrep -f "bin/qdrant.*storage-path")
    if [ -n "$pid" ]; then
        echo "$pid"
        return 0
    fi
    return 1
}

start_qdrant() {
    pid=$(get_pid)
    if [ -n "$pid" ]; then
        echo "Qdrant já está rodando (PID: $pid)."
        exit 0
    fi

    if [ ! -x "$BIN_PATH" ]; then
        echo "Erro: Executável do Qdrant não encontrado ou sem permissão em: $BIN_PATH"
        exit 1
    fi

    echo "Iniciando Qdrant Server em background..."
    QDRANT__STORAGE__STORAGE_PATH="$STORAGE_PATH" nohup "$BIN_PATH" > "$LOG_FILE" 2>&1 &
    new_pid=$!
    
    echo "$new_pid" > "$PID_FILE"
    sleep 2

    # Verifica se realmente subiu
    if ps -p "$new_pid" > /dev/null 2>&1; then
        echo "Qdrant Server iniciado com sucesso! (PID: $new_pid)"
        echo "Acesse o Dashboard em: http://localhost:6333/dashboard"
    else
        echo "Erro ao iniciar o Qdrant. Verifique os logs em: $LOG_FILE"
        exit 1
    fi
}

stop_qdrant() {
    pid=$(get_pid)
    if [ -z "$pid" ]; then
        echo "Qdrant não está rodando."
        exit 0
    fi

    echo "Parando Qdrant Server (PID: $pid)..."
    kill "$pid"
    
    # Espera parar
    for i in {1..5}; do
        if ! ps -p "$pid" > /dev/null 2>&1; then
            break
        fi
        sleep 1
    done

    if ps -p "$pid" > /dev/null 2>&1; then
        echo "Forçando encerramento..."
        kill -9 "$pid"
    fi

    rm -f "$PID_FILE"
    echo "Qdrant Server parado com sucesso."
}

status_qdrant() {
    pid=$(get_pid)
    if [ -n "$pid" ]; then
        echo "● Qdrant Server está ATIVO (PID: $pid)"
        echo "  Endpoint: http://localhost:6333"
        echo "  Armazenamento: $STORAGE_PATH"
        
        # Testa conexão via curl
        if command -v curl > /dev/null 2>&1; then
            if curl -s --connect-timeout 2 http://localhost:6333/collections > /dev/null; then
                echo "  API: respondendo corretamente (HTTP 200 OK)"
            else
                echo "  API: sem resposta ou erro de conexão"
            fi
        fi
    else
        echo "○ Qdrant Server está INATIVO"
    fi
}

logs_qdrant() {
    if [ -f "$LOG_FILE" ]; then
        tail -n 50 -f "$LOG_FILE"
    else
        echo "Nenhum arquivo de log encontrado em: $LOG_FILE"
    fi
}

case "$1" in
    start)
        start_qdrant
        ;;
    stop)
        stop_qdrant
        ;;
    status)
        status_qdrant
        ;;
    logs)
        logs_qdrant
        ;;
    *)
        echo "Uso: $0 {start|stop|status|logs}"
        exit 1
        ;;
esac
