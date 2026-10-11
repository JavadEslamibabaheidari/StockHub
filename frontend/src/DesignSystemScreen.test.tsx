// @vitest-environment jsdom
import { act } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getAppearancePreference, initializeAppearance } from './appearance'
import { DesignSystemScreen } from './DesignSystemScreen'

let root: Root | undefined
let container: HTMLElement
const originalMatchMedia = window.matchMedia

beforeEach(() => {
  document.body.replaceChildren()
  localStorage.clear()
  localStorage.setItem('stockhub-appearance', 'system')
  Object.defineProperty(window, 'matchMedia', { configurable: true, value: () => ({
    matches: true,
    media: '(prefers-color-scheme: dark)',
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  }) })
  initializeAppearance()
  container = document.createElement('div')
  document.body.append(container)
  root = createRoot(container)
})

afterEach(() => {
  if (root) act(() => root?.unmount())
  root = undefined
  document.body.replaceChildren()
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
  document.documentElement.removeAttribute('data-appearance')
  Object.defineProperty(window, 'matchMedia', { configurable: true, value: originalMatchMedia })
})

describe('Design system reference screen', () => {
  it('previews dark or light without changing the saved appearance and restores System on exit', () => {
    act(() => root?.render(<DesignSystemScreen />))

    expect(document.documentElement.dataset.theme).toBe('dark')
    const previewLight = [...container.querySelectorAll('button')].find(button => button.textContent === 'Preview light')!
    previewLight.focus()
    expect(document.activeElement).toBe(previewLight)
    act(() => previewLight.click())
    expect(document.documentElement.dataset.theme).toBe('light')
    expect(document.documentElement.dataset.appearance).toBe('system')
    expect(getAppearancePreference()).toBe('system')
    expect(localStorage.getItem('stockhub-appearance')).toBe('system')

    act(() => root?.unmount())
    root = undefined
    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(document.documentElement.dataset.appearance).toBe('system')
  })
})
