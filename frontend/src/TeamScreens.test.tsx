// @vitest-environment jsdom
import { act } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import type { TeamResponse, WorkspaceRole } from './api/generated'

const firstWorkspace = { id: 'workspace-1', businessName: 'Rossi Elettronica', country: 'IT', currency: 'EUR', role: 'Owner' as WorkspaceRole }
const secondWorkspace = { id: 'workspace-2', businessName: 'Bianchi Hi-Fi', country: 'IT', currency: 'EUR', role: 'Manager' as WorkspaceRole }
const session = {
  userId: 'owner-1', fullName: 'Marco Rossi', email: 'marco@example.test', activeWorkspaceId: firstWorkspace.id,
  workspaces: [firstWorkspace, secondWorkspace],
}
const initialTeam: TeamResponse = {
  canManage: true,
  members: [
    { userId: 'owner-1', fullName: 'Marco Rossi', email: 'marco@example.test', role: 'Owner', lastActive: 'Unavailable' },
    { userId: 'manager-1', fullName: 'Giulia Rossi', email: 'giulia@example.test', role: 'Manager', lastActive: 'Unavailable' },
    { userId: 'warehouse-1', fullName: 'Luca Bianchi', email: 'luca@example.test', role: 'WarehouseStaff', lastActive: 'Unavailable' },
  ],
  invitations: [{ id: 'invite-1', email: 'andrea@example.test', role: 'Manager', createdAt: '2026-10-10T10:00:00Z', expiresAt: '2026-10-13T10:00:00Z' }],
}
const secondTeam: TeamResponse = {
  canManage: false,
  members: [{ userId: 'other-1', fullName: 'Bianchi Viewer', email: 'viewer@example.test', role: 'Viewer', lastActive: 'Unavailable' }],
  invitations: [],
}
const json = (body: unknown, status = 200) => new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
let root: Root | null = null
let host: HTMLDivElement
let teamOne: TeamResponse
let teamTwo: TeamResponse
let failResend = false
let requests: { url: string; method: string; body?: string }[]
let fetchMock: ReturnType<typeof vi.fn>

function getRole(body?: string) { return body ? JSON.parse(body).role as WorkspaceRole : undefined }

