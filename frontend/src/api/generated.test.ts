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
})
