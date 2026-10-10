import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { AccessApiClient, AccessApiError, type WorkspaceSummary } from './api/generated'

type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type ShellIdentity = { workspaceName: string; workspaceInitials: string; workspaceCount: number; fullName: string; userInitials: string; role: string }
type PlatformStatus = 'Connected' | 'Paused' | 'Error' | 'Disconnected'
type Platform = {
  id: string
  name: string
  code: string
  seller: string
  status: PlatformStatus
  lastSync: string
  listedProducts: number
  ordersThisWeek: number
  errorTitle?: string
  errorBody?: string
}
type Marketplace = { id: string; name: string; code: string; seller: string }

const api = new AccessApiClient()
const nav = [
  ['Dashboard', '#dashboard'],
  ['Inventory', '#inventory'],
  ['Orders', '#orders'],
  ['Reservations', '#reservations'],
  ['Platforms', '#platforms'],
  ['Pricing rules', '#pricing'],
  ['Reports', '#reports'],
  ['Team', '#team'],
  ['Settings', '#settings'],
] as const

const initialPlatforms: Platform[] = [
  platform('amazon', 'Amazon', 'Am', 'Seller Central · amazon.it', 'Connected', '2139s ago', 1182, 113),
  platform('unieuro', 'Unieuro', 'Un', 'Marketplace · unieuro.it', 'Connected', '2142s ago', 1040, 51),
  platform('euronics', 'Euronics', 'Eu', 'Marketplace · euronics.it', 'Error', '12 min ago', 978, 33, 'API token expired', 'Stock and prices stopped syncing 12 min ago. Euronics may show outdated stock for 9 products. Listings pause automatically in 3 min.'),
  platform('ebay', 'eBay', 'eB', 'Seller Hub · ebay.it', 'Connected', '2146s ago', 640, 22),
]
const marketplaces: Marketplace[] = [
  { id: 'zalando', name: 'Zalando', code: 'Za', seller: 'Partner Program · zalando.it' },
  { id: 'eprice', name: 'ePRICE', code: 'eP', seller: 'Marketplace · eprice.it' },
  { id: 'mediaworld', name: 'MediaWorld', code: 'MW', seller: 'Marketplace · mediaworld.it' },
]

function platform(id: string, name: string, code: string, seller: string, status: PlatformStatus, lastSync: string, listedProducts: number, ordersThisWeek: number, errorTitle?: string, errorBody?: string): Platform {
  return { id, name, code, seller, status, lastSync, listedProducts, ordersThisWeek, errorTitle, errorBody }
}

function initials(value: string, fallback = 'SH') {
  const parts = value.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return fallback
  return parts.slice(0, 2).map(part => part[0]?.toUpperCase()).join('') || fallback
}

function formatRole(role?: WorkspaceSummary['role'] | string | null) {
  return role === 'WarehouseStaff' ? 'Warehouse staff' : role || 'Owner'
}

function statusClass(status: PlatformStatus) {
  return status.toLowerCase()
}

