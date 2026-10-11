ALTER TABLE invitations
    ADD COLUMN IF NOT EXISTS id uuid;

UPDATE invitations
SET id = gen_random_uuid()
WHERE id IS NULL;

ALTER TABLE invitations
    ALTER COLUMN id SET NOT NULL,
    ALTER COLUMN id SET DEFAULT gen_random_uuid();

CREATE UNIQUE INDEX IF NOT EXISTS invitations_id_unique ON invitations(id);
CREATE INDEX IF NOT EXISTS invitations_pending_workspace_created
    ON invitations(workspace_id, created_at DESC)
    WHERE consumed_at IS NULL;

INSERT INTO schema_migrations(version)
VALUES ('008_team_management')
ON CONFLICT (version) DO NOTHING;
