CREATE TABLE IF NOT EXISTS onboarding_selections (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    workspace_id uuid NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    selected_action_key text NOT NULL CHECK (selected_action_key IN ('import-products', 'connect-platform', 'invite-team')),
    selected_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, workspace_id)
);

INSERT INTO onboarding_selections(user_id, workspace_id, selected_action_key, selected_at)
SELECT DISTINCT ON (user_id, workspace_id)
    user_id,
    workspace_id,
    action_key,
    requested_at
FROM onboarding_actions
WHERE action_key IN ('import-products', 'connect-platform', 'invite-team')
ORDER BY user_id, workspace_id, requested_at DESC, action_key
ON CONFLICT (user_id, workspace_id) DO NOTHING;

INSERT INTO schema_migrations(version)
VALUES ('009_onboarding_selection')
ON CONFLICT (version) DO NOTHING;