function Shell({
  children,
  panel,
  setPanel,
  search,
  setSearch,
  collapsed,
  setCollapsed,
}: {
  children: ReactNode
  panel: Panel
  setPanel: (panel: Panel) => void
  search: string
  setSearch: (value: string) => void
  collapsed: boolean
  setCollapsed: (value: boolean) => void
}) {
  const [accountOpen, setAccountOpen] = useState(false)
  const [identity, setIdentity] = useState<ShellIdentity>({
    workspaceName: 'Your workspace',
    workspaceInitials: 'SH',
    workspaceCount: 1,
    fullName: 'Your account',
    userInitials: 'SH',
    role: 'Owner',
  })
  useEffect(() => {
    let alive = true
    api.session()
      .then(session => {
        if (!alive) return
        const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
        setIdentity({
          workspaceName: workspace?.businessName || 'Your workspace',
          workspaceInitials: initials(workspace?.businessName || ''),
          workspaceCount: Math.max(session.workspaces.length, 1),
          fullName: session.fullName || 'Your account',
          userInitials: initials(session.fullName || ''),
          role: formatRole(workspace?.role),
        })
      })
      .catch(error => { if (!(error instanceof AccessApiError && error.status === 401)) console.warn(error) })
    return () => { alive = false }
  }, [])

  function showPanel(next: NonNullable<Panel>) {
    setAccountOpen(false)
    setPanel(next)
  }

  return (
    <main className={`inventory-page reservations-page platforms-page ${collapsed ? 'reservations-collapsed' : ''}`}>
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <button className="inventory-workspace reservation-workspace-button" type="button" onClick={() => showPanel({ title: 'Workspace switcher', body: `${identity.workspaceName} is active. Switching workspaces belongs to the Team milestone.` })} aria-label="Workspace dashboard">
            <b>{identity.workspaceInitials}</b><span><strong>{identity.workspaceName}</strong><small>{identity.role} · {identity.workspaceCount} workspace{identity.workspaceCount === 1 ? '' : 's'}</small></span><span>⌄</span>
          </button>
          <nav aria-label="Main navigation">
            {nav.map(([label, href]) => <a key={label} className={label === 'Platforms' ? 'active' : ''} href={href}>{label}{label === 'Reservations' && <b>6</b>}{label === 'Platforms' && <i />}</a>)}
          </nav>
          <div className="inventory-plan"><strong>Pro plan</strong><a href="#settings" onClick={() => showPanel({ title: 'Billing handoff', body: 'Plan upgrades are handled in Settings Billing, milestone 10.' })}>Upgrade</a><span /><small>1,240 / 2,000 products</small></div>
          <button className="sidebar-collapse-button" type="button" onClick={() => { setAccountOpen(false); setCollapsed(!collapsed) }}>{collapsed ? 'Expand' : 'Collapse'}</button>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className="sync-alert" type="button" onClick={() => showPanel({ title: 'Sync status', body: '3 of 4 platforms synced. Euronics has an expired token and will pause listings automatically if reconnect is not completed.', tone: 'warn' })}>3 of 4 synced · Euronics failed</button>
              <button type="button" aria-label="Toggle appearance" onClick={() => document.documentElement.dataset.theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => showPanel({ title: 'Notifications', body: '5 platform alerts: 1 expired token, 3 paused listing warnings, and 1 sync delay.' })}>5</button>
              <button className="reservation-account" type="button" onClick={() => setAccountOpen(!accountOpen)} aria-expanded={accountOpen}><b>{identity.userInitials}</b><span>{identity.fullName}<small>{identity.role}</small></span><span>⌄</span></button>
            </div>
          </header>
          {accountOpen && <section className="dashboard-popover account-menu reservation-account-menu"><button type="button" onClick={() => showPanel({ title: 'Profile', body: `${identity.fullName} · ${identity.role} · ${identity.workspaceName}` })}>View profile</button><a href="#settings" onClick={() => setAccountOpen(false)}>Account settings</a><button type="button" onClick={() => showPanel({ title: 'Signed out preview', body: 'This preview keeps you in the app so Milestone 6 can be reviewed.' })}>Sign out</button></section>}
          {children}
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>×</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function OversellProtection({ enabled, setEnabled, setPanel }: { enabled: boolean; setEnabled: (enabled: boolean) => void; setPanel: (panel: Panel) => void }) {
  return (
    <section className="oversell-banner">
      <span aria-hidden="true">◊</span>
      <div><strong>Oversell protection</strong><small>{enabled ? "Pause a platform's listings automatically if it can't sync for 15 minutes. They resume when sync is back." : 'Disabled for this preview. Platforms will keep their last known listings until you pause them manually.'}</small></div>
      <button type="button" role="switch" aria-checked={enabled} onClick={() => { setEnabled(!enabled); setPanel({ title: 'Oversell protection', body: !enabled ? 'Automatic listing pause is on for delayed platform sync.' : 'Automatic listing pause is off for this preview.', tone: !enabled ? 'ok' : 'warn' }) }}><span /></button>
    </section>
  )
}

function PlatformCard({
  item,
  pause,
  disconnect,
  reconnect,
  settings,
}: {
  item: Platform
  pause: (item: Platform) => void
  disconnect: (item: Platform) => void
  reconnect: (item: Platform) => void
  settings: (item: Platform) => void
}) {
  const errored = item.status === 'Error'
  return (
    <article className={`platform-card ${errored ? 'error' : ''}`}>
      <header>
        <span className="platform-avatar">{item.code}</span>
        <div><h2>{item.name}</h2><small>{item.seller}</small></div>
        <b className={`platform-status ${statusClass(item.status)}`}>{item.status}</b>
      </header>
      {errored && <div className="platform-error"><strong>{item.errorTitle}</strong><p>{item.errorBody}</p><button type="button" onClick={() => reconnect(item)}>Reconnect</button></div>}
      <div className="platform-stats">
        <span><small>Last sync</small><b>{item.lastSync}</b></span>
        <span><small>Listed products</small><b>{item.listedProducts.toLocaleString()}</b></span>
        <span><small>Orders this week</small><b>{item.ordersThisWeek}</b></span>
      </div>
      {!errored && <footer><button type="button" onClick={() => pause(item)}>{item.status === 'Paused' ? 'Resume sync' : 'Pause sync'}</button><button type="button" onClick={() => disconnect(item)}>Disconnect</button><button type="button" onClick={() => settings(item)}>Settings</button></footer>}
    </article>
  )
}

function AddPlatformModal({
  connectedIds,
  connect,
  close,
}: {
  connectedIds: Set<string>
  connect: (marketplace: Marketplace) => void
  close: () => void
}) {
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') close()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [close])

  return (
    <div className="platform-modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) close() }}>
      <section className="platform-modal" role="dialog" aria-modal="true" aria-labelledby="add-platform-title">
        <button className="platform-modal-close" type="button" aria-label="Close add platform" onClick={close}>×</button>
        <h2 id="add-platform-title">Add a platform</h2>
        <p>Choose a marketplace. You'll sign in to it to authorise StockHub, then pick which products to list.</p>
        <div className="marketplace-list">
          {marketplaces.map(item => {
            const connected = connectedIds.has(item.id)
            return (
              <div className="marketplace-row" key={item.id}>
                <span>{item.code}</span><strong>{item.name}</strong><button type="button" disabled={connected} onClick={() => connect(item)}>{connected ? 'Connected' : 'Connect'}</button>
              </div>
            )
          })}
        </div>
        <button className="platform-modal-done" type="button" onClick={close}>Done</button>
      </section>
    </div>
  )
}

export function Platforms() {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  const [collapsed, setCollapsed] = useState(false)
  const [oversell, setOversell] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [rows, setRows] = useState(initialPlatforms)
  const connectedIds = useMemo(() => new Set(rows.map(item => item.id)), [rows])
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return rows.filter(item => !q || [item.name, item.seller, item.status, item.errorTitle || ''].some(value => value.toLowerCase().includes(q)))
  }, [rows, search])

  function pause(item: Platform) {
    const nextStatus: PlatformStatus = item.status === 'Paused' ? 'Connected' : 'Paused'
    setRows(rows.map(row => row.id === item.id ? { ...row, status: nextStatus } : row))
    setPanel({ title: nextStatus === 'Paused' ? 'Sync paused' : 'Sync resumed', body: `${item.name} ${nextStatus === 'Paused' ? 'will stop stock and price updates until you resume it.' : 'is back in the connected preview state.'}`, tone: nextStatus === 'Paused' ? 'warn' : 'ok' })
  }

  function disconnect(item: Platform) {
    setRows(rows.map(row => row.id === item.id ? { ...row, status: 'Disconnected', lastSync: 'Disconnected', listedProducts: 0, ordersThisWeek: 0 } : row))
    setPanel({ title: 'Platform disconnected', body: `${item.name} was disconnected in this preview. Reconnect it from the Add platform picker when credential storage exists.`, tone: 'warn' })
  }

  function reconnect(item: Platform) {
    setRows(rows.map(row => row.id === item.id ? { ...row, status: 'Connected', lastSync: 'just now', errorTitle: undefined, errorBody: undefined } : row))
    setPanel({ title: 'Platform reconnected', body: `${item.name} token is refreshed in the preview and listings are active again.`, tone: 'ok' })
  }

  function settings(item: Platform) {
    setPanel({ title: `${item.name} settings`, body: 'Listing defaults, credentials, and sync cadence belong to the durable Platforms integration backend.' })
  }

  function connect(marketplace: Marketplace) {
    if (connectedIds.has(marketplace.id)) return
    setRows([...rows, platform(marketplace.id, marketplace.name, marketplace.code, marketplace.seller, 'Connected', 'just now', 0, 0)])
    setPanel({ title: 'Marketplace connected', body: `${marketplace.name} is connected in preview mode. Product selection and OAuth persistence are future integration work.`, tone: 'ok' })
  }

  return (
    <Shell panel={panel} setPanel={setPanel} search={search} setSearch={setSearch} collapsed={collapsed} setCollapsed={setCollapsed}>
      <div className="inventory-heading">
        <div><h1>Platforms</h1><p>{rows.filter(item => item.status !== 'Disconnected').length} connected · StockHub pushes stock and prices to each one in real time</p></div>
        <div className="inventory-actions"><button type="button" onClick={() => setModalOpen(true)}>+ Add platform</button></div>
      </div>
      <OversellProtection enabled={oversell} setEnabled={setOversell} setPanel={setPanel} />
      {filtered.length === 0
        ? <section className="orders-no-results platform-empty-filter"><span>Search</span><h2>No platforms match this search</h2><p>Search: "{search}"</p><button type="button" onClick={() => setSearch('')}>Clear search</button></section>
        : <section className="platform-grid" aria-label="Connected platforms">
          {filtered.map(item => <PlatformCard key={item.id} item={item} pause={pause} disconnect={disconnect} reconnect={reconnect} settings={settings} />)}
          <button className="platform-add-tile" type="button" onClick={() => setModalOpen(true)}><span>+</span><strong>Add platform</strong><small>Connect another marketplace</small></button>
        </section>}
      {modalOpen && <AddPlatformModal connectedIds={connectedIds} connect={connect} close={() => setModalOpen(false)} />}
    </Shell>
  )
}
