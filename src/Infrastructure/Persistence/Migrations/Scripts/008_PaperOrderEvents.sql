CREATE TABLE IF NOT EXISTS paper_order_events (
    id BIGSERIAL PRIMARY KEY,
    paper_order_id BIGINT NOT NULL REFERENCES paper_orders(id) ON DELETE CASCADE,
    client_order_id VARCHAR(50) NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    from_status VARCHAR(20),
    to_status VARCHAR(20) NOT NULL,
    event_type VARCHAR(40) NOT NULL,
    reason VARCHAR(255) NOT NULL,
    fill_quantity NUMERIC(28, 8),
    fill_price NUMERIC(28, 8),
    fee NUMERIC(28, 8),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_paper_order_events_order ON paper_order_events (paper_order_id, created_at ASC, id ASC);
CREATE INDEX IF NOT EXISTS idx_paper_order_events_symbol ON paper_order_events (symbol, created_at DESC);
