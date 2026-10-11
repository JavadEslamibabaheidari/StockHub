import { useEffect, useState } from 'react'
import { getAppearancePreference, previewAppearance, type AppearancePreference } from './appearance'
import { formatAbsoluteDateTime, formatCountdown, formatCurrency, formatKpiCurrency, formatPercent, formatQuantity, formatRecentDate, formatSignedPercent } from './formatters'

type PreviewTheme = 'preference' | 'light' | 'dark'

const tokenRows = [
  ['Page', '--color-page'], ['Surface', '--color-surface'], ['Panel', '--color-panel'],
  ['Border', '--color-border'], ['Text', '--color-text'], ['Secondary text', '--color-text-secondary'],
  ['Accent', '--color-accent'], ['Available', '--color-available'],
  ['Healthy', '--color-healthy'], ['Warning', '--color-warning'], ['Error', '--color-error'],
] as const

function ThemePalette({ theme, title }: { theme: 'light' | 'dark'; title: string }) {
  return <section className="ds-theme-sample" data-theme={theme} aria-label={`${title} design tokens`}>
    <h3>{title}</h3>
    <div className="ds-token-grid">
      {tokenRows.map(([label, variable]) => <div className="ds-token" key={variable}>
        <span className="ds-token-swatch" style={{ backgroundColor: `var(${variable})` }} aria-hidden="true" />
        <span><strong>{label}</strong><code>{variable}</code></span>
      </div>)}
    </div>
    <div className="ds-status-row" aria-label={`${title} status examples`}>
      <span className="ds-status healthy"><i aria-hidden="true" />Synced · healthy</span>
      <span className="ds-status warning"><i aria-hidden="true" />Low stock · expiring</span>
      <span className="ds-status error"><i aria-hidden="true" />Failed · out of stock</span>
    </div>
  </section>
}

