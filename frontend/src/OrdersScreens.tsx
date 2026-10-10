import { useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  AccessApiClient,
  AccessApiError,
  type OrderDetailResponse,
  type OrderListResponse,
  type OrderSummaryResponse,
  type WorkspaceSummary,
} from './api/generated'

const api = new AccessApiClient()
type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type FilterTab = 'All' | 'Awaiting payment' | 'To pick' | 'To ship' | 'Shipped' | 'Returns' | 'Cancelled'
type ShellIdentity = { workspaceName: string; workspaceInitials: string; workspaceCount: number; fullName: string; userInitials: string; role: string }

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

function eur(value: number) {
  return `€${value.toFixed(2)}`
}

function problem(error: unknown, fallback: string) {
  return error instanceof AccessApiError ? error.problem.detail || fallback : fallback
}

function initials(value: string, fallback = 'SH') {
  const parts = value.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return fallback
  return parts.slice(0, 2).map(part => part[0]?.toUpperCase()).join('') || fallback
}

function formatRole(role?: WorkspaceSummary['role'] | string | null) {
  return role === 'WarehouseStaff' ? 'Warehouse staff' : role || 'Owner'
}

function csvEscape(value: string | number | null | undefined) {
  const text = value === null || value === undefined ? '' : String(value)
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

function csvDownload(rows: (string | number | null | undefined)[][]) {
  return `data:text/csv;charset=utf-8,${encodeURIComponent(rows.map(row => row.map(csvEscape).join(',')).join('\n'))}`
}

function statusForTab(orderStatus: string): FilterTab {
  if (orderStatus === 'Paid - to pick') return 'To pick'
  if (orderStatus === 'Picked - to ship') return 'To ship'
  if (orderStatus === 'Return requested') return 'Returns'
  if (orderStatus === 'Cancelled') return 'Cancelled'
  if (orderStatus === 'Awaiting payment') return 'Awaiting payment'
  return 'Shipped'
}

function statusClass(status: string) {
  return status.toLowerCase().replace(/\s+/g, '-')
}

function OrderStatusPill({ status }: { status: string }) {
  return <span className={`order-status ${statusClass(status)}`}>{status}</span>
}

function PlatformBadge({ code, name }: { code: string; name: string }) {
  return <span className="order-platform" title={name}>{code}</span>
}

function Shell({
  children,
  role,
  sync = 'Backend data · no mocked orders',
  panel,
  setPanel,
  search,
  setSearch,
}: {
  children: ReactNode
  role?: 'Owner' | 'Warehouse staff'
  sync?: string
  panel: Panel
  setPanel: (panel: Panel) => void
  search?: string
  setSearch?: (value: string) => void
}) {
  const [identity, setIdentity] = useState<ShellIdentity>({
    workspaceName: 'Your workspace',
    workspaceInitials: 'SH',
    workspaceCount: 1,
    fullName: 'Your account',
    userInitials: 'SH',
    role: role || 'Owner',
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
          role: role || formatRole(workspace?.role),
        })
      })
      .catch(error => { if (!(error instanceof AccessApiError && error.status === 401)) console.warn(error) })
    return () => { alive = false }
  }, [role])
  return (
    <main className="inventory-page orders-page">
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <a className="inventory-workspace" href="#dashboard" aria-label="Workspace dashboard">
            <b>{identity.workspaceInitials}</b><span><strong>{identity.workspaceName}</strong><small>{identity.role} · {identity.workspaceCount} workspace{identity.workspaceCount === 1 ? '' : 's'}</small></span><span>⌄</span>
          </a>
          <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={label} className={label === 'Orders' ? 'active' : ''} href={href}>{label}</a>)}</nav>
          <div className="inventory-plan"><strong>{identity.role === 'Owner' ? 'Pro plan' : 'Free trial'}</strong><a href="#settings">Upgrade</a><span /></div>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input value={search || ''} onChange={event => setSearch?.(event.target.value)} placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className={sync.includes('failed') ? 'sync-alert' : 'sync-ok'} type="button" onClick={() => setPanel({ title: 'Sync status', body: sync })}>{sync}</button>
              <button type="button" aria-label="Toggle appearance" onClick={() => { document.documentElement.dataset.theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark' }}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => setPanel({ title: 'Notifications', body: 'Order notifications come from persisted order state.' })}>!</button>
              <b>{identity.userInitials}</b><span>{identity.fullName}<small>{identity.role}</small></span>
            </div>
          </header>
          {children}
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>x</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function useWorkspaceOrders() {
  const [workspaceId, setWorkspaceId] = useState<string | null>(null)
  const [data, setData] = useState<OrderListResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setError(null)
    try {
      const session = await api.session()
      const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
      if (!workspace) {
        window.location.hash = '#workspace'
        return
      }
      setWorkspaceId(workspace.id)
      setData(await api.orders(workspace.id))
    } catch (caught) {
      if (caught instanceof AccessApiError && caught.status === 401) window.location.hash = '#signin'
      else setError(problem(caught, 'Could not load orders.'))
    }
  }

  useEffect(() => { void load() }, [])
  return { workspaceId, data, error, reload: load }
}

function OrdersFilters({
  tab,
  setTab,
  platform,
  setPlatform,
  date,
  setDate,
  search,
  setSearch,
  rows,
}: {
  tab: FilterTab
  setTab: (tab: FilterTab) => void
  platform: string
  setPlatform: (platform: string) => void
  date: string
  setDate: (date: string) => void
  search: string
  setSearch: (value: string) => void
  rows: OrderSummaryResponse[]
}) {
  const tabs: FilterTab[] = ['All', 'Awaiting payment', 'To pick', 'To ship', 'Shipped', 'Returns', 'Cancelled']
  return (
    <>
      <div className="filters orders-filters">
        {tabs.map(item => <button type="button" key={item} className={tab === item ? 'selected' : ''} onClick={() => setTab(item)}>{item} <b>{item === 'All' ? rows.length : rows.filter(row => statusForTab(row.status) === item).length}</b></button>)}
        <select aria-label="Platform filter" value={platform} onChange={event => setPlatform(event.target.value)}><option>All</option><option>Amazon</option><option>Unieuro</option><option>Euronics</option><option>eBay</option></select>
        <select aria-label="Date filter" value={date} onChange={event => setDate(event.target.value)}><option>Today</option><option>This week</option><option>This month</option></select>
      </div>
      <label className="order-inline-search"><span className="sr-only">Order search</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Order number or product" /></label>
    </>
  )
}

function OrdersTable({ workspaceId, rows, staff, reload, setPanel }: { workspaceId: string | null; rows: OrderSummaryResponse[]; staff?: boolean; reload: () => Promise<void>; setPanel: (panel: Panel) => void }) {
  async function action(row: OrderSummaryResponse, actionName: string) {
    if (!workspaceId) return
    try {
      const result = await api.orderAction(workspaceId, row.id, { action: actionName })
      setPanel({ title: 'Order updated', body: `${result.order.summary.orderNumber}: ${result.order.summary.status}.`, tone: 'ok' })
      await reload()
    } catch (caught) {
      setPanel({ title: 'Order action failed', body: problem(caught, 'The order could not be updated.'), tone: 'warn' })
    }
  }

  function staffAction(row: OrderSummaryResponse) {
    if (row.status === 'Paid - to pick') return <button type="button" onClick={() => action(row, 'mark-picked')}>Pick</button>
    if (row.status === 'Picked - to ship') return <button type="button" onClick={() => action(row, 'mark-shipped')}>Mark shipped</button>
    if (row.status === 'Shipped') return <button type="button" onClick={() => setPanel({ title: 'Tracking opened', body: `${row.orderNumber} is in transit with carrier tracking visible to the customer.` })}>Tracking</button>
    if (row.status === 'Return requested') return <button type="button" onClick={() => action(row, 'receive-return')}>Receive return</button>
    return <span>{row.nextStep}</span>
  }

  return (
    <section className="inventory-card orders-card">
      <div className={`orders-table ${staff ? 'staff' : ''}`} role="table" aria-label="Orders">
        <div className="orders-tr head" role="row">
          <span>Order</span><span>Product</span><span>Platform</span><span>Qty</span><span>Status</span>{staff ? <span>Next step</span> : <><span>Total</span><span>Fee</span><span>Net</span><span>Margin</span></>}<span />
        </div>
        {rows.map(row => (
          <div className="orders-tr" role="row" key={row.id}>
            <a href={`#order-detail?order=${encodeURIComponent(row.id)}`}><b>{row.orderNumber}</b><small>{row.age}</small></a>
            <span><b>{row.product}</b><small>{row.sku}</small></span>
            <span><PlatformBadge code={row.platformCode} name={row.platform} /> {row.platform}</span>
            <span>{row.quantity}</span>
            <span><OrderStatusPill status={row.status} /></span>
            {staff ? <span className="next-action">{staffAction(row)}</span> : <><span><b>{eur(row.total)}</b></span><span>{eur(row.fee)}</span><span>{eur(row.net)}</span><span>{row.margin === null || row.margin === undefined ? '-' : <b>{eur(row.margin)}</b>}</span></>}
            <a href={`#order-detail?order=${encodeURIComponent(row.id)}`} aria-label={`Open ${row.orderNumber}`}>›</a>
          </div>
        ))}
      </div>
      <footer className="table-foot">Showing {rows.length} persisted order{rows.length === 1 ? '' : 's'} <span><button disabled>Previous</button><button type="button" onClick={() => setPanel({ title: 'No more rows', body: 'All backend orders are visible.' })}>Next</button></span></footer>
    </section>
  )
}

export function Orders({ staff = false, noResults = false }: { staff?: boolean; noResults?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const { workspaceId, data, error, reload } = useWorkspaceOrders()
  const [search, setSearch] = useState(noResults ? 'XM5' : '')
  const [tab, setTab] = useState<FilterTab>(noResults ? 'Returns' : 'All')
  const [platform, setPlatform] = useState(noResults ? 'Euronics' : 'All')
  const [date, setDate] = useState('Today')
  const rows = data?.orders || []
  const filtered = useMemo(() => rows.filter(row => {
    const q = search.trim().toLowerCase()
    const matchesTab = tab === 'All' || statusForTab(row.status) === tab
    const matchesPlatform = platform === 'All' || row.platform === platform
    const matchesSearch = !q || [row.orderNumber, row.product, row.sku, row.customer].some(value => value.toLowerCase().includes(q))
    return matchesTab && matchesPlatform && matchesSearch
  }), [platform, rows, search, tab])
  const exportRows = [['order', 'product', 'platform', 'qty', 'status', 'total', 'fee', 'net', 'margin'], ...filtered.map(row => [row.orderNumber, row.product, row.platform, row.quantity, row.status, row.total, row.fee, row.net, row.margin])]

  function clearFilters() {
    setSearch('')
    setTab('All')
    setPlatform('All')
    setDate('Today')
  }

  return (
    <Shell role={staff ? 'Warehouse staff' : undefined} sync={data?.syncSummary || 'Loading backend orders'} panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="inventory-heading">
        <div><h1>Orders</h1><p>{data ? `${data.totalToday} today · ${rows.length} persisted` : 'Loading orders…'}{error ? ` · ${error}` : ''}</p></div>
        {staff ? <div className="inventory-actions"><button type="button" onClick={() => setPanel({ title: 'Picking list ready', body: `${rows.filter(row => row.status === 'Paid - to pick').length} paid orders are ready to pick.`, tone: 'ok' })}>Print picking list ({rows.filter(row => row.status === 'Paid - to pick').length})</button></div> : <div className="inventory-actions"><a href={csvDownload(exportRows)} download="stockhub-orders.csv">Export CSV</a></div>}
      </div>
      {staff && <p className="staff-note">You are signed in as <b>Warehouse staff</b>. You can pick, pack, ship and receive returns. Totals, fees, margins and refunds are handled by Owners and Managers.</p>}
      <OrdersFilters tab={tab} setTab={setTab} platform={platform} setPlatform={setPlatform} date={date} setDate={setDate} search={search} setSearch={setSearch} rows={rows} />
      {filtered.length === 0 ? <section className="orders-no-results"><span>Search</span><h2>No orders match these filters</h2><p>Status: {tab} · Platform: {platform} · Search: "{search}"</p><button type="button" onClick={clearFilters}>Clear filters</button></section> : <OrdersTable workspaceId={workspaceId} rows={filtered} staff={staff} reload={reload} setPanel={setPanel} />}
    </Shell>
  )
}

export function OrdersEmpty() {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  return (
    <Shell sync="No orders imported" panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="inventory-heading"><div><h1>Orders</h1><p>0 orders</p></div></div>
      <section className="empty-inventory orders-empty">
        <div><span className="empty-icon">cart</span><h2>No orders yet</h2><p>Orders appear here after you upload a Dashboard JSON import or connect a real platform integration.</p><a className="primary-link-button" href="#dashboard">Upload Dashboard JSON</a></div>
        <div className="order-how"><h3>How an order moves</h3><p><b>Awaiting payment</b><span>Reserved until paid or cancelled</span></p><p><b>Paid</b><span>Ready to pick</span></p><p><b>Picked</b><span>On hand decreases</span></p><p><b>Shipped</b><span>Tracking sent to customer</span></p></div>
      </section>
    </Shell>
  )
}

function useOrderDetail(mode: 'normal' | 'return' | 'cancel') {
  const [workspaceId, setWorkspaceId] = useState<string | null>(null)
  const [detail, setDetail] = useState<OrderDetailResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const params = typeof window === 'undefined' ? new URLSearchParams() : new URLSearchParams(window.location.hash.split('?')[1] || '')
  const requested = params.get('order')

  async function load() {
    try {
      const session = await api.session()
      const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
      if (!workspace) return
      setWorkspaceId(workspace.id)
      const list = await api.orders(workspace.id)
      const id = requested
        || (mode === 'return' ? list.orders.find(row => row.status === 'Return requested')?.id
          : mode === 'cancel' ? list.orders.find(row => row.status === 'Paid - to pick')?.id
            : list.orders[0]?.id)
      if (id) setDetail(await api.order(workspace.id, id))
      else setError('No persisted order is available yet.')
    } catch (caught) {
      setError(problem(caught, 'Could not load order detail.'))
    }
  }

  useEffect(() => { void load() }, [mode, requested])
  return { workspaceId, detail, error, reload: load }
}

export function OrderDetail({ mode = 'normal' }: { mode?: 'normal' | 'return' | 'cancel' }) {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  const [cancelOpen, setCancelOpen] = useState(mode === 'cancel')
  const { workspaceId, detail, error, reload } = useOrderDetail(mode)

  async function orderAction(action: string, reason?: string) {
    if (!workspaceId || !detail) return
    try {
      const result = await api.orderAction(workspaceId, detail.summary.id, { action, reason })
      setPanel({ title: 'Order updated', body: `${result.order.summary.orderNumber}: ${result.order.summary.status}.`, tone: 'ok' })
      setCancelOpen(false)
      await reload()
    } catch (caught) {
      setPanel({ title: 'Order action failed', body: problem(caught, 'The order could not be updated.'), tone: 'warn' })
    }
  }

  if (!detail) return <Shell panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}><p>{error || 'Loading order detail…'}</p></Shell>
  const order = detail.summary

  return (
    <Shell panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="detail-title order-detail-title">
        <a href="#orders">Orders</a><span>›</span><span>{order.orderNumber}</span><h1>Order {order.orderNumber}</h1><OrderStatusPill status={order.status} />
        <p>{order.platform} · placed {order.age} · {order.customer}</p>
        <button type="button" onClick={() => { setPanel({ title: 'Invoice opened', body: `${order.orderNumber} invoice is ready in the browser print dialog.`, tone: 'ok' }); window.print() }}>Print invoice</button>
        {mode === 'normal' && <button type="button" onClick={() => orderAction('request-return', 'Customer request')}>Start return</button>}
        {order.status === 'Paid - to pick' && <button type="button" className="danger-button" onClick={() => setCancelOpen(true)}>Cancel order</button>}
        <button type="button" onClick={() => orderAction('refund')}>Refund</button>
      </div>
      <div className="order-timeline">{detail.timeline.map(step => <span key={step.key} className={step.status === 'done' ? 'done' : ''}><b>{step.label}</b><small>{step.detail}</small></span>)}</div>
      {mode === 'return' && <section className="return-box"><h2>Return <small>{detail.return?.reason || 'Customer request'}</small></h2><div className="return-steps">{['Requested', 'Approved', 'Received', 'Restocked', 'Refunded'].map(step => <span key={step} className={detail.return?.stage === step ? 'done' : ''}>{step === 'Restocked' ? 'Restock or write off' : step}</span>)}</div><p>Return actions update persisted order state and restock inventory when requested.</p><div className="dialog-actions"><button type="button" onClick={() => orderAction('reject-return')}>Reject</button><button type="button" onClick={() => orderAction('approve-return')}>Approve return</button><button type="button" onClick={() => orderAction('receive-return')}>Receive item</button><button type="button" onClick={() => orderAction('restock-return')}>Restock</button><button type="button" onClick={() => orderAction('refund')}>Refund customer</button></div></section>}
      <div className="detail-grid order-detail-grid">
        <section className="detail-card order-items">
          <h2>Items</h2>
          <div className="mini-table"><b>Product</b><b>Qty</b><b>Unit price</b><b>Total</b>{detail.items.map(item => <div className="mini-row" key={`${item.sku}-${item.productId}`}><span>{item.productName}<small>{item.sku}</small></span><span>{item.quantity}</span><span>{eur(item.unitPrice)}</span><strong>{eur(item.total)}</strong></div>)}</div>
          <div className="order-totals"><p><span>Total paid by customer</span><b>{eur(detail.financials.totalPaidByCustomer)}</b></p><p><span>incl. VAT 22%</span><span>{eur(detail.financials.vat)}</span></p><p><span>{order.platform} fee</span><span>-{eur(detail.financials.platformFee)}</span></p><p><span>Net payout</span><b>{eur(detail.financials.netPayout)}</b></p><p><span>Cost of goods</span><span>-{eur(detail.financials.costOfGoods)}</span></p><p><span>Margin (ex VAT)</span><b>{detail.financials.marginExVat === null || detail.financials.marginExVat === undefined ? '-' : eur(detail.financials.marginExVat)}</b></p></div>
        </section>
        <aside className="detail-card"><h2>Customer and shipping</h2><p><b>Customer</b> {detail.customerAndShipping.customer}</p><p><b>Ship to</b> {detail.customerAndShipping.shipTo}</p><p><b>Carrier</b> {detail.customerAndShipping.carrier}</p></aside>
        <aside className="detail-card"><h2>Stock movements</h2>{detail.stockMovements.map(row => <p key={`${row.title}-${row.occurredAt}`}><b>{row.title}</b><small>{row.detail}</small></p>)}</aside>
      </div>
      {cancelOpen && <div className="dialog-backdrop" role="dialog" aria-modal="true" aria-labelledby="cancel-title"><section className="dialog order-cancel-dialog"><h2 id="cancel-title">Cancel order {order.orderNumber}?</h2><p>The order will be marked cancelled and reserved stock is released.</p><label>Reason<select><option>Customer request</option><option>Out of stock</option><option>Address issue</option></select></label><div className="dialog-actions"><button type="button" onClick={() => setCancelOpen(false)}>Keep order</button><button type="button" onClick={() => orderAction('cancel', 'Customer request')}>Cancel order</button></div></section></div>}
    </Shell>
  )
}
