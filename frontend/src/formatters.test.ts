import { describe, expect, it } from 'vitest'
import {
  formatAbsoluteDateTime,
  formatCountdown,
  formatCurrency,
  formatKpiCurrency,
  formatPercent,
  formatQuantity,
  formatRecentDate,
  formatSignedPercent,
} from './formatters'

describe('design system number and date formats', () => {
  it('formats currency with two decimals and keeps the requested currency', () => {
    expect(formatCurrency(1249)).toBe('€1,249.00')
    expect(formatCurrency(1249, 'GBP')).toBe('£1,249.00')
  })

  it('abbreviates KPI currency while keeping one decimal', () => {
    expect(formatKpiCurrency(48200)).toBe('€48.2k')
    expect(formatKpiCurrency(1_250_000)).toBe('€1.3M')
    expect(formatKpiCurrency(450)).toBe('€450')
  })

  it('formats grouped quantities and one-decimal percentages', () => {
    expect(formatQuantity(1240)).toBe('1,240')
    expect(formatPercent(0.042)).toBe('4.2%')
    expect(formatSignedPercent(0.042)).toBe('+4.2%')
    expect(formatSignedPercent(-0.018)).toBe('−1.8%')
    expect(formatSignedPercent(0)).toBe('0.0%')
  })

  it('keeps countdown widths stable at the minute boundary', () => {
    expect(formatCountdown(462)).toBe('07:42')
    expect(formatCountdown(60)).toBe('01:00')
    expect(formatCountdown(45)).toBe('0:45')
    expect(formatCountdown(-5)).toBe('0:00')
  })

  it('uses relative recent dates and absolute older dates', () => {
    const now = new Date('2026-10-12T12:00:00Z')
    expect(formatRecentDate('2026-10-12T08:00:00Z', now)).toBe('Today')
    expect(formatRecentDate('2026-10-11T08:00:00Z', now)).toBe('Yesterday')
    expect(formatRecentDate('2026-10-09T08:00:00Z', now)).toBe('3 days ago')
    expect(formatRecentDate('2026-10-01T08:00:00Z', now)).toBe('1 Oct 2026')
    expect(formatRecentDate('invalid', now)).toBe('Unavailable')
  })

  it('formats absolute timestamps for the active audit context', () => {
    expect(formatAbsoluteDateTime('invalid')).toBe('Unavailable')
    expect(formatAbsoluteDateTime('2026-10-12T10:30:00Z')).toContain('12 Oct 2026')
  })
})
