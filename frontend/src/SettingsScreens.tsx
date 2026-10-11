import { useEffect, useState } from 'react'
import { AccessApiClient, AccessApiError, type SessionResponse, type WorkspaceSummary } from './api/generated'
import { getAppearancePreference, setAppearancePreference, type AppearancePreference } from './appearance'

const api = new AccessApiClient()
type SettingsTab = 'billing' | 'appearance'
const nav = [
  ['Dashboard', '#dashboard'], ['Inventory', '#inventory'], ['Orders', '#orders'],
  ['Reservations', '#reservations'], ['Platforms', '#platforms'], ['Pricing rules', '#pricing'],
  ['Reports', '#reports'], ['Team', '#team'], ['Settings', '#settings'],
] as const
const roleName: Record<WorkspaceSummary['role'], string> = {
  Owner: 'Owner', Admin: 'Admin', Manager: 'Manager', WarehouseStaff: 'Warehouse staff', Viewer: 'Viewer',
}
const errorMessage = (error: unknown) => error instanceof AccessApiError
  ? error.problem.detail || 'Settings could not be loaded.'
  : 'Settings could not be loaded. Check your connection and try again.'

export function Settings() {
  const [tab, setTab] = useState<SettingsTab>('billing')
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reload, setReload] = useState(0)
  const [workspaceMenu, setWorkspaceMenu] = useState(false)
  const [switching, setSwitching] = useState(false)
  const [preference, setPreference] = useState<AppearancePreference>(getAppearancePreference)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(null)
    api.session().then(data => {
      if (!active) return
      setSession(data)
      const current = data.workspaces.find(item => item.id === data.activeWorkspaceId) ?? data.workspaces[0] ?? null
      if (!current) {
        window.location.hash = '#workspace'
        return
      }
      setWorkspace(current)
    }).catch(caught => {
      if (!active) return
      if (caught instanceof AccessApiError && caught.status === 401) window.location.hash = '#signin'
      else setError(errorMessage(caught))
    }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [reload])

  async function switchWorkspace(next: WorkspaceSummary) {
    if (switching || next.id === workspace?.id) return
    setSwitching(true)
    setError(null)
    try {
      await api.setActiveWorkspace(next.id)
      setWorkspace(next)
      setWorkspaceMenu(false)
    } catch (caught) {
      setError(errorMessage(caught))
    } finally {
      setSwitching(false)
    }
  }

  return <main className="inventory-page settings-page"><div className="inventory-shell">
    <aside className="inventory-sidebar">
      <div className="settings-workspace-wrap">
        <button className="inventory-workspace reservation-workspace-button" type="button" aria-expanded={workspaceMenu} onClick={() => setWorkspaceMenu(open => !open)}>
          <b>{workspace?.businessName.trim().slice(0, 2).toUpperCase() || 'SH'}</b>
          <span><strong>{workspace?.businessName || 'Your workspace'}</strong><small>{roleName[workspace?.role || 'Owner']} · {session?.workspaces.length || 1} workspace{session?.workspaces.length === 1 ? '' : 's'}</small></span>
          <span aria-hidden="true">⌄</span>
        </button>
        {workspaceMenu && <div className="dashboard-popover workspace-menu">{session?.workspaces.map(item => <button key={item.id} type="button" disabled={switching} aria-current={item.id === workspace?.id ? 'true' : undefined} onClick={() => void switchWorkspace(item)}>{item.businessName}<small>{roleName[item.role]}</small></button>)}<a href="#workspace">Create workspace</a></div>}
      </div>
      <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={href} className={label === 'Settings' ? 'active' : ''} aria-current={label === 'Settings' ? 'page' : undefined} href={href}>{label}</a>)}</nav>
      <div className="inventory-plan"><strong>Settings</strong><span /><small>Billing and appearance</small></div>
    </aside>
    <section className="inventory-main">
      <header className="inventory-top settings-top"><span className="settings-crumb">Workspace settings</span><div className="settings-identity">{session?.fullName && <><b aria-hidden="true">{session.fullName.trim().split(/\s+/).slice(0, 2).map(part => part[0]?.toUpperCase()).join('')}</b><span>{session.fullName}<small>{roleName[workspace?.role || 'Owner']}</small></span></>}</div></header>
      <section className="settings-content" aria-labelledby="settings-title">
        <header className="inventory-heading settings-heading"><div><span className="inventory-eyebrow">WORKSPACE</span><h1 id="settings-title">Settings</h1><p>Manage billing details and how StockHub looks in this browser.</p></div></header>
        <div className="settings-tabs" role="group" aria-label="Settings sections">
          <button type="button" aria-pressed={tab === 'billing'} onClick={() => setTab('billing')}>Billing</button>
          <button type="button" aria-pressed={tab === 'appearance'} onClick={() => setTab('appearance')}>Appearance</button>
        </div>
        {loading && <div className="inventory-card settings-state" role="status">Loading workspace settings…</div>}
        {!loading && error && <div className="inventory-card settings-state settings-error" role="alert"><strong>Settings are unavailable</strong><p>{error}</p><button type="button" onClick={() => setReload(key => key + 1)}>Try again</button></div>}
        {!loading && !error && workspace && <div id="settings-panel">
          {tab === 'billing' ? <BillingSettings workspace={workspace} accountEmail={session?.email || null} /> : <AppearanceSettings preference={preference} onChange={next => { setAppearancePreference(next); setPreference(next) }} />}
        </div>}
      </section>
      <nav className="settings-mobile-nav" aria-label="Main navigation">{nav.map(([label, href]) => <a key={href} className={label === 'Settings' ? 'active' : ''} aria-current={label === 'Settings' ? 'page' : undefined} href={href}>{label}</a>)}</nav>
    </section>
  </div></main>
}

