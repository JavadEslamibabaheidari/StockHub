// @vitest-environment jsdom
import { act, createElement } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Onboarding } from './AccessScreens'

vi.mock('./api/generated', () => ({
  AccessApiClient: class {
    session = async () => ({ fullName: 'Alex Morgan', email: 'alex@example.test', activeWorkspaceId: 'w1', workspaces: [{ id: 'w1', businessName: 'North Shop', country: 'Italy', currency: 'EUR', role: 'Owner' }] })
    products = async () => []
    onboarding = async () => undefined
    setActiveWorkspace = async () => undefined
  },
  AccessApiError: class extends Error {},
}))

let host: HTMLDivElement
let root: Root

beforeEach(async () => {
  localStorage.clear()
  document.documentElement.dataset.theme = 'light'
  host = document.createElement('div')
  document.body.append(host)
  root = createRoot(host)
  await act(async () => { root.render(createElement(Onboarding)) })
  await act(async () => { await Promise.resolve(); await Promise.resolve() })
})

afterEach(async () => {
  await act(async () => root.unmount())
  host.remove()
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
})

describe('Onboarding appearance toggle', () => {
  it('updates the shared document theme and persists the browser preference', async () => {
    const toggle = host.querySelector('.icon-button')
    expect(toggle).not.toBeNull()
    await act(async () => { toggle?.dispatchEvent(new MouseEvent('click', { bubbles: true })) })

    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(localStorage.getItem('stockhub-appearance')).toBe('dark')
    expect(localStorage.getItem('stockhub-theme')).toBe('dark')
  })
})
