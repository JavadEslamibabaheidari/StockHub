CREATE EXTENSION IF NOT EXISTS citext;

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
    country char(2) NOT NULL,
    currency char(3) NOT NULL,
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
