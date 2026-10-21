// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Dashboard } from './DashboardScreens'
import { Reports } from './ReportsScreens'

type Notification = { id: string; title: string; body: string; createdAt: string; readAt: string | null }
type Deferred<T> = { promise: Promise<T>; resolve: (value: T) => void; reject: (reason: unknown) => void }
const workspace = { id: 'workspace-1', businessName: 'Rossi Elettronica', country: 'IT', currency: 'EUR', role: 'Owner' }
const session = { userId: 'owner-1', fullName: 'Marco Rossi', email: 'marco@example.test', activeWorkspaceId: workspace.id, workspaces: [workspace] }
const dashboard = {
  state: 'empty', title: 'Dashboard', subtitle: 'Workspace activity', statusLabel: 'All platforms connected', statusTone: 'ok',
  metrics: [], reservations: [], salesByPlatform: [], attention: [], getStarted: [], searchIndex: [], notifications: [],
}
const initialNotifications = (): Notification[] => [
  { id: 'notification-a', title: 'Low stock A', body: 'Product A is low.', createdAt: '2026-10-20T10:00:00Z', readAt: null },
  { id: 'notification-b', title: 'Low stock B', body: 'Product B is low.', createdAt: '2026-10-20T10:01:00Z', readAt: null },
]
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
const empty = () => new Response(null, { status: 204 })

let root: Root | null = null
let host: HTMLDivElement
let notifications: Notification[]
let readGates: Map<string, Deferred<Response>[]>
let readAttempts: Map<string, number>
let fetchMock: ReturnType<typeof vi.fn>

function deferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}

function setReadGates() {
  readGates = new Map([
    ['notification-a', [deferred<Response>(), deferred<Response>()]],
    ['notification-b', [deferred<Response>()]],
  ])
}

beforeEach(() => {
  Object.defineProperty(globalThis, 'IS_REACT_ACT_ENVIRONMENT', { configurable: true, value: true })
  notifications = initialNotifications()
  setReadGates()
  readAttempts = new Map()
  fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method || 'GET'
    if (url === '/api/auth/session') return json(session)
    if (url.endsWith('/api/workspaces/workspace-1/dashboard')) return json(dashboard)
    if (url.endsWith('/api/workspaces/workspace-1/products')) return json([])
    if (url.endsWith('/api/workspaces/workspace-1/notifications') && method === 'GET') return json({ notifications, unreadCount: notifications.filter(item => item.readAt === null).length })
    const match = url.match(/\/api\/workspaces\/workspace-1\/notifications\/([^/]+)\/read$/)
    if (match && method === 'POST') {
      const id = match[1]
      const attempt = (readAttempts.get(id) || 0) + 1
      readAttempts.set(id, attempt)
      const gate = readGates.get(id)?.[attempt - 1]
      if (!gate) throw new Error(`Unexpected mark-read attempt ${attempt} for ${id}`)
      return gate.promise.then(response => {
        if (response.ok) notifications = notifications.map(item => item.id === id ? { ...item, readAt: '2026-10-21T10:00:00Z' } : item)
        return response
      })
    }
    return json({ title: 'Unavailable', detail: `Unexpected request: ${method} ${url}` }, 404)
  })
  vi.stubGlobal('fetch', fetchMock)
  host = document.createElement('div')
  document.body.append(host)
})

afterEach(async () => {
  if (root) await act(async () => { root?.unmount() })
  root = null
  host.remove()
  vi.unstubAllGlobals()
})

async function flush() {
  await act(async () => { await new Promise(resolve => setTimeout(resolve, 0)) })
}

async function renderSurface(surface: 'Dashboard' | 'Reports') {
  root = createRoot(host)
  await act(async () => { root?.render(createElement(surface === 'Dashboard' ? Dashboard : Reports)) })
  await flush()
  const toggle = host.querySelector<HTMLButtonElement>(surface === 'Dashboard' ? 'button[aria-label="Notifications"]' : 'button[aria-label^="Notifications,"]')
  expect(toggle).not.toBeNull()
  await act(async () => toggle?.click())
  await flush()
  expect(host.textContent).toContain('Notifications · 2 unread')
}

function markReadButton(id: 'notification-a' | 'notification-b') {
  const article = Array.from(host.querySelectorAll('article')).find(item => item.textContent?.includes(id === 'notification-a' ? 'Low stock A' : 'Low stock B'))
  return article?.querySelector<HTMLButtonElement>('button') ?? null
}

describe.each(['Dashboard', 'Reports'] as const)('%s notifications', surface => {
  it('keeps every overlapping request visibly pending and updates the unread count once', async () => {
    await renderSurface(surface)
    const buttonA = markReadButton('notification-a')
    const buttonB = markReadButton('notification-b')
    expect(buttonA?.disabled).toBe(false)
    expect(buttonB?.disabled).toBe(false)

    await act(async () => buttonA?.click())
    await act(async () => buttonB?.click())
    expect(markReadButton('notification-a')?.disabled).toBe(true)
    expect(markReadButton('notification-b')?.disabled).toBe(true)
    expect(readAttempts.get('notification-a')).toBe(1)
    expect(readAttempts.get('notification-b')).toBe(1)

    await act(async () => markReadButton('notification-a')?.click())
    expect(readAttempts.get('notification-a')).toBe(1)
    await act(async () => {
      readGates.get('notification-b')?.[0].resolve(empty())
      readGates.get('notification-a')?.[0].resolve(empty())
      await Promise.resolve()
    })

    expect(host.textContent).toContain('Notifications · 0 unread')
    expect(markReadButton('notification-a')).toBeNull()
    expect(markReadButton('notification-b')).toBeNull()
  })

  it('re-enables a failed request for retry and decrements only after success', async () => {
    await renderSurface(surface)
    await act(async () => markReadButton('notification-a')?.click())
    await act(async () => { readGates.get('notification-a')?.[0].reject(new Error('network failure')) })
    await flush()

    expect(host.textContent).toContain('Notifications · 2 unread')
    expect(host.textContent).toContain('Could not update this notification.')
    expect(markReadButton('notification-a')?.disabled).toBe(false)

    await act(async () => markReadButton('notification-a')?.click())
    expect(markReadButton('notification-a')?.disabled).toBe(true)
    await act(async () => {
      readGates.get('notification-a')?.[1].resolve(empty())
      await Promise.resolve()
    })

    expect(host.textContent).toContain('Notifications · 1 unread')
    expect(markReadButton('notification-a')).toBeNull()
    expect(readAttempts.get('notification-a')).toBe(2)
  })
})
