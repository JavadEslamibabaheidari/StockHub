import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { AccessApiClient, AccessApiError, type DashboardAttentionResponse, type DashboardImportRequest, type DashboardSnapshotResponse, type ProductRequest, type ProductResponse, type SessionResponse, type WorkspaceSummary } from './api/generated'

const api = new AccessApiClient()
const go = (route: string) => { window.location.hash = route }
const msg = (error: unknown, fallback: string) => error instanceof AccessApiError ? error.problem.detail || fallback : fallback
const nav = [
  ['Dashboard', '#dashboard', '▦', 'Milestone 2 Dashboard'],
  ['Inventory', '#inventory', '◇', 'Milestone 3 Inventory'],
  ['Orders', '#orders', '🛒', 'Milestone 4 Orders'],
  ['Reservations', '#reservations', '◷', 'Milestone 5 Reservations'],
  ['Platforms', '#platforms', '♧', 'Milestone 6 Platforms'],
  ['Pricing rules', '#pricing', '◇', 'Milestone 7 Pricing rules'],
  ['Reports', '#reports', '▥', 'Milestone 8 Reports'],
  ['Team', '#team', '♧', 'Milestone 9 Team'],
  ['Settings', '#settings', '☷', 'Milestone 10 Settings'],
] as const
const platforms = [['Amazon', 'Am'], ['Unieuro', 'Un'], ['Euronics', 'Eu'], ['eBay', 'eB']] as const

type Menu = 'workspace' | 'notifications' | 'account' | 'sync' | null

type Panel = { title: string; body: string; tone?: string } | null

function ErrorText({ value }: { value: string | null }) { return value && <p className="form-error" role="alert">{value}</p> }

function csv(text: string): ProductRequest[] {
  const rows: string[][] = []
  let row: string[] = []
  let cell = ''
  let quoted = false
  for (let index = 0; index < text.length; index += 1) {
    const char = text[index]
    const next = text[index + 1]
    if (char === '"' && quoted && next === '"') { cell += '"'; index += 1; continue }
    if (char === '"') { quoted = !quoted; continue }
    if (char === ',' && !quoted) { row.push(cell); cell = ''; continue }
    if ((char === '\n' || char === '\r') && !quoted) {
      if (char === '\r' && next === '\n') index += 1
      row.push(cell); rows.push(row); row = []; cell = ''; continue
    }
    cell += char
  }
  row.push(cell); rows.push(row)
  const nonEmpty = rows.filter(item => item.some(value => value.trim()))
  const data = nonEmpty[0]?.some(value => /sku/i.test(value)) ? nonEmpty.slice(1) : nonEmpty
  return data.map((cells, index) => {
    const [sku, name, onHand, basePrice, category] = cells
    const product = { sku: sku?.trim() || '', name: name?.trim() || '', onHand: Number(onHand), basePrice: Number(String(basePrice || '').replace(',', '.')), category: category?.trim() || null }
    if (!product.sku || !product.name || !Number.isFinite(product.onHand) || !Number.isFinite(product.basePrice)) throw new Error(`Row ${index + 2} needs SKU, name, on hand, and base price.`)
    return product
  })
}

