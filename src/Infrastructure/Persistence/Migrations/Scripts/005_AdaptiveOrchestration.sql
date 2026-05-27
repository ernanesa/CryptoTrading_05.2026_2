CREATE TABLE IF NOT EXISTS strategy_performance_metrics (
    strategy_name VARCHAR(100) NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    timeframe VARCHAR(10) NOT NULL,
    regime VARCHAR(50) NOT NULL,
    win_rate NUMERIC NOT NULL DEFAULT 0,
    profit_factor NUMERIC NOT NULL DEFAULT 0,
    max_drawdown NUMERIC NOT NULL DEFAULT 0,
    consecutive_losses INT NOT NULL DEFAULT 0,
    slippage_tolerance NUMERIC NOT NULL DEFAULT 0,
    risk_rejections INT NOT NULL DEFAULT 0,
    last_updated TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (strategy_name, symbol, timeframe, regime)
);

CREATE TABLE IF NOT EXISTS strategy_states (
    strategy_name VARCHAR(100) NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    is_paused BOOLEAN NOT NULL DEFAULT false,
    cooldown_until TIMESTAMPTZ,
    last_score NUMERIC NOT NULL DEFAULT 0,
    advantage_cycles INT NOT NULL DEFAULT 0,
    last_updated TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (strategy_name, symbol)
);
