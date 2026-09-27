CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE IF NOT EXISTS schema_migrations (
    version text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY,
    full_name text NOT NULL,
    email citext NOT NULL UNIQUE,
    password_hash text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS workspaces (
    id uuid PRIMARY KEY,
    business_name text NOT NULL,
    country varchar(2) NOT NULL,
    currency varchar(3) NOT NULL,
    vat_number text NULL,
    slug text NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS memberships (
    user_id uuid NOT NULL REFERENCES users(id),
    workspace_id uuid NOT NULL REFERENCES workspaces(id),
    role text NOT NULL CHECK (role IN ('Owner','Admin','Manager','WarehouseStaff','Viewer')),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, workspace_id)
);

CREATE TABLE IF NOT EXISTS sessions (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES users(id),
    active_workspace_id uuid NULL REFERENCES workspaces(id),
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS memberships_one_owner_per_workspace ON memberships(workspace_id) WHERE role = 'Owner';

CREATE TABLE IF NOT EXISTS workspace_idempotency (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    idempotency_key text NOT NULL,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    request_fingerprint text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, idempotency_key)
);

CREATE TABLE IF NOT EXISTS invitations (
    token_hash text PRIMARY KEY,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    email citext NOT NULL,
    role text NOT NULL CHECK (role IN ('Owner','Admin','Manager','WarehouseStaff','Viewer')),
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS onboarding_actions (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    action_key text NOT NULL,
    requested_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, workspace_id, action_key)
);

INSERT INTO schema_migrations(version)
VALUES ('001_access')
ON CONFLICT (version) DO NOTHING;
