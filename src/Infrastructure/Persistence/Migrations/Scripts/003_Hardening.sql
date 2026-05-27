CREATE TABLE IF NOT EXISTS paper_orders (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    client_order_id VARCHAR(50) UNIQUE NOT NULL,
    side VARCHAR(10) NOT NULL,
    type VARCHAR(10) NOT NULL,
    price NUMERIC(28, 8) NOT NULL,
    quantity NUMERIC(28, 8) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS risk_decisions (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    is_approved BOOLEAN NOT NULL,
    reason VARCHAR(255) NOT NULL,
    risk_score NUMERIC(10, 4) NOT NULL
);

CREATE TABLE IF NOT EXISTS strategy_metrics (
    id BIGSERIAL PRIMARY KEY,
    strategy_name VARCHAR(50) NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    win_rate NUMERIC(10, 4) NOT NULL,
    profit_factor NUMERIC(10, 4) NOT NULL,
    sharpe_ratio NUMERIC(10, 4) NOT NULL,
    max_drawdown NUMERIC(10, 4) NOT NULL
);

-- Revisar índices
CREATE INDEX IF NOT EXISTS idx_paper_orders_symbol ON paper_orders (symbol, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_risk_decisions_symbol ON risk_decisions (symbol, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_strategy_metrics_strategy ON strategy_metrics (strategy_name, symbol, timestamp DESC);

-- Otimizações para consultas de timeseries
CREATE INDEX IF NOT EXISTS idx_candles_time_range ON candles (symbol, interval, open_time, close_time);
CREATE INDEX IF NOT EXISTS idx_candle_features_time_range ON candle_features (symbol, open_time);
