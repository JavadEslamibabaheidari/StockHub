import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { AccessApiClient, AccessApiError, type WorkspaceSummary } from './api/generated'

type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type ShellIdentity = { workspaceName: string; workspaceInitials: string; workspaceCount: number; fullName: string; userInitials: string; role: string }
type ReservationStatus = 'Active' | 'Released' | 'Converted' | 'Expired'
type Reservation = {
  id: string
  product: string
  sku: string
  platform: string
  code: string
  order: string
  qty: number
  reserved: string
  timeLeftSeconds: number
  value: number
  status: ReservationStatus
}
type ExpiredReservation = {
  product: string
  platform: string
  order: string
  expired: string
  value: number
}

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
const api = new AccessApiClient()

const initialReservations: Reservation[] = [
  reservation('res-1', 'Samsung Galaxy S24 128GB', 'SAM-S921B-128-BK', 'Unieuro', 'Un', '#UN-48213', 1, '9 min ago', 53, 799),
  reservation('res-2', 'Nintendo Switch OLED', 'NIN-HEG-001-W', 'Amazon', 'Am', '#AMZ-7731', 1, '6 min ago', 213, 349),
  reservation('res-3', "De'Longhi Magnifica S", 'DLG-ECAM22-110', 'Amazon', 'Am', '#AMZ-7735', 1, '5 min ago', 298, 366.45),
  reservation('res-4', 'Dyson V15 Detect', 'DYS-V15-DET', 'Unieuro', 'Un', '#UN-48220', 1, '2 min ago', 430, 649),
  reservation('res-5', 'Samsung Galaxy S24 128GB', 'SAM-S921B-128-BK', 'Amazon', 'Am', '#AMZ-7742', 1, '2 min ago', 450, 838.95),
  reservation('res-6', 'Apple AirPods Pro 2', 'APL-MTJY3ZM-A', 'eBay', 'eB', '#EB-2291', 1, 'just now', 545, 279),
]

const initialExpired: ExpiredReservation[] = [
  { product: 'Samsung Galaxy S24 128GB', platform: 'Unieuro', order: '#UN-48190', expired: '14 min ago', value: 799 },
  { product: 'Dyson V15 Detect', platform: 'eBay', order: '#EB-2284', expired: '1 h ago', value: 649 },
  { product: 'Nintendo Switch OLED', platform: 'Amazon', order: '#AMZ-7702', expired: '26 Sep 2026, 09:05', value: 366.45 },
]

function reservation(id: string, product: string, sku: string, platform: string, code: string, order: string, qty: number, reserved: string, timeLeftSeconds: number, value: number): Reservation {
  return { id, product, sku, platform, code, order, qty, reserved, timeLeftSeconds, value, status: 'Active' }
}

function initials(value: string, fallback = 'SH') {
  const parts = value.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return fallback
  return parts.slice(0, 2).map(part => part[0]?.toUpperCase()).join('') || fallback
}

function formatRole(role?: WorkspaceSummary['role'] | string | null) {
  return role === 'WarehouseStaff' ? 'Warehouse staff' : role || 'Owner'
}

function eur(value: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'EUR' }).format(value)
}

function mmss(seconds: number) {
  const minutes = Math.floor(seconds / 60)
  const rest = seconds % 60
  return `${minutes.toString().padStart(2, '0')}:${rest.toString().padStart(2, '0')}`
}

