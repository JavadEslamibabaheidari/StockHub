CREATE TABLE IF NOT EXISTS orders (
    id uuid PRIMARY KEY,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    order_number text NOT NULL,
    platform text NOT NULL,
    customer_name text NOT NULL,
    ship_to text NOT NULL,
    carrier text NOT NULL,
    placed_at timestamptz NOT NULL,
    status text NOT NULL CHECK (status IN ('AwaitingPayment','PaidToPick','PickedToShip','Shipped','Delivered','ReturnRequested','Cancelled')),
    return_stage text NULL CHECK (return_stage IS NULL OR return_stage IN ('Requested','Approved','Received','Restocked','Refunded','Rejected')),
    return_reason text NULL,
    cancel_reason text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, order_number)
);

CREATE INDEX IF NOT EXISTS orders_workspace_placed_at_idx ON orders(workspace_id, placed_at DESC);
CREATE INDEX IF NOT EXISTS orders_workspace_status_idx ON orders(workspace_id, status);

CREATE TABLE IF NOT EXISTS order_items (
    id uuid PRIMARY KEY,
    order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id uuid NULL REFERENCES products(id) ON DELETE SET NULL,
    sku text NOT NULL,
    product_name text NOT NULL,
    quantity integer NOT NULL CHECK (quantity > 0),
    unit_price numeric(12,2) NOT NULL CHECK (unit_price >= 0),
    cost_of_goods numeric(12,2) NOT NULL CHECK (cost_of_goods >= 0)
);

CREATE INDEX IF NOT EXISTS order_items_order_id_idx ON order_items(order_id);
CREATE INDEX IF NOT EXISTS order_items_product_id_idx ON order_items(product_id);

CREATE TABLE IF NOT EXISTS order_events (
    id uuid PRIMARY KEY,
    order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    kind text NOT NULL,
    title text NOT NULL,
    detail text NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS order_events_order_id_idx ON order_events(order_id, occurred_at);

INSERT INTO schema_migrations(version)
VALUES ('005_orders')
ON CONFLICT (version) DO NOTHING;