function QuickImporter({ workspaceId, onSaved }: { workspaceId: string; onSaved: () => Promise<void> }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const sample: ProductRequest[] = [{ sku: 'S24-128', name: 'Samsung Galaxy S24 128GB', onHand: 5, basePrice: 799, category: 'Phones' }]
  async function save(items: ProductRequest[]) {
    setBusy(true); setError(null); setOk(null)
    try { const saved = await api.importProducts(workspaceId, items); setOk(`${saved.length} product${saved.length === 1 ? '' : 's'} saved.`); await onSaved() }
    catch (caught) { setError(msg(caught, 'Could not import products.')) }
    finally { setBusy(false) }
  }
  async function saveDashboardImport(request: DashboardImportRequest) {
    setBusy(true); setError(null); setOk(null)
    try {
      const saved = await api.importDashboard(workspaceId, request)
      setOk(`${saved.productsImported} product${saved.productsImported === 1 ? '' : 's'} and ${saved.ordersImported} order${saved.ordersImported === 1 ? '' : 's'} imported.`)
      await onSaved()
    } catch (caught) {
      setError(msg(caught, 'Could not import dashboard JSON.'))
    } finally {
      setBusy(false)
    }
  }
  async function upload(file: File | null) {
    if (!file) return
    setError(null)
    try {
      const text = await file.text()
      if (file.name.toLowerCase().endsWith('.json') || file.type.includes('json')) {
        await saveDashboardImport(JSON.parse(text) as DashboardImportRequest)
      } else {
        await save(csv(text))
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not read this import file.')
    }
  }
  return <div className="dash-inline-action"><button className="primary-button" disabled={busy} type="button" onClick={() => save(sample)}>{busy ? 'Saving…' : 'Upload CSV'}</button><label className="mini-upload">Browse<input type="file" accept=".csv,.json,text/csv,application/json" onChange={event => upload(event.target.files?.[0] ?? null)} /></label><a href="data:text/csv;charset=utf-8,SKU%2Cname%2Con%20hand%2Cbase%20price%2Ccategory%0AS24-128%2CSamsung%20Galaxy%20S24%20128GB%2C5%2C799%2CPhones%0A" download="stockhub-products-template.csv">CSV template</a><a href="/samples/dashboard-import.sample.json" download>JSON sample</a><ErrorText value={error} />{ok && <p className="form-success" role="status">{ok}</p>}</div>
}

export function Dashboard() {
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null)
  const [snapshot, setSnapshot] = useState<DashboardSnapshotResponse | null>(null)
  const [products, setProducts] = useState<ProductResponse[]>([])
  const [menu, setMenu] = useState<Menu>(null)
  const [query, setQuery] = useState('')
  const [collapsed, setCollapsed] = useState(false)
  const [theme, setTheme] = useState(() => typeof localStorage === 'undefined' ? 'light' : localStorage.getItem('stockhub-theme') || 'light')
  const [error, setError] = useState<string | null>(null)
  const [panel, setPanel] = useState<Panel>(null)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteStatus, setInviteStatus] = useState<string | null>(null)
  const [adjusting, setAdjusting] = useState<DashboardAttentionResponse | null>(null)
  const [onHand, setOnHand] = useState(5)
  const searchRef = useRef<HTMLInputElement | null>(null)

  useEffect(() => { document.documentElement.dataset.theme = theme; localStorage.setItem('stockhub-theme', theme) }, [theme])
  useEffect(() => {
    const key = (event: KeyboardEvent) => { if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); searchRef.current?.focus() } }
    window.addEventListener('keydown', key)
    return () => window.removeEventListener('keydown', key)
  }, [])
  useEffect(() => { void load() }, [])

  async function load(preferredWorkspaceId?: string) {
    setError(null)
    try {
      const data = await api.session()
      const current = data.workspaces.find(item => item.id === (preferredWorkspaceId || data.activeWorkspaceId)) || data.workspaces[0]
      if (!current) { go('workspace'); return }
      setSession(data); setWorkspace(current)
      const [dash, productList] = await Promise.all([api.dashboard(current.id), api.products(current.id)])
      setSnapshot(dash); setProducts(productList)
    } catch (caught) { if (caught instanceof AccessApiError && caught.status === 401) go('signin'); else setError(msg(caught, 'Could not load the dashboard.')) }
  }
  async function chooseWorkspace(id: string) { await api.setActiveWorkspace(id); setMenu(null); await load(id) }
  async function action(actionName: string, options: { targetId?: string | null; onHand?: number | null; platform?: string | null } = {}) {
    if (!workspace) return
    setError(null)
    try { const result = await api.dashboardAction(workspace.id, { action: actionName, ...options }); setSnapshot(result.snapshot); setPanel({ title: result.status, body: result.message }); setProducts(await api.products(workspace.id)) }
    catch (caught) { setError(msg(caught, 'This dashboard action could not be completed.')) }
  }
  async function invite(event: FormEvent) {
    event.preventDefault(); if (!workspace) return
    setInviteStatus(null); setError(null)
    try { await api.invite(workspace.id, { email: inviteEmail.trim(), role: 'Viewer' }); setInviteStatus('Invitation sent.'); setInviteEmail('') }
    catch (caught) { setInviteStatus(msg(caught, 'Invitation email is not configured for this environment. Team management continues in Milestone 9.')) }
  }

  const matches = useMemo(() => {
    const term = query.trim().toLowerCase()
    if (!term || !snapshot) return []
    return snapshot.searchIndex.filter(item => `${item.label} ${item.detail}`.toLowerCase().includes(term)).slice(0, 7)
  }, [query, snapshot])
  const primaryProduct = products[0]
  if (!snapshot || !workspace) return <main className="dashboard-page"><div className="dashboard-shell"><section className="dashboard-main"><p>Loading dashboard…</p><ErrorText value={error} /></section></div></main>

  return <main className={`dashboard-page ${collapsed ? 'sidebar-collapsed' : ''}`}><div className="dashboard-shell"><aside className="dashboard-sidebar"><button className="workspace-switch" type="button" onClick={() => setMenu(menu === 'workspace' ? null : 'workspace')}><b>{workspace.businessName.slice(0, 2).toUpperCase()}</b><span><strong>{workspace.businessName}</strong><small>{workspace.role} · {session?.workspaces.length || 1} workspace{(session?.workspaces.length || 1) === 1 ? '' : 's'}</small></span><span aria-hidden="true">⌄</span></button>{menu === 'workspace' && <div className="dashboard-popover workspace-menu">{session?.workspaces.map(item => <button key={item.id} type="button" onClick={() => chooseWorkspace(item.id)}>{item.businessName}<small>{item.role}</small></button>)}<a href="#workspace">Create workspace</a></div>}<nav aria-label="Main navigation">{nav.map(item => <a key={item[0]} className={item[0] === 'Dashboard' ? 'active' : ''} href={item[1]} title={item[3]}><span aria-hidden="true">{item[2]}</span>{item[0]}{item[0] === 'Reservations' && snapshot.reservations.length > 0 && <b className="nav-badge">{snapshot.reservations.length}</b>}{item[0] === 'Platforms' && snapshot.statusTone === 'danger' && <b className="nav-dot" aria-label="Platform attention" />}</a>)}</nav><div className="sidebar-bottom"><strong>{products.length > 0 ? 'Products imported' : 'Free trial'}</strong><a href="#deferred">Upgrade</a><small>{products.length.toLocaleString()} product{products.length === 1 ? '' : 's'}</small><button type="button" onClick={() => setCollapsed(!collapsed)}>{collapsed ? 'Expand' : 'Collapse'}</button></div></aside><section className="dashboard-main"><header className="dashboard-top"><div className="dashboard-search"><input ref={searchRef} value={query} onChange={event => setQuery(event.target.value)} placeholder="Search products, SKUs, orders..." aria-label="Search products, SKUs, orders" /><kbd>⌘K</kbd>{query && <div className="dashboard-popover search-results">{matches.length ? matches.map(item => <button key={item.id} type="button" onClick={() => { setPanel({ title: item.label, body: `${item.detail}. ${item.type === 'product' ? 'Product editing belongs to Milestone 3 Inventory; on-hand can be adjusted here from attention cards.' : 'Opened from dashboard search.'}` }); setQuery('') }}><b>{item.label}</b><span>{item.detail}</span></button>) : <p>No matching dashboard results.</p>}</div>}</div><div><button type="button" className={`platform-pill ${snapshot.statusTone}`} onClick={() => { setMenu('sync'); setPanel({ title: 'Sync status', body: snapshot.statusLabel }) }}>● &nbsp; {snapshot.statusLabel}</button><button type="button" className="icon-button" aria-label="Toggle appearance" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>{theme === 'dark' ? '☀' : '☾'}</button><button type="button" className="icon-button" aria-label="Notifications" onClick={() => setMenu(menu === 'notifications' ? null : 'notifications')}>♧</button><button type="button" className="account-button" onClick={() => setMenu(menu === 'account' ? null : 'account')}><span className="user-avatar">{session?.fullName?.slice(0, 2).toUpperCase() || 'MR'}</span><span>{session?.fullName || 'Marco Rossi'}<small>Owner</small></span></button></div>{menu === 'notifications' && <div className="dashboard-popover notification-menu"><strong>Notifications</strong>{snapshot.notifications.map(note => <p key={note}>{note}</p>)}</div>}{menu === 'account' && <div className="dashboard-popover account-menu"><a href="#cover">Cover page</a><a href="#onboarding">Onboarding checklist</a><button type="button" onClick={async () => { await api.signOut(); go('signin') }}>Sign out</button></div>}</header><div className="dashboard-title"><h1>{snapshot.title}</h1><p>{snapshot.subtitle}</p></div><ErrorText value={error} />{snapshot.state === 'first-use' && <FirstUse snapshot={snapshot} workspaceId={workspace.id} inviteEmail={inviteEmail} setInviteEmail={setInviteEmail} invite={invite} inviteStatus={inviteStatus} reload={() => load(workspace.id)} action={action} />}{snapshot.state === 'live' && <LiveDashboard snapshot={snapshot} primaryProduct={primaryProduct} action={action} setAdjusting={(item) => { setAdjusting(item); setOnHand(primaryProduct?.onHand ?? 5) }} />}{panel && <section className={`dash-panel ${panel.tone || ''}`} role="status"><button type="button" onClick={() => setPanel(null)} aria-label="Close panel">×</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}{adjusting && <div className="dialog-backdrop"><form className="dialog" onSubmit={event => { event.preventDefault(); void action('adjust-on-hand', { targetId: adjusting.id.startsWith('product:') ? adjusting.id.slice(8) : null, onHand }).then(() => setAdjusting(null)) }}><h2 className="dialog-title">Adjust on hand</h2><p className="dialog-body">{adjusting.title}. This updates the workspace product count used by Dashboard.</p><label className="access-form">On hand<input type="number" min="0" value={onHand} onChange={event => setOnHand(Number(event.target.value))} /></label><div className="dialog-actions"><button className="secondary-button" type="button" onClick={() => setAdjusting(null)}>Cancel</button><button className="primary-button">Save count</button></div></form></div>}</section></div></main>
}

