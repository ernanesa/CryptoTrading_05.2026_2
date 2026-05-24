CREATE TABLE IF NOT EXISTS candles (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    interval VARCHAR(10) NOT NULL,
    open_time TIMESTAMP WITH TIME ZONE NOT NULL,
    open NUMERIC(28, 8) NOT NULL,
    high NUMERIC(28, 8) NOT NULL,
    low NUMERIC(28, 8) NOT NULL,
    close NUMERIC(28, 8) NOT NULL,
    volume NUMERIC(28, 8) NOT NULL,
    taker_buy_volume NUMERIC(28, 8) NOT NULL DEFAULT 0,
    close_time TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uq_candles UNIQUE (symbol, interval, open_time)
);

CREATE TABLE IF NOT EXISTS candle_features (
    candle_id BIGINT PRIMARY KEY REFERENCES candles(id) ON DELETE CASCADE,
    symbol VARCHAR(20) NOT NULL,
    open_time TIMESTAMP WITH TIME ZONE NOT NULL,
    ema_9 NUMERIC(28, 8) NOT NULL,
    ema_21 NUMERIC(28, 8) NOT NULL,
    ema_50 NUMERIC(28, 8) NOT NULL,
    ema_200 NUMERIC(28, 8) NOT NULL,
    rsi_14 NUMERIC(28, 8) NOT NULL,
    macd_value NUMERIC(28, 8) NOT NULL,
    macd_signal NUMERIC(28, 8) NOT NULL,
    macd_histogram NUMERIC(28, 8) NOT NULL,
    atr_14 NUMERIC(28, 8) NOT NULL,
    bb_upper NUMERIC(28, 8) NOT NULL,
    bb_middle NUMERIC(28, 8) NOT NULL,
    bb_lower NUMERIC(28, 8) NOT NULL,
    adx NUMERIC(28, 8) NOT NULL,
    returns NUMERIC(28, 8) NOT NULL DEFAULT 0,
    volume_z_score NUMERIC(28, 8) NOT NULL DEFAULT 0,
    spread NUMERIC(28, 8) NOT NULL DEFAULT 0,
    imbalance NUMERIC(28, 8) NOT NULL DEFAULT 0,
    calculated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_candles_lookup ON candles (symbol, interval, open_time DESC);
CREATE INDEX IF NOT EXISTS idx_features_lookup ON candle_features (symbol, open_time DESC);

CREATE TABLE IF NOT EXISTS paper_wallet (
    symbol VARCHAR(20) PRIMARY KEY,
    free NUMERIC(28, 8) NOT NULL,
    locked NUMERIC(28, 8) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS paper_trades (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    type VARCHAR(10) NOT NULL,
    price NUMERIC(28, 8) NOT NULL,
    quantity NUMERIC(28, 8) NOT NULL,
    fee NUMERIC(28, 8) NOT NULL,
    pnl NUMERIC(28, 8) NOT NULL,
    executed_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS decision_audits (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    strategy_name VARCHAR(50) NOT NULL,
    signal_type VARCHAR(10) NOT NULL,
    price NUMERIC(28, 8) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    decision VARCHAR(20) NOT NULL,
    reason VARCHAR(255) NOT NULL
);

INSERT INTO paper_wallet (symbol, free, locked, updated_at)
VALUES ('USDT', 10000.0, 0.0, NOW())
ON CONFLICT (symbol) DO NOTHING;

CREATE TABLE IF NOT EXISTS exchange_filter_info (
    symbol VARCHAR(20) PRIMARY KEY,
    tick_size NUMERIC(28, 8) NOT NULL,
    step_size NUMERIC(28, 8) NOT NULL,
    min_qty NUMERIC(28, 8) NOT NULL,
    max_qty NUMERIC(28, 8) NOT NULL,
    min_notional NUMERIC(28, 8) NOT NULL,
    price_precision INT NOT NULL,
    quantity_precision INT NOT NULL
);

CREATE TABLE IF NOT EXISTS testnet_orders (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    client_order_id VARCHAR(50) UNIQUE NOT NULL,
    binance_order_id VARCHAR(50),
    side VARCHAR(10) NOT NULL,
    type VARCHAR(10) NOT NULL,
    price NUMERIC(28, 8) NOT NULL,
    quantity NUMERIC(28, 8) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS testnet_audit_logs (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    action VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    details VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);
