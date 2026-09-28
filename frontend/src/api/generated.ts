export type Problem = { title: string; detail: string; code?: string | null };
export type SignUpRequest = { fullName: string; email: string; password: string };
export type SignInRequest = { email: string; password: string };
export type PasswordResetRequest = { email: string };
export type PasswordResetConfirmRequest = { token: string; newPassword: string };
export type CreateWorkspaceRequest = { businessName: string; country: string; currency: string; vatNumber?: string | null };
export type WorkspaceRole = 'Owner' | 'Admin' | 'Manager' | 'WarehouseStaff' | 'Viewer';
export type WorkspaceSummary = { id: string; businessName: string; country: string; currency: string; role: WorkspaceRole };
export type WorkspaceResponse = WorkspaceSummary & { vatNumber?: string | null; slug: string };
export type SessionResponse = { userId: string; fullName: string; email: string; activeWorkspaceId?: string | null; workspaces: WorkspaceSummary[] };
export type OnboardingTask = { key: string; title: string; status: string; handoff?: string | null };
export type InvitationRequest = { email: string; role: WorkspaceRole };
export class AccessApiError extends Error { constructor(public readonly status: number, public readonly problem: Problem) { super(problem.detail); } }
export class AccessApiClient {
  constructor(private readonly baseUrl = '') {}
  private async request<T>(path: string, init: RequestInit = {}): Promise<T> { const response = await fetch(`${this.baseUrl}${path}`, { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } }); if (!response.ok) throw new AccessApiError(response.status, await response.json() as Problem); return response.status === 204 ? undefined as T : await response.json() as T; }
  signUp(request: SignUpRequest) { return this.request<{ next: string }>('/api/auth/sign-up', { method: 'POST', body: JSON.stringify(request) }); }
  signIn(request: SignInRequest) { return this.request<{ next: string }>('/api/auth/sign-in', { method: 'POST', body: JSON.stringify(request) }); }
  signOut() { return this.request<void>('/api/auth/sign-out', { method: 'POST' }); }
  requestPasswordReset(request: PasswordResetRequest) { return this.request<{ message: string }>('/api/auth/password-reset/request', { method: 'POST', body: JSON.stringify(request) }); }
  confirmPasswordReset(request: PasswordResetConfirmRequest) { return this.request<void>('/api/auth/password-reset/confirm', { method: 'POST', body: JSON.stringify(request) }); }
  session() { return this.request<SessionResponse>('/api/auth/session'); }
  workspaces() { return this.request<WorkspaceSummary[]>('/api/workspaces'); }
  createWorkspace(request: CreateWorkspaceRequest, idempotencyKey: string) { return this.request<WorkspaceResponse>('/api/workspaces', { method: 'POST', headers: { 'Idempotency-Key': idempotencyKey }, body: JSON.stringify(request) }); }
  setActiveWorkspace(workspaceId: string) { return this.request<void>(`/api/workspaces/${workspaceId}/active`, { method: 'PUT' }); }
  onboarding(workspaceId: string) { return this.request<OnboardingTask[]>(`/api/workspaces/${workspaceId}/onboarding`); }
  selectOnboardingAction(workspaceId: string, key: string) { return this.request<void>(`/api/workspaces/${workspaceId}/onboarding/actions`, { method: 'POST', body: JSON.stringify({ key }) }); }
  invite(workspaceId: string, request: InvitationRequest) { return this.request<{ status: string }>(`/api/workspaces/${workspaceId}/invitations`, { method: 'POST', body: JSON.stringify(request) }); }
}
