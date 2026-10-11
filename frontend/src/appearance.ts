export type AppearancePreference = 'light' | 'dark' | 'system'

const preferenceKey = 'stockhub-appearance'
const legacyKey = 'stockhub-theme'
let preference: AppearancePreference = 'system'
let mediaQuery: MediaQueryList | null = null

function browserPreference(): AppearancePreference {
  try {
    const stored = localStorage.getItem(preferenceKey) ?? localStorage.getItem(legacyKey)
    if (stored === 'light' || stored === 'dark' || stored === 'system') return stored
  } catch {
    // Storage can be disabled; the in-memory preference still works this session.
  }
  return 'system'
}

function systemTheme(): 'light' | 'dark' {
  return mediaQuery?.matches ? 'dark' : 'light'
}

function applyPreference(): void {
  if (typeof document === 'undefined') return
  document.documentElement.dataset.appearance = preference
  document.documentElement.dataset.theme = preference === 'system' ? systemTheme() : preference
}

export function getAppearancePreference(): AppearancePreference {
  return preference
}

export function getResolvedTheme(): 'light' | 'dark' {
  return preference === 'system' ? systemTheme() : preference
}

export function setAppearancePreference(next: AppearancePreference): void {
  preference = next
  try {
    localStorage.setItem(preferenceKey, next)
    if (next === 'system') localStorage.removeItem(legacyKey)
    else localStorage.setItem(legacyKey, next)
  } catch {
    // Keep the selected value for the current page session.
  }
  applyPreference()
}

export function toggleExplicitTheme(): void {
  setAppearancePreference(getResolvedTheme() === 'dark' ? 'light' : 'dark')
}

export function initializeAppearance(): void {
  if (typeof window === 'undefined') return
  preference = browserPreference()
  if (typeof window.matchMedia === 'function') {
    mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    mediaQuery.addEventListener('change', () => {
      if (preference === 'system') applyPreference()
    })
  }
  window.addEventListener('storage', event => {
    if (event.key !== preferenceKey && event.key !== legacyKey) return
    preference = browserPreference()
    applyPreference()
  })
  applyPreference()
}
