const dateLocale = 'en-GB'

function numberFormat(options: Intl.NumberFormatOptions): Intl.NumberFormat {
  return new Intl.NumberFormat(dateLocale, options)
}

export function formatCurrency(value: number, currency = 'EUR'): string {
  try {
    return numberFormat({
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value)
  } catch {
    return `${currency} ${value.toFixed(2)}`
  }
}

export function formatKpiCurrency(value: number, currency = 'EUR'): string {
  try {
    const absoluteValue = Math.abs(value)
    const suffix = absoluteValue >= 1_000_000 ? 'M' : absoluteValue >= 1_000 ? 'k' : ''
    const divisor = suffix === 'M' ? 1_000_000 : suffix ? 1_000 : 1
    const formatted = numberFormat({
      style: 'currency',
      currency,
      minimumFractionDigits: suffix ? 1 : 0,
      maximumFractionDigits: 1,
    }).format(value / divisor)

    return suffix ? `${formatted.replace(/\s?$/, '')}${suffix}` : formatted
  } catch {
    return formatCurrency(value, currency)
  }
}

export function formatQuantity(value: number): string {
  return numberFormat({ maximumFractionDigits: 0 }).format(value)
}

export function formatPercent(value: number): string {
  return numberFormat({
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(value)
}

export function formatSignedPercent(value: number): string {
  const formatted = formatPercent(Math.abs(value))
  if (value > 0) return `+${formatted}`
  if (value < 0) return `−${formatted}`
  return formatted
}

/** Formats API values expressed as percentage points (4.2 means 4.2%). */
export function formatPercentPoints(value: number): string {
  return formatPercent(value / 100)
}

export function formatSignedPercentPoints(value: number): string {
  const formatted = formatPercentPoints(Math.abs(value))
  if (value > 0) return `+${formatted}`
  if (value < 0) return `−${formatted}`
  return formatted
}

export function formatCountdown(totalSeconds: number): string {
  const seconds = Math.max(0, Math.floor(totalSeconds))
  const minutes = Math.floor(seconds / 60)
  const remainder = String(seconds % 60).padStart(2, '0')
  return seconds < 60 ? `0:${remainder}` : `${String(minutes).padStart(2, '0')}:${remainder}`
}

export function formatRecentDate(value: Date | string, now = new Date()): string {
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.valueOf())) return 'Unavailable'

  const dateDay = Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())
  const nowDay = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())
  const dayDelta = Math.round((dateDay - nowDay) / 86_400_000)

  if (dayDelta >= -6 && dayDelta <= 0) {
    if (dayDelta === 0) return 'Today'
    if (dayDelta === -1) return 'Yesterday'
    return new Intl.RelativeTimeFormat(dateLocale, { numeric: 'auto' }).format(dayDelta, 'day')
  }

  return new Intl.DateTimeFormat(dateLocale, { dateStyle: 'medium' }).format(date)
}

export function formatAbsoluteDateTime(value: Date | string): string {
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.valueOf())) return 'Unavailable'
  return new Intl.DateTimeFormat(dateLocale, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}
