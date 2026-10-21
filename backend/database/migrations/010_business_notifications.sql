CREATE TABLE IF NOT EXISTS alert_rule_states (
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    rule_key text NOT NULL,
    subject_key text NOT NULL,
    is_active boolean NOT NULL,
    active_since timestamptz,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY(workspace_id, rule_key, subject_key)
);

CREATE TABLE IF NOT EXISTS alert_notification_jobs (
    id uuid PRIMARY KEY,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    recipient_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    rule_key text NOT NULL,
    subject_key text NOT NULL,
    dedupe_key text NOT NULL,
    title text NOT NULL,
    body text NOT NULL,
    channel text NOT NULL CHECK (channel IN ('inApp','email','slack')),
    status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','processing','delivered','dead')),
    attempts integer NOT NULL DEFAULT 0,
    next_attempt_at timestamptz NOT NULL DEFAULT now(),
    lease_expires_at timestamptz,
    last_error text,
    created_at timestamptz NOT NULL DEFAULT now(),
    delivered_at timestamptz,
    UNIQUE(workspace_id, recipient_id, dedupe_key)
);
CREATE INDEX IF NOT EXISTS ix_alert_notification_jobs_due ON alert_notification_jobs(status,next_attempt_at,lease_expires_at,created_at) WHERE status IN ('pending','processing');

CREATE TABLE IF NOT EXISTS business_notifications (
    id uuid PRIMARY KEY,
    job_id uuid NOT NULL UNIQUE REFERENCES alert_notification_jobs(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    recipient_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title text NOT NULL,
    body text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    read_at timestamptz
);
CREATE INDEX IF NOT EXISTS ix_business_notifications_recipient ON business_notifications(workspace_id,recipient_id,created_at DESC);

CREATE TABLE IF NOT EXISTS workspace_alert_integrations (
    workspace_id uuid PRIMARY KEY REFERENCES workspaces(id) ON DELETE CASCADE,
    slack_webhook_protected text NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO schema_migrations(version) VALUES ('010_business_notifications') ON CONFLICT (version) DO NOTHING;