function FirstUse({ snapshot, workspaceId, inviteEmail, setInviteEmail, invite, inviteStatus, reload, action }: { snapshot: DashboardSnapshotResponse; workspaceId: string; inviteEmail: string; setInviteEmail: (value: string) => void; invite: (event: FormEvent) => void; inviteStatus: string | null; reload: () => Promise<void>; action: (name: string, options?: { platform?: string }) => Promise<void> }) {
  return <><section className="get-started-card"><div className="progress-line"><strong>Get started&nbsp;&nbsp; 0 of 3 done</strong><span><i style={{ width: '0%' }} /></span></div><div className="started-grid"><article><b>1</b><h2>Import products</h2><p>Upload a CSV or add products one by one</p><QuickImporter workspaceId={workspaceId} onSaved={reload} /></article><article><b>2</b><h2>Connect a platform</h2><p>Choose where you sell. You can add more later.</p><div className="platform-shortcuts">{platforms.map(platform => <button key={platform[0]} type="button" onClick={() => action('start-first-sync', { platform: platform[0] })}>{platform[1]}</button>)}</div></article><article><b>3</b><h2>Invite your team</h2><p>Give your staff their own login and role</p><form className="invite-inline" onSubmit={invite}><input type="email" required value={inviteEmail} onChange={event => setInviteEmail(event.target.value)} placeholder="name@company.it" /><button className="primary-button">Invite</button></form>{inviteStatus && <p className="field-hint" role="status">{inviteStatus}</p>}</article></div></section><Metrics metrics={snapshot.metrics} /><div className="dashboard-grid"><section className="dash-card large"><h2>Live reservations</h2><div className="empty-box"><strong>No reservations yet</strong><p>When a customer books a product on a connected platform, it appears here with a 10-minute countdown.</p></div></section><aside className="side-stack"><Sales snapshot={snapshot} /><Attention snapshot={snapshot} /></aside></div></>
}