function csvEscape(value: string | number) {
  const text = String(value)
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

function csvDownload(rows: (string | number)[][]) {
  return `data:text/csv;charset=utf-8,${encodeURIComponent(rows.map(row => row.map(csvEscape).join(',')).join('\n'))}`
}

function ProductIcon({ product }: { product: string }) {
  const icon = product.includes('Nintendo') ? '▣' : product.includes('Magnifica') ? '◫' : product.includes('Dyson') ? '⇄' : product.includes('AirPods') ? '◌' : '▯'
  return <span className="reservation-product-icon" aria-hidden="true">{icon}</span>
}

function PlatformBadge({ code, name }: { code: string; name: string }) {
  return <span className="order-platform reservation-platform" title={name}>{code}</span>
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
    <main className={`inventory-page reservations-page ${collapsed ? 'reservations-collapsed' : ''}`}>
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <button className="inventory-workspace reservation-workspace-button" type="button" onClick={() => showPanel({ title: 'Workspace switcher', body: `${identity.workspaceName} is active. Workspace switching belongs to the Team milestone.` })} aria-label="Workspace dashboard">
            <b>{identity.workspaceInitials}</b><span><strong>{identity.workspaceName}</strong><small>{identity.role} · {identity.workspaceCount} workspace{identity.workspaceCount === 1 ? '' : 's'}</small></span><span>⌄</span>
          </button>
          <nav aria-label="Main navigation">
            {nav.map(([label, href]) => <a key={label} className={label === 'Reservations' ? 'active' : ''} href={href}>{label}{label === 'Reservations' && <b>6</b>}{label === 'Platforms' && <i />}</a>)}
          </nav>
          <div className="inventory-plan"><strong>Pro plan</strong><a href="#settings" onClick={() => showPanel({ title: 'Billing handoff', body: 'Plan upgrades are handled in Settings Billing, milestone 10.' })}>Upgrade</a><span /><small>1,240 / 2,000 products</small></div>
          <button className="sidebar-collapse-button" type="button" onClick={() => { setAccountOpen(false); setCollapsed(!collapsed) }}>{collapsed ? 'Expand' : 'Collapse'}</button>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className="sync-alert" type="button" onClick={() => showPanel({ title: 'Sync status', body: '3 of 4 platforms synced. Euronics failed; retry and credential repair belong to Platforms milestone 6.', tone: 'warn' })}>3 of 4 synced · Euronics failed</button>
              <button type="button" aria-label="Toggle appearance" onClick={() => document.documentElement.dataset.theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => showPanel({ title: 'Notifications', body: '5 reservation alerts: 3 expired today and 2 platform sync warnings.' })}>5</button>
              <button className="reservation-account" type="button" onClick={() => setAccountOpen(!accountOpen)} aria-expanded={accountOpen}><b>{identity.userInitials}</b><span>{identity.fullName}<small>{identity.role}</small></span><span>⌄</span></button>
            </div>
          </header>
          {accountOpen && <section className="dashboard-popover account-menu reservation-account-menu"><button type="button" onClick={() => showPanel({ title: 'Profile', body: `${identity.fullName} · ${identity.role} · ${identity.workspaceName}` })}>View profile</button><a href="#settings" onClick={() => setAccountOpen(false)}>Account settings</a><button type="button" onClick={() => showPanel({ title: 'Signed out preview', body: 'This preview keeps you in the app so Milestone 5 can be reviewed.' })}>Sign out</button></section>}
          {children}
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>×</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function SummaryCards({ reservations, expired }: { reservations: Reservation[]; expired: ExpiredReservation[] }) {
  const active = reservations.filter(item => item.status === 'Active')
  const expiring = active.filter(item => item.timeLeftSeconds < 60)
  return (
    <div className="reservation-metrics">
      <article><span>Active reservations</span><strong>{active.length}</strong><small>Across 3 platforms</small></article>
      <article><span>Units held</span><strong>{active.reduce((total, item) => total + item.qty, 0)}</strong><small>Taken out of Available</small></article>
      <article><span>Expiring in under a minute</span><strong>{expiring.length}</strong><small>Amber below 60 s</small></article>
      <article><span>Expired today</span><strong>{expired.length}</strong><small>Lost-sale signals · {eur(expired.reduce((total, item) => total + item.value, 0))}</small></article>
    </div>
  )
}

function ReservationsTable({
  rows,
  onRelease,
  setPanel,
}: {
  rows: Reservation[]
  onRelease: (id: string) => void
  setPanel: (panel: Panel) => void
}) {
  return (
    <section className="inventory-card reservation-card">
      <header><h2><i />Active now</h2><button type="button" onClick={() => setPanel({ title: 'Reservation rule', body: 'Every active hold lasts 10 minutes. Payment converts it to an order; expiry releases units back to Available.' })}>Rule</button></header>
      <div className="reservation-table" role="table" aria-label="Active reservations">
        <div className="reservation-tr head" role="row"><span>Product</span><span>Platform</span><span>Order</span><span>Qty</span><span>Reserved</span><span>Time left</span><span /></div>
        {rows.map(row => (
          <div className={`reservation-tr ${row.timeLeftSeconds < 60 ? 'expiring' : ''}`} role="row" key={row.id}>
            <span className="reservation-product"><ProductIcon product={row.product} /><b>{row.product}</b><small>{row.sku}</small></span>
            <span><PlatformBadge code={row.code} name={row.platform} />{row.platform}</span>
            <a href={`#order-detail?order=${encodeURIComponent(row.order)}`}>{row.order}</a>
            <span>{row.qty}</span>
            <span>{row.reserved}</span>
            <span className="reservation-time"><b>{mmss(row.timeLeftSeconds)}</b><i><em style={{ width: `${Math.max(10, (row.timeLeftSeconds / 600) * 100)}%` }} /></i></span>
            <button type="button" onClick={() => onRelease(row.id)}>Release now</button>
          </div>
        ))}
      </div>
    </section>
  )
}

function ExpiredTable({ rows, restore }: { rows: ExpiredReservation[]; restore: (row: ExpiredReservation) => void }) {
  return (
    <section className="inventory-card reservation-card expired-reservations">
      <header><h2>Expired today <span>Lost-sale signals</span></h2></header>
      <div className="expired-table" role="table" aria-label="Expired reservations">
        <div className="expired-tr head" role="row"><span>Product</span><span>Platform</span><span>Order</span><span>Expired</span><span>Value</span><span /></div>
        {rows.map(row => (
          <div className="expired-tr" role="row" key={row.order}>
            <span><b>{row.product}</b></span><span>{row.platform}</span><span>{row.order}</span><span>{row.expired}</span><strong>{eur(row.value)}</strong><button type="button" onClick={() => restore(row)}>Restore hold</button>
          </div>
        ))}
      </div>
    </section>
  )
}

export function Reservations() {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  const [collapsed, setCollapsed] = useState(false)
  const [rows, setRows] = useState(initialReservations)
  const [expired, setExpired] = useState(initialExpired)
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return rows.filter(row => row.status === 'Active' && (!q || [row.product, row.sku, row.order, row.platform].some(value => value.toLowerCase().includes(q))))
  }, [rows, search])
  const exportRows = [['product', 'sku', 'platform', 'order', 'qty', 'reserved', 'time_left', 'value'], ...filtered.map(row => [row.product, row.sku, row.platform, row.order, row.qty, row.reserved, mmss(row.timeLeftSeconds), row.value])]

  function release(id: string) {
    const target = rows.find(row => row.id === id)
    if (!target) return
    setRows(rows.map(row => row.id === id ? { ...row, status: 'Released' } : row))
    setPanel({ title: 'Reservation released', body: `${target.qty} ${target.qty === 1 ? 'unit' : 'units'} of ${target.product} returned to Available on ${target.platform}.`, tone: 'ok' })
  }

  function restore(row: ExpiredReservation) {
    setExpired(expired.filter(item => item.order !== row.order))
    setRows([...rows, reservation(`restored-${row.order}`, row.product, 'RESTORED-HOLD', row.platform, row.platform.slice(0, 2), row.order, 1, 'just now', 600, row.value)])
    setPanel({ title: 'Hold restored', body: `${row.order} is held again for 10 minutes and Available is reduced by 1.`, tone: 'ok' })
  }

  function refreshTimers() {
    setRows(rows.map(row => row.status === 'Active' ? { ...row, timeLeftSeconds: Math.max(0, row.timeLeftSeconds - 15) } : row))
    setPanel({ title: 'Timers refreshed', body: 'Active reservation timers moved forward by 15 seconds for this preview.', tone: 'ok' })
  }

  return (
    <Shell panel={panel} setPanel={setPanel} search={search} setSearch={setSearch} collapsed={collapsed} setCollapsed={setCollapsed}>
      <div className="inventory-heading">
        <div><h1>Reservations</h1><p>Units booked on a platform but not yet paid. Each hold lasts 10 minutes, then the units return to Available.</p></div>
        <div className="inventory-actions"><button type="button" onClick={refreshTimers}>Refresh timers</button><a href={csvDownload(exportRows)} download="stockhub-reservations.csv">Export CSV</a></div>
      </div>
      <SummaryCards reservations={rows} expired={expired} />
      {filtered.length === 0
        ? <section className="orders-no-results reservation-empty-filter"><span>Search</span><h2>No active reservations match this search</h2><p>Search: "{search}"</p><button type="button" onClick={() => setSearch('')}>Clear search</button></section>
        : <ReservationsTable rows={filtered} onRelease={release} setPanel={setPanel} />}
      <ExpiredTable rows={expired} restore={restore} />
    </Shell>
  )
}
