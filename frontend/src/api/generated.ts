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
export type InvitationResponse = { email: string; role: WorkspaceRole; status: string };
export type ProductRequest = { sku: string; name: string; onHand: number; basePrice: number; category?: string | null };
export type ProductResponse = ProductRequest & { id: string };
export type DashboardMetricResponse = { key: string; label: string; value: string; hint: string; tone: string };
export type DashboardReservationResponse = { id: string; productName: string; platform: string; orderNumber: string; quantity: string; timeLeft: string; progressPercent: number };
export type DashboardPlatformSaleResponse = { platform: string; percent: number; tone: string };
export type DashboardActionResponse = { key: string; label: string; style: string };
export type DashboardAttentionResponse = { id: string; kind: string; tone: string; title: string; detail: string; actions: DashboardActionResponse[] };
export type DashboardGetStartedResponse = { key: string; title: string; detail: string; status: string };
export type DashboardSyncStepResponse = { key: string; title: string; detail: string; status: string };
export type DashboardSyncResponse = { platform: string; syncedProducts: number; totalProducts: number; remainingLabel: string; steps: DashboardSyncStepResponse[] };
export type DashboardSearchResultResponse = { id: string; type: string; label: string; detail: string; action: string };
export type DashboardSnapshotResponse = { state: string; title: string; subtitle: string; statusLabel: string; statusTone: string; metrics: DashboardMetricResponse[]; reservations: DashboardReservationResponse[]; salesByPlatform: DashboardPlatformSaleResponse[]; attention: DashboardAttentionResponse[]; getStarted: DashboardGetStartedResponse[]; sync?: DashboardSyncResponse | null; searchIndex: DashboardSearchResultResponse[]; notifications: string[] };
export type DashboardActionRequest = { action: string; targetId?: string | null; onHand?: number | null; platform?: string | null };
export type DashboardActionResultResponse = { status: string; message: string; snapshot: DashboardSnapshotResponse };

export class AccessApiError extends Error { constructor(public readonly status: number, public readonly problem: Problem) { super(problem.detail); } }
export class AccessApiClient {
  constructor(private readonly baseUrl = '') {}
  private async request<T>(path: string, init: RequestInit = {}): Promise<T> { const response = await fetch(`${this.baseUrl}${path}`, { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } }); if (!response.ok) { const problem = await readProblem(response); throw new AccessApiError(response.status, problem); } return response.status === 204 ? undefined as T : await response.json() as T; }
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
  products(workspaceId: string) { return this.request<ProductResponse[]>(`/api/workspaces/${workspaceId}/products`); }
  importProducts(workspaceId: string, products: ProductRequest[]) { return this.request<ProductResponse[]>(`/api/workspaces/${workspaceId}/products/import`, { method: 'POST', body: JSON.stringify(products) }); }
  dashboard(workspaceId: string) { return this.request<DashboardSnapshotResponse>(`/api/workspaces/${workspaceId}/dashboard`); }
  dashboardAction(workspaceId: string, request: DashboardActionRequest) { return this.request<DashboardActionResultResponse>(`/api/workspaces/${workspaceId}/dashboard/actions`, { method: 'POST', body: JSON.stringify(request) }); }
  invite(workspaceId: string, request: InvitationRequest) { return this.request<{ status: string }>(`/api/workspaces/${workspaceId}/invitations`, { method: 'POST', body: JSON.stringify(request) }); }
  invitation(token: string) { return this.request<InvitationResponse>(`/api/invitations/${encodeURIComponent(token)}`); }
  acceptInvitation(token: string) { return this.request<{ status: string }>('/api/invitations/accept', { method: 'POST', body: JSON.stringify({ token }) }); }
}
async function readProblem(response: Response): Promise<Problem> {
  const fallback: Problem = { title: `Request failed (${response.status})`, detail: response.statusText || 'The request could not be completed.', code: null }
  const text = await response.text().catch(() => '')
  if (!text) return fallback
  try {
    const parsed = JSON.parse(text) as Partial<Problem>
    return { title: parsed.title || fallback.title, detail: parsed.detail || fallback.detail, code: parsed.code ?? null }
  } catch {
    return { ...fallback, detail: text }
  }
}
