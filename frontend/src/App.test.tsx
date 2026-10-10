import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import App from './App'

const coverStyles = readFileSync(new URL('./styles.css', import.meta.url), 'utf8')

describe('Milestone 0 Cover', () => {
  it('renders the approved product proposition and stock model', () => {
    const markup = renderToStaticMarkup(<App />)

    expect(markup).toContain('<h1 id="hero-title">StockHub</h1>')
    expect(markup.match(/<h1\b/g)).toHaveLength(1)
    expect(markup).toContain('One stock count and one price list')
    expect(markup).toContain('The three stock numbers')
    expect(markup).toContain('>On hand</strong>')
    expect(markup).toContain('clock-icon')
    expect(markup).toContain('Reserved</strong>')
    expect(markup).toContain('>Available</strong>')
    expect(markup).toContain('Physically in the warehouse')
    expect(markup).toContain('Booked, not paid')
    expect(markup).toContain('What every platform sees')
  })

  it('communicates every stock status with text and a visible status marker', () => {
    const markup = renderToStaticMarkup(<App />)

    expect(markup).toContain('aria-label="StockHub status meanings"')
    for (const status of ['Synced · healthy', 'Low stock · expiring', 'Failed · out of stock', 'Paused · reserved · neutral']) {
      expect(markup).toContain(status)
    }
    expect(markup.match(/class="status-pill/g)).toHaveLength(4)
  })

  it('shows account actions without the roadmap panel', () => {
    const markup = renderToStaticMarkup(<App />)
    expect(markup).toContain('Create your account')
    expect(markup).toContain('Sign in')
    expect(markup).not.toContain('StockHub roadmap contents')
  })

  it('keeps responsive, focus, and reduced-motion safeguards in the Cover stylesheet', () => {
    expect(coverStyles).toContain(':focus-visible')
    expect(coverStyles).toContain('@media (prefers-reduced-motion: reduce)')
    expect(coverStyles).toContain('@media (max-width: 760px)')
    expect(coverStyles).toContain('@media (max-width: 520px)')
    expect(coverStyles).toContain('overflow-x: hidden')
  })
})

describe('Milestone 1 Access', () => {
  it('renders labelled sign-up controls and deferred legal state', () => {
    const originalWindow = globalThis.window
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#signup' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)
    expect(markup).toContain('Create your account')
    expect(markup).toContain('aria-describedby="signup-guidance"')
    expect(markup).toContain('Terms')
    expect(markup).toContain('Continue with Google')
    expect(markup).toContain('Up and running in')
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })

  it('renders the workspace and onboarding routes', () => {
    const originalWindow = globalThis.window
    for (const [hash, expected] of [['#workspace', 'Create your workspace'], ['#onboarding', 'Welcome, there'], ['#dashboard', 'Loading dashboard'], ['#invite', 'Invite your team']] as const) {
      Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash }, addEventListener: () => {}, removeEventListener: () => {} } })
      const markup = renderToStaticMarkup(<App />)
      expect(markup).toContain(expected)
      expect(markup).not.toContain('Marco Rossi')
      expect(markup).not.toContain('Luca Bianchi')
    }
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })

  it('renders password recovery and reset routes', () => {
    const originalWindow = globalThis.window
    for (const [hash, expected] of [['#forgot', 'Send reset link'], ['#reset?token=ABC', 'Update password']] as const) {
      Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash }, addEventListener: () => {}, removeEventListener: () => {} } })
      expect(renderToStaticMarkup(<App />)).toContain(expected)
    }
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })
})


describe('Milestone 2 Dashboard route', () => {
  it('routes #dashboard to the Dashboard application shell', () => {
    const originalWindow = globalThis.window
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#dashboard' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)
    expect(markup).toContain('dashboard-page')
    expect(markup).toContain('Loading dashboard')
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })
})

describe('Milestone 3 Inventory routes', () => {
  it('renders the Inventory list, empty, staff, detail, and failure states', () => {
    const originalWindow = globalThis.window
    for (const [hash, expected] of [
      ['#inventory', 'Loading backend inventory'],
      ['#inventory-empty', 'No products yet'],
      ['#inventory-staff', 'Warehouse staff'],
      ['#inventory-detail', 'Loading product detail'],
      ['#inventory-detail-failing', 'Loading product detail'],
    ] as const) {
      Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash }, addEventListener: () => {}, removeEventListener: () => {} } })
      expect(renderToStaticMarkup(<App />)).toContain(expected)
    }
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })

  it('keeps previous milestone routes distinct from Inventory routes', () => {
    const originalWindow = globalThis.window
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#dashboard' }, addEventListener: () => {}, removeEventListener: () => {} } })
    expect(renderToStaticMarkup(<App />)).toContain('dashboard-page')
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#signin' }, addEventListener: () => {}, removeEventListener: () => {} } })
    expect(renderToStaticMarkup(<App />)).toContain('Sign in')
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })
})

