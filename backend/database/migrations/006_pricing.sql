CREATE TABLE IF NOT EXISTS pricing_rules (
    id uuid PRIMARY KEY,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    creation_sequence bigint GENERATED ALWAYS AS IDENTITY,
    name text NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    platform text NOT NULL CHECK (platform IN ('Amazon','Unieuro','Euronics','eBay')),
    scope text NOT NULL CHECK (scope IN ('All','Category','Sku')),
    scope_value text,
    adjustment numeric NOT NULL,
    unit text NOT NULL CHECK (unit IN ('Percent','Euro')),
    enabled boolean NOT NULL,
    priority integer NOT NULL CHECK (priority BETWEEN 1 AND 999),
    start_date date,
    end_date date,
    CHECK ((start_date IS NULL AND end_date IS NULL)
        OR (start_date IS NOT NULL AND end_date IS NOT NULL AND start_date <= end_date)),
    CHECK ((scope = 'All' AND scope_value IS NULL)
        OR (scope <> 'All' AND length(scope_value) > 0)),
    CHECK ((unit = 'Percent' AND adjustment BETWEEN -100 AND 100)
        OR (unit = 'Euro' AND adjustment BETWEEN -1000000 AND 1000000))
);
CREATE INDEX IF NOT EXISTS pricing_rules_workspace_order
    ON pricing_rules(workspace_id, priority, creation_sequence);
