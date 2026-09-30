CREATE TABLE IF NOT EXISTS products (
    id uuid PRIMARY KEY,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    sku text NOT NULL,
    name text NOT NULL,
    on_hand integer NOT NULL CHECK (on_hand >= 0),
    base_price numeric(12,2) NOT NULL CHECK (base_price >= 0),
    category text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, sku)
);

CREATE INDEX IF NOT EXISTS products_workspace_name_idx ON products(workspace_id, lower(name));

INSERT INTO schema_migrations(version)
VALUES ('003_onboarding_products')
ON CONFLICT (version) DO NOTHING;
