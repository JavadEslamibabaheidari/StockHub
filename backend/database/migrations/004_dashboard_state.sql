CREATE TABLE IF NOT EXISTS dashboard_states (
    workspace_id uuid PRIMARY KEY REFERENCES workspaces(id) ON DELETE CASCADE,
    mode text NOT NULL DEFAULT 'first-use',
    sync_platform text NOT NULL DEFAULT 'Amazon',
    euronics_retried boolean NOT NULL DEFAULT false,
    mismatch_resolved boolean NOT NULL DEFAULT false,
    restock_listed boolean NOT NULL DEFAULT false,
    show_more_low_stock boolean NOT NULL DEFAULT false,
    detail_panel text NULL,
    updated_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO schema_migrations(version)
VALUES ('004_dashboard_state')
ON CONFLICT (version) DO NOTHING;
