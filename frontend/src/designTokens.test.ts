import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const css = readFileSync(new URL('./styles.css', import.meta.url), 'utf8')
const light = css.match(/:root(?:,\s*\[data-theme='light'\])?\s*\{([^}]+)\}/)?.[1] ?? ''
const dark = css.match(/\[data-theme='dark'\]\s*\{([^}]+)\}/)?.[1] ?? ''

function token(block: string, name: string): string {
  return block.match(new RegExp(`${name}:\\s*(#[0-9a-fA-F]{6})`))?.[1] ?? ''
}

function luminance(hex: string): number {
  const channels = hex.match(/[0-9a-f]{2}/gi)?.map(value => Number.parseInt(value, 16) / 255) ?? []
  const [r, g, b] = channels.map(value => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4)
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

function contrast(foreground: string, background: string): number {
  const a = luminance(foreground)
  const b = luminance(background)
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05)
}

describe('semantic design tokens', () => {
  it('defines matching semantic surfaces and status roles in both themes', () => {
    for (const name of ['--color-page', '--color-surface', '--color-panel', '--color-border', '--color-text', '--color-text-secondary', '--color-accent', '--color-accent-bg', '--color-sage', '--color-focus', '--color-available', '--color-healthy', '--color-warning', '--color-error']) {
      expect(token(light, name), `${name} light token`).not.toBe('')
      expect(token(dark, name), `${name} dark token`).not.toBe('')
    }
    expect(token(light, '--color-page')).not.toBe(token(dark, '--color-page'))
    expect(token(light, '--color-healthy-bg')).not.toBe(token(dark, '--color-healthy-bg'))
    expect(light).toContain('--font-display: "Caprasimo"')
    expect(light).toContain('--font-body: "Figtree Variable"')
  })

  it('keeps text and status label pairs at AA contrast in both themes', () => {
    for (const theme of [light, dark]) {
      const page = token(theme, '--color-page')
      const surface = token(theme, '--color-surface')
      expect(contrast(token(theme, '--color-text'), page)).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-text-secondary'), surface)).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-available'), token(theme, '--color-available-bg'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-healthy'), token(theme, '--color-healthy-bg'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-warning'), token(theme, '--color-warning-bg'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-error'), token(theme, '--color-error-bg'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-on-accent'), token(theme, '--color-accent-fill'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-accent-fill'), token(theme, '--color-accent-bg'))).toBeGreaterThanOrEqual(4.5)
      expect(contrast(token(theme, '--color-focus'), page)).toBeGreaterThanOrEqual(3)
    }
  })
})
