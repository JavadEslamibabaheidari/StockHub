import { useMemo, useState, type ReactNode } from 'react'

type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type OrderStatus = 'Awaiting payment' | 'Paid - to pick' | 'Picked - to ship' | 'Shipped' | 'Delivered' | 'Return requested' | 'Cancelled'
type Order = {
  id: string
  product: string
  sku: string
  platform: string
  platformCode: string
  qty: number
  status: OrderStatus
  total: number
  fee: number
  net: number
  margin: number | null
  customer: string
  age: string
  next: string
}
type FilterTab = 'All' | 'Awaiting payment' | 'To pick' | 'To ship' | 'Shipped' | 'Returns' | 'Cancelled'
type ReturnStep = 'requested' | 'approved' | 'received' | 'restocked' | 'refunded' | 'rejected'

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

const initialOrders: Order[] = [
  order('#AMZ-7742', 'Samsung Galaxy S24 128GB', 'SAM-S921B-128-BK', 'Amazon', 'Am', 1, 'Awaiting payment', 838.95, 67.12, 771.83, 80.55, 'M. Greco', '2 min ago', 'Held until payment'),
  order('#UN-48213', 'Samsung Galaxy S24 128GB', 'SAM-S921B-128-BK', 'Unieuro', 'Un', 1, 'Awaiting payment', 799, 79.9, 719.1, 35.02, 'A. Russo', '8 min ago', 'Held until payment'),
  order('#AMZ-7728', "De'Longhi Magnifica S", 'DLG-ECAM22-110', 'Amazon', 'Am', 1, 'Paid - to pick', 366.45, 29.32, 337.13, 35.05, 'S. Romano', '31 min ago', 'Pick'),
  order('#UN-48201', 'Philips Airfryer XXL', 'PHL-HD9650-90', 'Unieuro', 'Un', 2, 'Picked - to ship', 458, 45.8, 412.2, 19.61, 'A. Ferri', '46 min ago', 'Mark shipped'),
  order('#EU-11872', 'Samsung Galaxy S24 128GB', 'SAM-S921B-128-BK', 'Euronics', 'Eu', 1, 'Shipped', 779, 70.11, 708.89, 28.41, 'G. Esposito', '1 h ago', 'Tracking'),
  order('#EB-2279', 'Sony WH-1000XM5', 'SNY-WH1000XM5-B', 'eBay', 'eB', 1, 'Delivered', 389, 31.12, 357.88, 31.73, 'N. Conti', '3 h ago', 'Done'),
  order('#AMZ-7690', 'Dyson V15 Detect', 'DYS-V15-DET', 'Amazon', 'Am', 1, 'Return requested', 681.45, 54.52, 626.93, 65.05, 'L. Ferrari', '21 Sep 2026, 18:02', 'Receive return'),
  order('#EB-2270', 'Apple AirPods Pro 2', 'APL-MTJY3ZM-A', 'eBay', 'eB', 1, 'Cancelled', 289, 23.12, 265.88, null, 'P. Gallo', '5 h ago', 'Nothing to do'),
]

function order(id: string, product: string, sku: string, platform: string, platformCode: string, qty: number, status: OrderStatus, total: number, fee: number, net: number, margin: number | null, customer: string, age: string, next: string): Order {
  return { id, product, sku, platform, platformCode, qty, status, total, fee, net, margin, customer, age, next }
}

function eur(value: number) {
  return `€${value.toFixed(2)}`
}

