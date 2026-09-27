import { readFileSync } from 'node:fs';
const spec = JSON.parse(readFileSync(new URL('../contracts/access.openapi.json', import.meta.url)));
const required = ['/api/auth/sign-up', '/api/auth/sign-in', '/api/auth/sign-out', '/api/auth/session', '/api/workspaces', '/api/workspaces/{workspaceId}/onboarding', '/api/workspaces/{workspaceId}/onboarding/actions', '/api/workspaces/{workspaceId}/invitations', '/api/invitations/{token}', '/api/auth/google/start', '/api/auth/google/callback'];
const missing = required.filter((path) => !spec.paths[path]);
if (missing.length) { console.error(`Missing Access paths: ${missing.join(', ')}`); process.exit(1); }
const source = readFileSync(new URL('../frontend/src/api/generated.ts', import.meta.url), 'utf8');
for (const operation of ['signUp', 'signIn', 'signOut', 'session', 'workspaces', 'createWorkspace']) if (!source.includes(`${operation}(`)) { console.error(`Missing generated operation: ${operation}`); process.exit(1); }
console.log('Access OpenAPI document and generated client are aligned.');
