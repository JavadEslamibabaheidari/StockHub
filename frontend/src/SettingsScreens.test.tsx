// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Settings } from './SettingsScreens'

vi.mock('./api/generated', () => ({
  AccessApiClient: class {
    session = async () => ({
      fullName: 'Alex Morgan', email: 'alex@example.test', activeWorkspaceId: 'w1',
      workspaces: [{ id: 'w1', businessName: 'North Shop', country: 'Italy', currency: 'EUR', role: 'Owner' }],
    })
    setActiveWorkspace = async () => undefined
  },
  AccessApiError: class extends Error {},
}))

let host: HTMLDivElement
let root: Root

beforeEach(async () => {
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
})