function csvEscape(value: string | number | null) {
  const text = value === null ? '' : String(value)
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

function csvDownload(rows: (string | number | null)[][]) {
  return `data:text/csv;charset=utf-8,${encodeURIComponent(rows.map(row => row.map(csvEscape).join(',')).join('\n'))}`
}

function Shell({
  children,
  role = 'Owner',
  sync = '3 of 4 synced · Euronics failed',
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
  return (
    <main className="inventory-page orders-page">
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <a className="inventory-workspace" href="#dashboard" aria-label="Workspace dashboard">
            <b>RE</b><span><strong>Rossi Elettronica</strong><small>{role} · {role === 'Owner' ? '3' : '1'} workspaces</small></span><span>⌄</span>
          </a>
          <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={label} className={label === 'Orders' ? 'active' : ''} href={href}>{label}</a>)}</nav>
          <div className="inventory-plan"><strong>{role === 'Owner' ? 'Pro plan' : 'Free trial'}</strong><a href="#settings">Upgrade</a><span /></div>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input value={search || ''} onChange={event => setSearch?.(event.target.value)} placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className={sync.includes('failed') ? 'sync-alert' : 'sync-ok'} type="button" onClick={() => setPanel({ title: 'Sync status', body: sync })}>{sync}</button>
              <button type="button" aria-label="Toggle appearance" onClick={() => document.documentElement.dataset.theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => setPanel({ title: 'Notifications', body: '5 order events need review today.' })}>!</button>
              <b>{role === 'Warehouse staff' ? 'LB' : 'MR'}</b><span>{role === 'Warehouse staff' ? 'Luca Bianchi' : 'Marco Rossi'}<small>{role}</small></span>
            </div>
          </header>
          {children}
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>x</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function statusForTab(orderStatus: OrderStatus): FilterTab {
  if (orderStatus === 'Paid - to pick') return 'To pick'
  if (orderStatus === 'Picked - to ship') return 'To ship'
  if (orderStatus === 'Return requested') return 'Returns'
  if (orderStatus === 'Cancelled') return 'Cancelled'
  if (orderStatus === 'Awaiting payment') return 'Awaiting payment'
  return 'Shipped'
}

function statusClass(status: OrderStatus) {
  return status.toLowerCase().replace(/\s+/g, '-')
}

function OrderStatusPill({ status }: { status: OrderStatus }) {
  return <span className={`order-status ${statusClass(status)}`}>{status}</span>
}

function PlatformBadge({ code, name }: { code: string; name: string }) {
  return <span className="order-platform" title={name}>{code}</span>
}

function OrdersTable({ rows, staff, setRows, setPanel }: { rows: Order[]; staff?: boolean; setRows: (rows: Order[]) => void; setPanel: (panel: Panel) => void }) {
  function updateStatus(id: string, status: OrderStatus, next: string) {
    setRows(rows.map(row => row.id === id ? { ...row, status, next } : row))
    setPanel({ title: 'Order updated', body: `${id} is now ${status}.`, tone: 'ok' })
  }

  function staffAction(row: Order) {
    if (row.status === 'Paid - to pick') return <button type="button" onClick={() => updateStatus(row.id, 'Picked - to ship', 'Mark shipped')}>Pick</button>
    if (row.status === 'Picked - to ship') return <button type="button" onClick={() => updateStatus(row.id, 'Shipped', 'Tracking')}>Mark shipped</button>
    if (row.status === 'Shipped') return <button type="button" onClick={() => setPanel({ title: 'Tracking opened', body: `${row.id} is in transit with carrier tracking visible to the customer.` })}>Tracking</button>
    if (row.status === 'Return requested') return <button type="button" onClick={() => updateStatus(row.id, 'Delivered', 'Return received')}>Receive return</button>
    return <span>{row.next}</span>
  }

  return (
    <section className="inventory-card orders-card">
      <div className={`orders-table ${staff ? 'staff' : ''}`} role="table" aria-label="Orders">
        <div className="orders-tr head" role="row">
          <span>Order</span><span>Product</span><span>Platform</span><span>Qty</span><span>Status</span>{staff ? <span>Next step</span> : <><span>Total</span><span>Fee</span><span>Net</span><span>Margin</span></>}<span />
        </div>
        {rows.map(row => (
          <div className="orders-tr" role="row" key={row.id}>
            <a href={`#order-detail?order=${encodeURIComponent(row.id)}`}><b>{row.id}</b><small>{row.age}</small></a>
            <span><b>{row.product}</b><small>{row.sku}</small></span>
            <span><PlatformBadge code={row.platformCode} name={row.platform} /> {row.platform}</span>
            <span>{row.qty}</span>
            <span><OrderStatusPill status={row.status} /></span>
            {staff ? <span className="next-action">{staffAction(row)}</span> : <><span><b>{eur(row.total)}</b></span><span>{eur(row.fee)}</span><span>{eur(row.net)}</span><span>{row.margin === null ? '-' : <b>{eur(row.margin)}</b>}</span></>}
            <a href={`#order-detail?order=${encodeURIComponent(row.id)}`} aria-label={`Open ${row.id}`}>›</a>
          </div>
        ))}
      </div>
      <footer className="table-foot">Showing {rows.length} of 37 orders today <span><button disabled>Previous</button><button type="button" onClick={() => setPanel({ title: 'No more preview rows', body: 'All loaded Orders preview rows are visible.' })}>Next</button></span></footer>
    </section>
  )
}

function useOrders(initial = initialOrders) {
  const [rows, setRows] = useState(initial)
  return { rows, setRows }
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
}: {
  tab: FilterTab
  setTab: (tab: FilterTab) => void
  platform: string
  setPlatform: (platform: string) => void
  date: string
  setDate: (date: string) => void
  search: string
  setSearch: (value: string) => void
}) {
  const tabs: FilterTab[] = ['All', 'Awaiting payment', 'To pick', 'To ship', 'Shipped', 'Returns', 'Cancelled']
  return (
    <>
      <div className="filters orders-filters">
        {tabs.map(item => <button type="button" key={item} className={tab === item ? 'selected' : ''} onClick={() => setTab(item)}>{item} <b>{item === 'All' ? 8 : initialOrders.filter(row => statusForTab(row.status) === item).length}</b></button>)}
        <select aria-label="Platform filter" value={platform} onChange={event => setPlatform(event.target.value)}><option>All</option><option>Amazon</option><option>Unieuro</option><option>Euronics</option><option>eBay</option></select>
        <select aria-label="Date filter" value={date} onChange={event => setDate(event.target.value)}><option>Today</option><option>This week</option><option>This month</option></select>
      </div>
      <label className="order-inline-search"><span className="sr-only">Order search</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Order number or product" /></label>
    </>
  )
}

export function Orders({ staff = false, noResults = false }: { staff?: boolean; noResults?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const { rows, setRows } = useOrders()
  const [search, setSearch] = useState(noResults ? 'XM5' : '')
  const [tab, setTab] = useState<FilterTab>(noResults ? 'Returns' : 'All')
  const [platform, setPlatform] = useState(noResults ? 'Euronics' : 'All')
  const [date, setDate] = useState('Today')
  const filtered = rows.filter(row => {
    const q = search.trim().toLowerCase()
    const matchesTab = tab === 'All' || statusForTab(row.status) === tab
    const matchesPlatform = platform === 'All' || row.platform === platform
    const matchesSearch = !q || [row.id, row.product, row.sku, row.customer].some(value => value.toLowerCase().includes(q))
    return matchesTab && matchesPlatform && matchesSearch
  })
  const exportRows = [['order', 'product', 'platform', 'qty', 'status', 'total', 'fee', 'net', 'margin'], ...filtered.map(row => [row.id, row.product, row.platform, row.qty, row.status, row.total, row.fee, row.net, row.margin])]
  function clearFilters() {
    setSearch('')
    setTab('All')
    setPlatform('All')
    setDate('Today')
  }
  return (
    <Shell role={staff ? 'Warehouse staff' : 'Owner'} panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="inventory-heading">
        <div><h1>Orders</h1><p>37 today · 219 this week · synced from Amazon, Unieuro, Euronics and eBay</p></div>
        {staff ? <div className="inventory-actions"><button type="button" onClick={() => setPanel({ title: 'Picking list ready', body: `${rows.filter(row => row.status === 'Paid - to pick').length} paid orders are ready to pick.`, tone: 'ok' })}>Print picking list ({rows.filter(row => row.status === 'Paid - to pick').length})</button></div> : <div className="inventory-actions"><a href={csvDownload(exportRows)} download="stockhub-orders.csv">Export CSV</a></div>}
      </div>
      {staff && <p className="staff-note">You are signed in as <b>Warehouse staff</b>. You can pick, pack, ship and receive returns. Totals, fees, margins and refunds are handled by Owners and Managers.</p>}
      <OrdersFilters tab={tab} setTab={setTab} platform={platform} setPlatform={setPlatform} date={date} setDate={setDate} search={search} setSearch={setSearch} />
      {filtered.length === 0 ? <section className="orders-no-results"><span>Search</span><h2>No orders match these filters</h2><p>Status: {tab} · Platform: {platform} · Search: "{search}"</p><button type="button" onClick={clearFilters}>Clear filters</button></section> : <OrdersTable rows={filtered} staff={staff} setRows={nextRows => setRows(rows.map(row => nextRows.find(next => next.id === row.id) || row))} setPanel={setPanel} />}
    </Shell>
  )
}

export function OrdersEmpty() {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  return (
    <Shell sync="No platforms connected" panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="inventory-heading"><div><h1>Orders</h1><p>0 orders</p></div></div>
      <section className="empty-inventory orders-empty">
        <div><span className="empty-icon">cart</span><h2>No orders yet</h2><p>Orders from your marketplaces appear here as soon as a customer books a product. Connect a platform to start receiving them.</p><a className="primary-link-button" href="#platforms">Connect a platform</a></div>
        <div className="order-how"><h3>How an order moves</h3><p><b>Awaiting payment</b><span>Reserved for 10 min · Available -1</span></p><p><b>Paid</b><span>On hand -1 · ready to pick</span></p><p><b>Shipped</b><span>Tracking sent to the platform</span></p></div>
      </section>
    </Shell>
  )
}

function detailOrder(kind: 'normal' | 'return' | 'cancel') {
  if (kind === 'return') return initialOrders[6]
  if (kind === 'cancel') return initialOrders[2]
  return initialOrders[4]
}

function Timeline({ status, returnMode = false }: { status: OrderStatus; returnMode?: boolean }) {
  const labels = returnMode ? ['Reserved', 'Paid', 'Picked', 'Shipped', 'Delivered'] : ['Reserved', 'Paid', 'Picked', 'Shipped', 'Delivered']
  const active = status === 'Paid - to pick' ? 2 : status === 'Picked - to ship' ? 3 : status === 'Shipped' ? 4 : status === 'Delivered' || status === 'Return requested' ? 5 : 1
  return <div className="order-timeline">{labels.map((label, index) => <span key={label} className={index < active ? 'done' : ''}><b>{label}</b><small>{index < active ? index === 0 ? '1 h ago' : index === 4 ? '23 Sep 2026, 15:20' : `${index * 21} min ago` : index === 4 ? 'Expected 29 Sep 2026' : '-'}</small></span>)}</div>
}

function ReturnBox({ step, setStep, setPanel }: { step: ReturnStep; setStep: (step: ReturnStep) => void; setPanel: (panel: Panel) => void }) {
  const steps: ReturnStep[] = ['requested', 'approved', 'received', 'restocked', 'refunded']
  function advance(next: ReturnStep) {
    setStep(next)
    setPanel({ title: 'Return updated', body: `Return is now ${next}.`, tone: 'ok' })
  }
  return (
    <section className="return-box">
      <h2>Return <small>Requested 25 Sep 2026, 10:14 · reason: "Battery doesn't hold charge"</small></h2>
      <div className="return-steps">{steps.map(item => <span key={item} className={steps.indexOf(item) <= steps.indexOf(step === 'rejected' ? 'requested' : step) ? 'done' : ''}>{item === 'restocked' ? 'Restock or write off' : item[0].toUpperCase() + item.slice(1)}</span>)}</div>
      {step === 'requested' && <p>Approve to send the customer an Amazon return label. Reject if the item is outside the 30-day return window or was not sold by you.</p>}
      {step === 'approved' && <p>Waiting for the warehouse to receive and inspect the returned item.</p>}
      {step === 'received' && <p>Returned item received. Choose whether it can be restocked or written off.</p>}
      {step === 'restocked' && <p>Restocked · On hand 18 to 19 · Available 17 to 18 on all platforms.</p>}
      {step === 'refunded' && <p>Refund sent to the customer and recorded in the order margin.</p>}
      {step === 'rejected' && <p>Return rejected. No stock or refund change was made.</p>}
      <div className="dialog-actions">
        {step === 'requested' && <><button type="button" onClick={() => advance('rejected')}>Reject</button><button type="button" onClick={() => advance('approved')}>Approve return</button></>}
        {step === 'approved' && <button type="button" onClick={() => advance('received')}>Receive item</button>}
        {step === 'received' && <><button type="button" onClick={() => advance('restocked')}>Restock</button><button type="button" onClick={() => advance('restocked')}>Write off</button></>}
        {step === 'restocked' && <button type="button" onClick={() => advance('refunded')}>Refund customer</button>}
        {(step === 'refunded' || step === 'rejected') && <button type="button" onClick={() => advance('requested')}>Reset preview</button>}
      </div>
    </section>
  )
}

export function OrderDetail({ mode = 'normal' }: { mode?: 'normal' | 'return' | 'cancel' }) {
  const [panel, setPanel] = useState<Panel>(null)
  const [search, setSearch] = useState('')
  const [returnStep, setReturnStep] = useState<ReturnStep>('requested')
  const [cancelOpen, setCancelOpen] = useState(mode === 'cancel')
  const [cancelled, setCancelled] = useState(false)
  const order = useMemo(() => detailOrder(mode), [mode])
  const currentStatus: OrderStatus = cancelled ? 'Cancelled' : mode === 'return' ? 'Return requested' : order.status
  function printInvoice() {
    setPanel({ title: 'Invoice opened', body: `${order.id} invoice is ready in the browser print dialog.`, tone: 'ok' })
    if (typeof window !== 'undefined') window.print()
  }
  function cancelOrder() {
    setCancelled(true)
    setCancelOpen(false)
    setPanel({ title: 'Order cancelled', body: `${order.id} was cancelled and 1 unit returned to Available on all platforms.`, tone: 'ok' })
  }
  return (
    <Shell panel={panel} setPanel={setPanel} search={search} setSearch={setSearch}>
      <div className="detail-title order-detail-title">
        <a href="#orders">Orders</a><span>›</span><span>{order.id}</span><h1>Order {order.id}</h1><OrderStatusPill status={currentStatus} />
        <p>{order.platform} · placed {order.age} · {order.customer}</p>
        <button type="button" onClick={printInvoice}>Print invoice</button>
        {mode === 'normal' && <button type="button" onClick={() => window.location.hash = '#order-return'}>Start return</button>}
        {mode === 'cancel' || order.status === 'Paid - to pick' ? <button type="button" className="danger-button" onClick={() => setCancelOpen(true)}>Cancel order</button> : <button type="button" onClick={() => { setReturnStep('refunded'); setPanel({ title: 'Refund recorded', body: `${order.id} refund was recorded in Orders preview.`, tone: 'ok' }) }}>Refund</button>}
      </div>
      <Timeline status={currentStatus} returnMode={mode === 'return'} />
      {mode === 'return' && <ReturnBox step={returnStep} setStep={setReturnStep} setPanel={setPanel} />}
      <div className="detail-grid order-detail-grid">
        <section className="detail-card order-items"><h2>Items</h2><div className="mini-table"><b>Product</b><b>Qty</b><b>Unit price</b><b>Total</b><span>{order.product}<small>{order.sku}</small></span><span>{order.qty}</span><span>{eur(order.total)}</span><strong>{eur(order.total)}</strong></div><div className="order-totals"><p><span>Total paid by customer</span><b>{eur(order.total)}</b></p><p><span>incl. VAT 22%</span><span>{eur(order.total * 0.18)}</span></p><p><span>{order.platform} fee</span><span>-{eur(order.fee)}</span></p><p><span>Net payout</span><b>{eur(order.net)}</b></p><p><span>Cost of goods</span><span>-{eur(order.status === 'Return requested' ? 439 : 540)}</span></p><p><span>Margin (ex VAT)</span><b>{order.margin === null ? '-' : eur(order.margin)}</b></p></div></section>
        <aside className="detail-card"><h2>Customer and shipping</h2><p><b>Customer</b> {order.customer}</p><p><b>Ship to</b> {mode === 'return' ? 'Corso Buenos Aires 33, 20124 Milano' : 'Via Toledo 214, 80134 Napoli'}</p><p><b>Carrier</b> {mode === 'cancel' ? 'BRT · label not printed yet' : 'BRT · 04217788 2951 · in transit'}</p></aside>
        <aside className="detail-card"><h2>Stock movements</h2><p><b>Reserved 1 unit</b><small>1 h ago · Available -1</small></p><p><b>Paid</b><small>1 h ago · On hand -1 · Reserved -1</small></p>{mode === 'return' && <p><b>Return requested</b><small>25 Sep 2026, 10:14 · no stock change until received</small></p>}{cancelled && <p><b>Cancelled</b><small>just now · Available +1</small></p>}</aside>
      </div>
      {cancelOpen && <div className="dialog-backdrop" role="dialog" aria-modal="true" aria-labelledby="cancel-title"><section className="dialog order-cancel-dialog"><h2 id="cancel-title">Cancel order {order.id}?</h2><p>The customer is refunded {eur(order.total)} through {order.platform} and the order is marked cancelled on the platform.</p><div className="cancel-stock-note"><b>1 unit will return to Available on all platforms</b><span>{order.product} · On hand 42 to 43 · Available 41 to 42 on Amazon, Unieuro, Euronics and eBay</span></div><label>Reason<select><option>Customer request</option><option>Out of stock</option><option>Address issue</option></select></label><div className="dialog-actions"><button type="button" onClick={() => setCancelOpen(false)}>Keep order</button><button type="button" onClick={cancelOrder}>Cancel order</button></div></section></div>}
    </Shell>
  )
}
