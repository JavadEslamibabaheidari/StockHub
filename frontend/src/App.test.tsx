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
    for (const [hash, expected] of [['#workspace', 'Create your workspace'], ['#onboarding', 'Welcome, there'], ['#invite', 'Invite your team']] as const) {
      Object.defineProperty(globalThis, 'window', { configurable: true, value: { location: { hash }, addEventListener: () => {}, removeEventListener: () => {} } })
      expect(renderToStaticMarkup(<App />)).toContain(expected)
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