describe('Milestone 4 Orders routes', () => {
  it('renders the Orders list, detail, empty, no-results, return, cancel, and staff states', () => {
    const originalWindow = globalThis.window
    for (const [hash, expected] of [
      ['#orders', 'Loading orders'],
      ['#order-detail', 'Loading order detail'],
      ['#orders-empty', 'No orders yet'],
      ['#orders-no-results', 'No orders match these filters'],
      ['#order-return', 'Loading order detail'],
      ['#order-cancel', 'Loading order detail'],
      ['#orders-staff', 'Print picking list'],
    ] as const) {
      Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash }, addEventListener: () => {}, removeEventListener: () => {}, print: () => {} } })
      const markup = renderToStaticMarkup(<App />)
      expect(markup).toContain(expected)
      expect(markup).not.toContain('Marco Rossi')
      expect(markup).not.toContain('Luca Bianchi')
    }
    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
  })
})

describe('Milestone 5 Reservations route', () => {
  it('renders the Reservations screen with active and expired reservation workflows', () => {
    const originalWindow = globalThis.window
    const originalDocument = globalThis.document
    const documentElement = { dataset: {} as Record<string, string> }
    Object.defineProperty(globalThis, 'document', { configurable: true, value: { documentElement } })
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#reservations' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)

    expect(markup).toContain('Reservations')
    expect(markup).toContain('Active reservations')
    expect(markup).toContain('Release now')
    expect(markup).toContain('Expired today')
    expect(markup).toContain('Restore hold')
    expect(markup).toContain('Export CSV')
    expect(markup).toContain('Refresh timers')
    expect(markup).toContain('Search products, SKUs, orders...')
    expect(markup).toContain('3 of 4 synced · Euronics failed')
    expect(markup).toContain('Toggle appearance')
    expect(markup).toContain('Notifications')
    expect(markup).toContain('Workspace dashboard')
    expect(markup).toContain('Pro plan')
    expect(markup).toContain('Collapse')
    expect(markup).not.toContain('Marco Rossi')
    expect(markup).not.toContain('Rossi Elettronica')

    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
    Object.defineProperty(globalThis, 'document', { configurable: true, value: originalDocument })
  })

  it('keeps future milestone navigation reachable from the Reservations shell', () => {
    const originalWindow = globalThis.window
    const originalDocument = globalThis.document
    const documentElement = { dataset: {} as Record<string, string> }
    Object.defineProperty(globalThis, 'document', { configurable: true, value: { documentElement } })
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#reservations' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)

    for (const route of ['#dashboard', '#inventory', '#orders', '#reservations', '#platforms', '#pricing', '#reports', '#team', '#settings']) {
      expect(markup).toContain(`href="${route}"`)
    }

    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
    Object.defineProperty(globalThis, 'document', { configurable: true, value: originalDocument })
  })
})

describe('Milestone 6 Platforms route', () => {
  it('renders the Platforms screen with connector cards and visible page controls', () => {
    const originalWindow = globalThis.window
    const originalDocument = globalThis.document
    const documentElement = { dataset: {} as Record<string, string> }
    Object.defineProperty(globalThis, 'document', { configurable: true, value: { documentElement } })
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#platforms' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)

    expect(markup).toContain('Platforms')
    expect(markup).toContain('4 connected')
    expect(markup).toContain('Oversell protection')
    expect(markup).toContain('Amazon')
    expect(markup).toContain('Unieuro')
    expect(markup).toContain('Euronics')
    expect(markup).toContain('eBay')
    expect(markup).toContain('API token expired')
    expect(markup).toContain('Pause sync')
    expect(markup).toContain('Disconnect')
    expect(markup).toContain('Settings')
    expect(markup).toContain('Reconnect')
    expect(markup).toContain('+ Add platform')
    expect(markup).toContain('Add platform')
    expect(markup).toContain('Search products, SKUs, orders...')
    expect(markup).toContain('3 of 4 synced · Euronics failed')
    expect(markup).toContain('Toggle appearance')
    expect(markup).toContain('Notifications')
    expect(markup).toContain('Workspace dashboard')
    expect(markup).toContain('Collapse')
    expect(markup).not.toContain('Marco Rossi')
    expect(markup).not.toContain('Rossi Elettronica')

    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
    Object.defineProperty(globalThis, 'document', { configurable: true, value: originalDocument })
  })

  it('keeps previous and future milestone navigation reachable from the Platforms shell', () => {
    const originalWindow = globalThis.window
    const originalDocument = globalThis.document
    const documentElement = { dataset: {} as Record<string, string> }
    Object.defineProperty(globalThis, 'document', { configurable: true, value: { documentElement } })
    Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash: '#platforms' }, addEventListener: () => {}, removeEventListener: () => {} } })
    const markup = renderToStaticMarkup(<App />)

    for (const route of ['#dashboard', '#inventory', '#orders', '#reservations', '#platforms', '#pricing', '#reports', '#team', '#settings']) {
      expect(markup).toContain(`href="${route}"`)
    }

    Object.defineProperty(globalThis, 'window', { configurable: true, value: originalWindow })
    Object.defineProperty(globalThis, 'document', { configurable: true, value: originalDocument })
  })
})
