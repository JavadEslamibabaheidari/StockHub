// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { getAppearancePreference, getResolvedTheme, initializeAppearance, setAppearancePreference } from './appearance'

function setSystemPreference(dark: boolean) {
  const listeners = new Set<(event: MediaQueryListEvent) => void>()
  const query = {
    matches: dark,
    media: '(prefers-color-scheme: dark)',
    onchange: null,
    addEventListener: (_: string, listener: (event: MediaQueryListEvent) => void) => listeners.add(listener),
    removeEventListener: (_: string, listener: (event: MediaQueryListEvent) => void) => listeners.delete(listener),
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => true,
  } as unknown as MediaQueryList
  vi.stubGlobal('matchMedia', vi.fn(() => query))
  return (next: boolean) => {
    Object.defineProperty(query, 'matches', { configurable: true, value: next })
    for (const listener of listeners) listener({ matches: next, media: query.media } as MediaQueryListEvent)
  }
}

afterEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
  document.documentElement.removeAttribute('data-appearance')
  vi.unstubAllGlobals()
})

describe('appearance preference', () => {
  it('defaults to System and follows OS preference changes', () => {
    const changeSystem = setSystemPreference(false)
    initializeAppearance()

    expect(getAppearancePreference()).toBe('system')
    expect(document.documentElement.dataset.theme).toBe('light')
    changeSystem(true)
    expect(getResolvedTheme()).toBe('dark')
    expect(document.documentElement.dataset.theme).toBe('dark')
  })

  it('persists explicit selection and applies it over the system preference', () => {
    setSystemPreference(true)
    initializeAppearance()
    setAppearancePreference('light')

    expect(localStorage.getItem('stockhub-appearance')).toBe('light')
    expect(localStorage.getItem('stockhub-theme')).toBe('light')
    expect(document.documentElement.dataset.theme).toBe('light')
    expect(getResolvedTheme()).toBe('light')
  })

  it('loads a stored preference and migrates the legacy value', () => {
    localStorage.setItem('stockhub-theme', 'dark')
    setSystemPreference(false)
    initializeAppearance()

    expect(getAppearancePreference()).toBe('dark')
    expect(document.documentElement.dataset.appearance).toBe('dark')
    expect(document.documentElement.dataset.theme).toBe('dark')
  })
})