function BillingSettings({ workspace, accountEmail }: { workspace: WorkspaceSummary; accountEmail: string | null }) {
  return <div className="settings-billing">
    <div className="settings-notice" role="status"><span aria-hidden="true">ⓘ</span><p>StockHub does not currently provide subscription management or payment processing. No plan, payment method, or billing action is available here.</p></div>
    <section className="settings-card" aria-labelledby="current-plan-title">
      <div className="settings-card-heading"><div><h2 id="current-plan-title">Current plan</h2><p>Plan and usage information is unavailable until billing is connected.</p></div><span className="settings-unavailable">Unavailable</span></div>
      <div className="settings-metrics"><BillingMetric label="Plan" /><BillingMetric label="Renewal" /><BillingMetric label="Products" /><BillingMetric label="Platforms" /><BillingMetric label="Team members" /></div>
    </section>
    <section className="settings-card" aria-labelledby="payment-title">
      <div className="settings-card-heading"><div><h2 id="payment-title">Payment method</h2><p>Payment method details are not available in StockHub.</p></div></div>
      <p className="settings-empty">Payment methods, invoices, and plan changes are not available in this workspace.</p>
    </section>
    <section className="settings-card" aria-labelledby="billing-profile-title">
      <div className="settings-card-heading"><div><h2 id="billing-profile-title">Billing profile</h2><p>Workspace and account details currently available to StockHub.</p></div></div>
      <dl className="settings-profile-grid">
        <div><dt>Company</dt><dd>{workspace.businessName}</dd></div>
        <div><dt>Country</dt><dd>{workspace.country}</dd></div>
        <div><dt>Currency</dt><dd>{workspace.currency}</dd></div>
        <div><dt>Account email</dt><dd>{accountEmail || 'Unavailable'}</dd></div>
        <div><dt>VAT number</dt><dd>Unavailable in the active-workspace session</dd></div>
      </dl>
      <p className="settings-footnote">Account email is shown for identification only; it is not represented as a separate billing contact. Billing profile editing is not connected.</p>
    </section>
    <section className="settings-card" aria-labelledby="plan-comparison-title">
      <div className="settings-card-heading"><div><h2 id="plan-comparison-title">Compare plans</h2><p>Plan prices and included limits are not configured in the application.</p></div></div>
      <p className="settings-empty">Plan comparison is unavailable. No plan prices or limits are shown from mockup sample data.</p>
    </section>
  </div>
}

function BillingMetric({ label }: { label: string }) {
  return <div className="settings-metric"><span>{label}</span><strong>Unavailable</strong></div>
}

function AppearanceSettings({ preference, onChange }: { preference: AppearancePreference; onChange: (preference: AppearancePreference) => void }) {
  const choices: { value: AppearancePreference; title: string; detail: string; icon: string }[] = [
    { value: 'light', title: 'Light', detail: 'Use the light theme', icon: '☀' },
    { value: 'dark', title: 'Dark', detail: 'Use the dark theme', icon: '☾' },
    { value: 'system', title: 'System', detail: 'Follow this device’s appearance setting', icon: '◐' },
  ]
  return <section className="settings-card" aria-labelledby="theme-title">
    <div className="settings-card-heading"><div><h2 id="theme-title">Theme</h2><p>Your choice is saved in this browser and applies across StockHub.</p></div></div>
    <div className="settings-theme-options" role="group" aria-labelledby="theme-title">{choices.map(choice => <button key={choice.value} type="button" aria-pressed={preference === choice.value} className={preference === choice.value ? 'selected' : ''} onClick={() => onChange(choice.value)}><span className="settings-theme-icon" aria-hidden="true">{choice.icon}</span><strong>{choice.title}</strong><small>{choice.detail}</small></button>)}</div>
    <p className="settings-footnote">Status colors are paired with labels or icons so meaning remains clear in both themes.</p>
  </section>
}