function FirstSync({ snapshot, action }: { snapshot: DashboardSnapshotResponse; action: (name: string) => Promise<void> }) {
  return <><section className="sync-card"><div className="sync-heading"><h2>● First sync with {snapshot.sync?.platform}</h2><strong>{snapshot.sync?.syncedProducts.toLocaleString()} of {snapshot.sync?.totalProducts.toLocaleString()} products <small>{snapshot.sync?.remainingLabel}</small></strong></div><div className="sync-bar"><i style={{ width: `${((snapshot.sync?.syncedProducts || 0) / (snapshot.sync?.totalProducts || 1)) * 100}%` }} /></div><div className="sync-steps">{snapshot.sync?.steps.map(step => <article key={step.key} className={step.status}><b>{step.status === 'done' ? '✓' : '↻'}</b><strong>{step.title}</strong><small>{step.detail}</small></article>)}</div><p>You can keep working. Stock changes made now are queued and sent as soon as the first sync finishes.</p><button className="secondary-button" onClick={() => action('finish-first-sync')}>Finish sync preview</button></section><Metrics metrics={snapshot.metrics} /><div className="dashboard-grid"><section className="dash-card large"><h2>Live reservations</h2><div className="empty-box"><strong>Waiting for Amazon</strong><p>Once your products are live on Amazon, customer bookings appear here with a 10-minute countdown.</p></div></section><aside className="side-stack"><Sales snapshot={snapshot} /><Attention snapshot={snapshot} /></aside></div></>
}