export function DesignSystemScreen() {
  const [preview, setPreview] = useState<PreviewTheme>('preference')
  const preference: AppearancePreference = getAppearancePreference()
  const previewTheme = preview === 'preference' ? null : preview

  useEffect(() => {
    previewAppearance(previewTheme)
    return () => previewAppearance(null)
  }, [previewTheme])

  const samples = [
    ['Currency', formatCurrency(1249)],
    ['KPI currency', formatKpiCurrency(48_200)],
    ['Large KPI', formatKpiCurrency(1_250_000)],
    ['Quantity', formatQuantity(1240)],
    ['Percentage', formatPercent(0.042)],
    ['Signed change', `${formatSignedPercent(0.042)} · ${formatSignedPercent(-0.018)}`],
    ['Countdown', `${formatCountdown(462)} · ${formatCountdown(45)}`],
    ['Recent date', formatRecentDate(new Date())],
    ['Absolute date', formatAbsoluteDateTime(new Date('2026-10-12T10:30:00Z'))],
  ] as const

  return <main className="design-system-page">
    <header className="ds-header">
      <a className="ds-back-link" href="#cover">← Back to StockHub</a>
      <span className="ds-overline">StockHub · Foundations</span>
      <h1 className="ds-type-title">Design system</h1>
      <p>Organic foundations, StockHub status tokens, and number formats for every workspace screen.</p>
      <div className="ds-theme-controls" role="group" aria-label="Theme preview">
        <button type="button" aria-pressed={preview === 'preference'} onClick={() => setPreview('preference')}>Use my preference</button>
        <button type="button" aria-pressed={preview === 'light'} onClick={() => setPreview('light')}>Preview light</button>
        <button type="button" aria-pressed={preview === 'dark'} onClick={() => setPreview('dark')}>Preview dark</button>
      </div>
      <p className="ds-theme-note" role="status">Saved appearance: {preference}. Preview does not change your saved preference.</p>
    </header>

    <section className="ds-section" aria-labelledby="ds-colors-title">
      <div className="ds-section-heading"><span className="ds-overline">01 · Colour</span><h2 id="ds-colors-title" className="ds-type-heading">Semantic color tokens</h2><p>Light and dark share token names; status meaning always has a label.</p></div>
      <div className="ds-theme-grid"><ThemePalette theme="light" title="Light theme" /><ThemePalette theme="dark" title="Dark theme" /></div>
    </section>

    <section className="ds-section" aria-labelledby="ds-type-title">
      <div className="ds-section-heading"><span className="ds-overline">02 · Type</span><h2 id="ds-type-title" className="ds-type-heading">Typography roles</h2><p>Caprasimo for display moments; Figtree for clear, compact application data.</p></div>
      <div className="ds-type-grid">
        <div><span className="ds-type-label">Display · Caprasimo · 36</span><p className="ds-type-display">Stock that sells</p></div>
        <div><span className="ds-type-label">Title · 700 · 30</span><p className="ds-type-title">One title per screen</p></div>
        <div><span className="ds-type-label">Heading · 700 · 20</span><p className="ds-type-heading">Panel heading</p></div>
        <div><span className="ds-type-label">KPI · 700 · 30</span><p className="ds-type-kpi numeric">€48.2k</p></div>
        <div><span className="ds-type-label">Panel title · 700 · 15</span><p className="ds-type-panel-title">Live reservations</p></div>
        <div><span className="ds-type-label">Body · 400 · 14</span><p className="ds-type-body">Available stock updates with every confirmed order.</p></div>
        <div><span className="ds-type-label">Secondary · 13</span><p className="ds-type-secondary">Updated 3 minutes ago</p></div>
        <div><span className="ds-type-label">Caption and badge · 12</span><p><span className="ds-type-caption">Last sync</span> <span className="ds-type-badge">Healthy</span></p></div>
        <div><span className="ds-type-label">Overline · 700 · 11</span><p className="ds-type-overline">Inventory status</p></div>
      </div>
    </section>

    <section className="ds-section" aria-labelledby="ds-number-title">
      <div className="ds-section-heading"><span className="ds-overline">03 · Numbers</span><h2 id="ds-number-title" className="ds-type-heading">Strict en-GB formats</h2><p>Prices, quantities, changes and time stay consistent across screen families.</p></div>
      <dl className="ds-format-list">{samples.map(([label, value]) => <div key={label}><dt>{label}</dt><dd className="numeric">{value}</dd></div>)}</dl>
    </section>

    <section className="ds-section" aria-labelledby="ds-components-title">
      <div className="ds-section-heading"><span className="ds-overline">04 · Components</span><h2 id="ds-components-title" className="ds-type-heading">Shared controls and states</h2></div>
      <div className="ds-component-grid">
        <article className="ds-component-card"><h3 className="ds-type-panel-title">Buttons</h3><div className="ds-button-row"><button className="ds-primary-button" type="button">Save changes</button><button className="ds-secondary-button" type="button">Cancel</button><button className="ds-secondary-button" type="button" disabled>Unavailable</button></div></article>
        <article className="ds-component-card"><h3 className="ds-type-panel-title">Platform sync</h3><div className="ds-platform-state"><span aria-hidden="true">▣</span><span><strong>Amazon</strong><small>Last sync 3 minutes ago</small></span><span className="ds-status healthy"><i aria-hidden="true" />Synced</span></div></article>
        <article className="ds-component-card"><h3 className="ds-type-panel-title">Banners</h3><p className="ds-banner warning"><strong>Attention:</strong> 3 products are below their safety stock.</p><p className="ds-banner success"><strong>Synced:</strong> Your stock is up to date.</p></article>
        <article className="ds-component-card"><h3 className="ds-type-panel-title">KPI card</h3><div className="ds-kpi-card"><span className="ds-type-secondary">Sales this month</span><strong className="ds-type-kpi numeric">€48.2k</strong><span className="ds-status healthy"><i aria-hidden="true" />+4.2% this week</span></div></article>
        <article className="ds-component-card"><h3 className="ds-type-panel-title">Search</h3><label className="ds-search-label" htmlFor="ds-search">Find products</label><input className="ds-search-input" id="ds-search" type="search" placeholder="Search products, SKUs, orders" /></article>
        <article className="ds-component-card"><h3 className="ds-type-panel-title">Theme control</h3><p className="ds-type-body">Light, Dark, and System remain browser preferences, separate from this temporary preview.</p><a className="ds-secondary-button ds-inline-link" href="#settings">Manage appearance</a></article>
      </div>
    </section>

    <footer className="ds-footer"><a href="#cover">Return to the StockHub cover</a><a href="#settings">Appearance settings</a></footer>
  </main>
}