beforeEach(() => {
  Object.defineProperty(globalThis, 'IS_REACT_ACT_ENVIRONMENT', { configurable: true, value: true })
  window.location.hash = '#team'
  localStorage.clear()
  teamOne = structuredClone(initialTeam)
  teamTwo = structuredClone(secondTeam)
  failResend = false
  requests = []
  fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method || 'GET'
    requests.push({ url, method, body: typeof init?.body === 'string' ? init.body : undefined })
    if (url === '/api/auth/session') return json(session)
    if (url.endsWith('/api/workspaces/workspace-1/active') || url.endsWith('/api/workspaces/workspace-2/active')) return json(undefined, 204)
    if (url === '/api/workspaces/workspace-1/team') return json(teamOne)
    if (url === '/api/workspaces/workspace-2/team') return json(teamTwo)
    if (url === '/api/workspaces/workspace-1/invitations' && method === 'POST') {
      const body = JSON.parse(String(init?.body))
      teamOne = { ...teamOne, invitations: [...teamOne.invitations, { id: 'invite-new', email: body.email, role: body.role, createdAt: '2026-10-11T12:00:00Z', expiresAt: '2026-10-14T12:00:00Z' }] }
      return json({ status: 'sent', expiresInHours: 72 }, 202)
    }
    if (url.endsWith('/invite-1/resend') && failResend) return json({ title: 'Email unavailable', detail: 'Invitation email is temporarily unavailable.', code: 'invitation_delivery_failed' }, 503)
    if (url.endsWith('/invite-1/resend')) return json({ status: 'sent', expiresInHours: 72 }, 202)
    if (url.endsWith('/invitations/invite-1') && method === 'DELETE') {
      teamOne = { ...teamOne, invitations: [] }
      return json(undefined, 204)
    }
    if (url.endsWith('/team/members/manager-1') && method === 'DELETE') {
      teamOne = { ...teamOne, members: teamOne.members.filter(member => member.userId !== 'manager-1') }
      return json(undefined, 204)
    }
    if (url.endsWith('/api/auth/sign-out')) return json(undefined, 204)
    return json({ title: 'Not found', detail: `Unexpected request: ${method} ${url}` }, 404)
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

async function renderTeam() {
  root = createRoot(host)
  await act(async () => {
    root?.render(<App />)
    await new Promise(resolve => setTimeout(resolve, 0))
  })
  await flush()
}

async function flush() {
  await act(async () => { await new Promise(resolve => setTimeout(resolve, 0)) })
}

function button(label: string | RegExp, scope: ParentNode = host): HTMLButtonElement {
  const match = [...scope.querySelectorAll('button')].find(item => typeof label === 'string' ? item.textContent?.trim().includes(label) || item.getAttribute('aria-label') === label : label.test(item.textContent || '') || label.test(item.getAttribute('aria-label') || ''))
  if (!match) throw new Error(`Button not found: ${String(label)}`)
  return match
}

async function click(target: HTMLElement) { await act(async () => { target.click(); await new Promise(resolve => setTimeout(resolve, 0)) }) }
async function setValue(target: HTMLInputElement | HTMLSelectElement, value: string) {
  await act(async () => {
    const setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(target), 'value')?.set
    setter?.call(target, value)
    target.dispatchEvent(new Event(target instanceof HTMLSelectElement ? 'change' : 'input', { bubbles: true }))
  })
}

describe('Milestone 9 Team route', () => {
  it('loads real workspace data and renders the roster, pending invite, role cards, and permission matrix', async () => {
    await renderTeam()
    expect(host.textContent).toContain('Rossi Elettronica')
    expect(host.textContent).toContain('Team members')
    expect(host.textContent).toContain('Giulia Rossi')
    expect(host.textContent).toContain('andrea@example.test')
    expect(host.textContent).toContain('Warehouse staff')
    expect(host.textContent).toContain('What each role can do')
    expect(host.textContent?.toLowerCase()).toContain('cannot access price data')
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-1/team', method: 'GET' }))
  })

  it('validates an invitation locally and sends a selected role only after valid input', async () => {
    await renderTeam()
    await click(button('Invite member'))
    const email = host.querySelector<HTMLInputElement>('input[type="email"]')!
    await setValue(email, 'not-an-email')
    await act(async () => { host.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })) })
    expect(host.textContent).toContain('Enter a valid email address.')
    expect(requests.some(request => request.method === 'POST' && request.url.endsWith('/invitations'))).toBe(false)

    await setValue(email, 'new-member@example.test')
    await setValue(host.querySelector<HTMLSelectElement>('select')!, 'WarehouseStaff')
    await act(async () => { host.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })) })
    await flush()
    const inviteRequest = requests.find(request => request.method === 'POST' && request.url.endsWith('/invitations'))
    expect(inviteRequest).toBeDefined()
    expect(JSON.parse(inviteRequest!.body || '{}')).toEqual({ email: 'new-member@example.test', role: 'WarehouseStaff' })
    expect(host.textContent).toContain('The invitation email was sent to new-member@example.test.')
    expect(host.textContent).toContain('new-member@example.test')
  })

  it('moves focus into the invitation dialog and restores it when closed with Escape', async () => {
    await renderTeam()
    const inviteButton = button('Invite member')
    inviteButton.focus()
    await click(inviteButton)
    const email = host.querySelector<HTMLInputElement>('input[type="email"]')!
    expect(document.activeElement).toBe(email)
    await act(async () => { email.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })) })
    expect(host.querySelector('[role="dialog"]')).toBeNull()
    expect(document.activeElement).toBe(inviteButton)
  })

  it('confirms removal before calling the workspace-scoped endpoint and refreshes the roster', async () => {
    await renderTeam()
    await click(button('Remove Giulia Rossi'))
    expect(host.textContent).toContain('will lose access to Rossi Elettronica immediately')
    expect(requests.some(request => request.url.endsWith('/team/members/manager-1'))).toBe(false)
    await click(button('Remove member'))
    await flush()
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-1/team/members/manager-1', method: 'DELETE' }))
    expect(host.querySelector('section[aria-labelledby="team-members-title"]')?.textContent).not.toContain('Giulia Rossi')
    expect(host.textContent).toContain('Member removed')
  })

  it('confirms resend, reports delivery failures, and leaves the pending invite visible', async () => {
    await renderTeam()
    failResend = true
    await click(button('Resend'))
    await click(button('Resend invitation'))
    await flush()
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-1/invitations/invite-1/resend', method: 'POST' }))
    expect(host.textContent).toContain('Invitation email is temporarily unavailable.')
    expect(host.textContent).toContain('andrea@example.test')
  })

  it('confirms cancellation before invalidating the pending invitation', async () => {
    await renderTeam()
    await click(button('Cancel invite'))
    expect(host.textContent).toContain('will be invalidated')
    expect(requests.some(request => request.method === 'DELETE' && request.url.endsWith('/invitations/invite-1'))).toBe(false)
    await click(button('Cancel invitation'))
    await flush()
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-1/invitations/invite-1', method: 'DELETE' }))
    expect(host.querySelector('section[aria-labelledby="team-invitations-title"]')?.textContent).not.toContain('andrea@example.test')
    expect(host.textContent).toContain('Invitation cancelled')
  })

  it('hides management actions for read-only roles and reloads only the selected workspace', async () => {
    await renderTeam()
    await click(button('Switch workspace'))
    await click(button('Bianchi Hi-Fi'))
    await flush()
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-2/active', method: 'PUT' }))
    expect(requests).toContainEqual(expect.objectContaining({ url: '/api/workspaces/workspace-2/team', method: 'GET' }))
    expect(host.textContent).toContain('Bianchi Viewer')
    expect(host.textContent).not.toContain('Giulia Rossi')
    expect(host.textContent).toContain('Read-only access')
    expect(host.querySelector('button.team-primary-button')).toBeNull()
    expect([...host.querySelectorAll('button')].some(item => item.textContent?.trim() === 'Remove')).toBe(false)
  })
})
