CREATE TABLE IF NOT EXISTS paper_positions (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    type VARCHAR(10) NOT NULL,
    entry_price NUMERIC(28, 8) NOT NULL,
    quantity NUMERIC(28, 8) NOT NULL,
    entry_time TIMESTAMP WITH TIME ZONE NOT NULL,
    exit_price NUMERIC(28, 8),
    exit_time TIMESTAMP WITH TIME ZONE,
    realized_pnl NUMERIC(28, 8) NOT NULL,
    fees_paid NUMERIC(28, 8) NOT NULL,
    stop_loss_price NUMERIC(28, 8),
    take_profit_price NUMERIC(28, 8),
    is_closed BOOLEAN NOT NULL DEFAULT FALSE
);
