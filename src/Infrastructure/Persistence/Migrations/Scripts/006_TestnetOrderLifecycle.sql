ALTER TABLE testnet_orders
ADD COLUMN IF NOT EXISTS original_exchange_status VARCHAR(40);
