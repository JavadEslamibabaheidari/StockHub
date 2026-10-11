// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Inventory } from './InventoryScreens'
import { AccessApiError } from './api/generated'

const { sessionMock, inventoryMock, inventoryProductMock } = vi.hoisted(() => ({
  sessionMock: vi.fn(), inventoryMock: vi.fn(), inventoryProductMock: vi.fn(),
}))

vi.mock('./api/generated', () => ({
  AccessApiClient: class {
    session = () => sessionMock()
    inventory = (workspaceId: string) => inventoryMock(workspaceId)
    inventoryProduct = (workspaceId: string, productId: string) => inventoryProductMock(workspaceId, productId)
  },
  AccessApiError: class extends Error { status: number; problem: { detail: string }; constructor(status: number, problem: { detail: string }) { super(problem.detail); this.status = status; this.problem = problem } },
}))

const product = {
  id: 'persisted-42', sku: 'TV-42', name: 'Samsung Frame 55', category: 'Televisions',
  onHand: 12, reserved: 2, available: 10, basePrice: 1299.5, stockStatus: 'In stock',
  platforms: [
    { platform: 'Amazon', code: 'Am', status: 'Synced', detail: 'Live · 10 available', availableShown: 10, listed: true },
    { platform: 'Euronics', code: 'Eu', status: 'Failed', detail: 'Token expired', availableShown: 8, listed: true },
  ],
}
const detail = {
  product, safetyBufferEnabled: false, lowStockAlertAt: 3, syncAlert: 'One platform needs attention.',
  pricing: [], auditLog: [], capabilities: { canAdjustOnHand: true, canChangePrice: true, canManageListings: true },
}
const session = { fullName: 'Alex Morgan', activeWorkspaceId: 'w1', workspaces: [{ id: 'w1', businessName: 'North Shop', role: 'Owner' }] }

let host: HTMLDivElement
let root: Root
;(globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true
async function flush() { await act(async () => { await Promise.resolve(); await Promise.resolve() }) }

beforeEach(async () => {
  sessionMock.mockReset().mockResolvedValue(session)
  inventoryMock.mockReset().mockResolvedValue({ totalProducts: 1, syncSummary: '1 platform needs attention', products: [product], capabilities: detail.capabilities })
  inventoryProductMock.mockReset().mockResolvedValue(detail)
  host = document.createElement('div')
  document.body.append(host)
  root = createRoot(host)
  await act(async () => root.render(createElement(Inventory)))
  await flush()
})

afterEach(async () => {
  await act(async () => root.unmount())
  host.remove()
  document.documentElement.dataset.theme = 'light'
})

describe('Inventory quick view', () => {
  it('loads persisted product details and preserves the direct detail route', async () => {
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    const directLink = host.querySelector('a[aria-label="Open Samsung Frame 55"]') as HTMLAnchorElement
    expect(directLink.getAttribute('href')).toBe('#inventory-detail?product=persisted-42')
    await act(async () => open.click())
    await flush()

    const dialog = host.querySelector('[role="dialog"]')
    expect(dialog?.getAttribute('aria-modal')).toBe('true')
    expect(dialog?.textContent).toContain('Samsung Frame 55')
    expect(dialog?.textContent).toContain('10')
    expect(dialog?.textContent).toContain('€1299.50')
    expect(dialog?.textContent).toContain('Euronics')
    expect(dialog?.textContent).toContain('Token expired')
    expect(dialog?.textContent).toContain('One platform needs attention.')
    expect(inventoryProductMock).toHaveBeenCalledWith('w1', 'persisted-42')
  })

  it('closes on Escape and restores keyboard focus to the opener', async () => {
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    await act(async () => open.click())
    await flush()
    const close = host.querySelector('.inventory-quick-view-close') as HTMLButtonElement
    expect(document.activeElement).toBe(close)
    await act(async () => close.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })))
    expect(host.querySelector('[role="dialog"]')).toBeNull()
    expect(document.activeElement).toBe(open)
  })

  it('keeps keyboard focus inside the dialog when tabbing past either end', async () => {
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    await act(async () => open.click())
    await flush()
    const close = host.querySelector('.inventory-quick-view-close') as HTMLButtonElement
    const detailLink = host.querySelector('.inventory-quick-view-detail-link') as HTMLAnchorElement
    await act(async () => detailLink.focus())
    await act(async () => detailLink.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true })))
    expect(document.activeElement).toBe(close)
    await act(async () => close.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true })))
    expect(document.activeElement).toBe(detailLink)
  })

  it('shows a retry when the detail endpoint fails', async () => {
    inventoryProductMock.mockRejectedValueOnce(new Error('offline'))
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    await act(async () => open.click())
    await flush()
    expect(host.textContent).toContain('Product details could not be loaded.')
    inventoryProductMock.mockResolvedValueOnce(detail)
    const retry = Array.from(host.querySelectorAll('button')).find(button => button.textContent === 'Try again') as HTMLButtonElement
    await act(async () => retry.click())
    await flush()
    expect(host.textContent).toContain('One platform needs attention.')
  })

  it('applies the shared dark theme to the Inventory page and quick view', async () => {
    document.documentElement.dataset.theme = 'dark'
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    await act(async () => open.click())
    await flush()
    expect(host.querySelector('.inventory-page')).not.toBeNull()
    expect(host.querySelector('.inventory-quick-view')).not.toBeNull()
    expect(document.documentElement.dataset.theme).toBe('dark')
  })

  it('does not expose base price in quick view when the API capability denies it', async () => {
    await act(async () => root.unmount())
    inventoryMock.mockResolvedValueOnce({ totalProducts: 1, syncSummary: 'Backend inventory', products: [product], capabilities: { ...detail.capabilities, canChangePrice: false } })
    inventoryProductMock.mockResolvedValueOnce({ ...detail, capabilities: { ...detail.capabilities, canChangePrice: false } })
    root = createRoot(host)
    await act(async () => root.render(createElement(Inventory, { staff: true })))
    await flush()
    const open = host.querySelector('[aria-label="Quick view Samsung Frame 55"]') as HTMLButtonElement
    await act(async () => open.click())
    await flush()
    expect(host.querySelector('[role="dialog"]')?.textContent).not.toContain('€1299.50')
  })
})

describe('Inventory authentication state', () => {
  it('does not present an unauthorized response as an empty inventory', async () => {
    sessionMock.mockReset().mockRejectedValue(new AccessApiError(401, { title: 'Unauthorized', detail: 'Sign in required' }))
    await act(async () => root.unmount())
    root = createRoot(host)
    await act(async () => root.render(createElement(Inventory)))
    await flush()
    expect(host.textContent).toContain('Sign in required')
    expect(host.textContent).toContain('cannot report whether inventory is empty')
    expect(host.textContent).not.toContain('No backend products yet')
  })
})