function LiveDashboard({ snapshot, primaryProduct, action, setAdjusting }: { snapshot: DashboardSnapshotResponse; primaryProduct?: ProductResponse; action: (name: string) => Promise<void>; setAdjusting: (item: DashboardAttentionResponse) => void }) {
  return <><Metrics metrics={snapshot.metrics} /><div className="dashboard-grid"><section className="dash-card large"><header><h2>● Live reservations</h2><button type="button" className="text-button" onClick={() => action('open-reservations')}>View all</button></header><div className="reservation-list">{snapshot.reservations.map(row => <button type="button" key={row.id} onClick={() => action('open-reservations')}><span className="reservation-icon">◷</span><span><strong>{row.productName}</strong><small>{row.platform} · {row.quantity} · Order {row.orderNumber}</small></span><b>{row.platform.slice(0, 2)}</b><i><em style={{ width: `${row.progressPercent}%` }} /></i><strong>{row.timeLeft}</strong></button>)}</div></section><aside className="side-stack"><Sales snapshot={snapshot} /><Attention snapshot={snapshot} primaryProduct={primaryProduct} action={action} setAdjusting={setAdjusting} /></aside></div></>
}

function Metrics({ metrics }: { metrics: DashboardSnapshotResponse['metrics'] }) { return <section className="metric-grid">{metrics.map(metric => <article key={metric.key} className={`metric-card ${metric.tone}`}><span>{metric.label}</span><strong>{metric.value}</strong><small>{metric.hint}</small></article>)}</section> }
function Sales({ snapshot }: { snapshot: DashboardSnapshotResponse }) { return <section className="dash-card"><h2>Sales by platform</h2>{snapshot.salesByPlatform.length ? <><div className="sales-bar">{snapshot.salesByPlatform.map(sale => <i key={sale.platform} className={sale.tone} style={{ width: `${sale.percent}%` }} title={`${sale.platform} ${sale.percent}%`} />)}</div><ul className="sales-list">{snapshot.salesByPlatform.map(sale => <li key={sale.platform}><span>● {sale.platform}</span><b>{sale.percent.toFixed(1)}%</b></li>)}</ul></> : <div className="empty-line">Connect a platform to see where your sales come from.</div>}</section> }
function Attention({ snapshot, primaryProduct, action, setAdjusting }: { snapshot: DashboardSnapshotResponse; primaryProduct?: ProductResponse; action?: (name: string) => Promise<void>; setAdjusting?: (item: DashboardAttentionResponse) => void }) { return <section className="dash-card"><h2>Alerts</h2><div className="attention-list">{snapshot.attention.map(item => <article key={item.id} className={item.tone}><strong>{item.title}</strong><p>{item.detail}</p>{item.actions.map(act => <button key={act.key} type="button" className={act.style === 'primary' ? 'primary-button' : 'secondary-button'} onClick={() => { if (act.key === 'adjust-on-hand' && setAdjusting && item.id.startsWith('product:')) setAdjusting(item); else action?.(act.key) }}>{act.label}</button>)}</article>)}</div></section> }
