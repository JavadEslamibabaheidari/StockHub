// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Settings } from './SettingsScreens'
import { AccessApiError } from './api/generated'

const { sessionMock, setActiveWorkspaceMock } = vi.hoisted(() => ({ sessionMock: vi.fn(), setActiveWorkspaceMock: vi.fn() }))
const activeSession = {
  fullName: 'Alex Morgan', email: 'alex@example.test', activeWorkspaceId: 'w1',
  workspaces: [{ id: 'w1', businessName: 'North Shop', country: 'Italy', currency: 'EUR', role: 'Owner' }],
}

vi.mock('./api/generated', () => ({
  AccessApiClient: class {
    session = () => sessionMock()
    setActiveWorkspace = (workspaceId: string) => setActiveWorkspaceMock(workspaceId)
  },
  AccessApiError: class extends Error {
    status: number
    problem: { detail: string }
    constructor(status: number, problem: { detail: string }) { super(problem.detail); this.status = status; this.problem = problem }
  },
}))

let host: HTMLDivElement
let root: Root

beforeEach(async () => {
  sessionMock.mockReset().mockResolvedValue(activeSession)
  setActiveWorkspaceMock.mockReset().mockResolvedValue(undefined)
  host = document.createElement('div')
  document.body.append(host)
  root = createRoot(host)
  await act(async () => { root.render(createElement(Settings)) })
  await act(async () => { await Promise.resolve(); await Promise.resolve() })
})

afterEach(async () => {
  await act(async () => root.unmount())
  host.remove()
})

describe('Settings screen', () => {
  it('shows workspace-backed billing identity and honest unavailable billing states', () => {
    expect(host.textContent).toContain('North Shop')
    expect(host.textContent).toContain('alex@example.test')
    expect(host.textContent).toContain('StockHub does not currently provide subscription management or payment processing')
    expect(host.textContent).toContain('Payment methods, invoices, and plan changes are not available in this workspace.')
    expect(host.textContent).toContain('Plan comparison is unavailable.')
    expect(host.textContent).not.toContain('4242')
  })

  it('offers Light, Dark, and System appearance controls', async () => {
    const appearanceTab = Array.from(host.querySelectorAll('button')).find(button => button.textContent === 'Appearance')
    expect(appearanceTab).toBeDefined()
    await act(async () => appearanceTab?.click())

    for (const label of ['Light', 'Dark', 'System']) expect(host.textContent).toContain(label)
    const dark = Array.from(host.querySelectorAll('button')).find(button => button.querySelector('strong')?.textContent === 'Dark')
    await act(async () => dark?.click())
    expect(dark?.getAttribute('aria-pressed')).toBe('true')
    expect(localStorage.getItem('stockhub-appearance')).toBe('dark')
    expect(document.documentElement.dataset.theme).toBe('dark')
  })

  it('shows a recoverable session error and retries successfully', async () => {
    await act(async () => root.unmount())
    sessionMock.mockReset().mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(activeSession)
    root = createRoot(host)
    await act(async () => { root.render(createElement(Settings)) })
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    expect(host.textContent).toContain('Settings are unavailable')
    const retry = Array.from(host.querySelectorAll('button')).find(button => button.textContent === 'Try again')
    expect(retry).toBeDefined()
    await act(async () => retry?.click())
    await act(async () => { await Promise.resolve(); await Promise.resolve() })
    expect(host.textContent).toContain('North Shop')
    expect(host.textContent).not.toContain('Settings are unavailable')
  })

  it('routes an expired session to sign-in', async () => {
    const originalHash = window.location.hash
    await act(async () => root.unmount())
    sessionMock.mockReset().mockRejectedValueOnce(new AccessApiError(401, { title: 'Unauthorized', detail: 'Please sign in again.' }))
    root = createRoot(host)
    await act(async () => { root.render(createElement(Settings)) })
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    expect(window.location.hash).toBe('#signin')
    window.location.hash = originalHash
  })

  it('routes an account without a workspace to workspace creation', async () => {
    const originalHash = window.location.hash
    await act(async () => root.unmount())
    sessionMock.mockReset().mockResolvedValueOnce({ ...activeSession, activeWorkspaceId: null, workspaces: [] })
    root = createRoot(host)
    await act(async () => { root.render(createElement(Settings)) })
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    expect(window.location.hash).toBe('#workspace')
    window.location.hash = originalHash
  })

  it('switches the active workspace using the existing session action', async () => {
    const secondWorkspace = { id: 'w2', businessName: 'South Shop', country: 'France', currency: 'EUR', role: 'Admin' }
    await act(async () => root.unmount())
    sessionMock.mockReset().mockResolvedValueOnce({ ...activeSession, workspaces: [...activeSession.workspaces, secondWorkspace] })
    root = createRoot(host)
    await act(async () => { root.render(createElement(Settings)) })
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    const workspaceButton = host.querySelector('.inventory-workspace') as HTMLButtonElement
    await act(async () => workspaceButton.click())
    const second = Array.from(host.querySelectorAll('.workspace-menu button')).find(button => button.textContent?.includes('South Shop')) as HTMLButtonElement | undefined
    await act(async () => second?.click())

    expect(setActiveWorkspaceMock).toHaveBeenCalledWith('w2')
    expect(host.textContent).toContain('South Shop')
  })
})
