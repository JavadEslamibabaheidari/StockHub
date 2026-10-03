import { afterEach, describe, expect, it, vi } from 'vitest'
import { AccessApiClient } from './generated'
afterEach(() => {
  vi.unstubAllGlobals()
})
describe('AccessApiClient origin', () => {
  it('uses relative API paths by default for same-origin deployments', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          userId: 'user-1',
          fullName: 'Ada Lovelace',
          email: 'ada@example.com',
          activeWorkspaceId: null,
          workspaces: [],
        }),
        { headers: { 'Content-Type': 'application/json' } },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)
    await new AccessApiClient().session()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/auth/session',
      expect.objectContaining({ credentials: 'include' }),
    )
    expect(String(fetchMock.mock.calls[0]?.[0])).not.toMatch(/^https?:\/\//)
  })

  it('retains JSON content type when sending an idempotency key', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ id: 'workspace-1' }), {
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    await new AccessApiClient().createWorkspace(
      { businessName: 'Rossi Elettronica', country: 'IT', currency: 'EUR' },
      'request-1',
    )
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(init.headers).toMatchObject({
      'Content-Type': 'application/json',
      'Idempotency-Key': 'request-1',
    })
  })
  it('preserves status codes for empty API errors', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 401, statusText: 'Unauthorized' }))
    vi.stubGlobal('fetch', fetchMock)
    await expect(new AccessApiClient().session()).rejects.toMatchObject({
      status: 401,
      problem: expect.objectContaining({ detail: 'Unauthorized' }),
    })
  })

  it('uses workspace-scoped Inventory API paths', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ totalProducts: 0, syncSummary: 'No platforms connected', products: [], capabilities: { canAdjustOnHand: true, canChangePrice: true, canManageListings: true } }), {
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)
    await new AccessApiClient().inventory('workspace-1')
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/workspaces/workspace-1/inventory/products',
      expect.objectContaining({ credentials: 'include' }),
    )
  })
})
