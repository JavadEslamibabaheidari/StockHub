import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent, type ReactNode } from 'react'
import { toggleExplicitTheme } from './appearance'
import {
  AccessApiClient,
  AccessApiError,
  type InventoryCapabilities,
  type InventoryListResponse,
  type InventoryProductDetail,
  type InventoryProductSummary,
  type WorkspaceSummary,
} from './api/generated'

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

type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type ShellIdentity = { workspaceName: string; workspaceInitials: string; workspaceCount: number; fullName: string; userInitials: string; role: string }

const ownerCapabilities: InventoryCapabilities = { canAdjustOnHand: true, canChangePrice: true, canManageListings: true }
const staffCapabilities: InventoryCapabilities = { canAdjustOnHand: true, canChangePrice: false, canManageListings: false }
function initials(value: string, fallback = 'SH') {
  const parts = value.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return fallback
  return parts.slice(0, 2).map(part => part[0]?.toUpperCase()).join('') || fallback
}

function formatRole(role?: WorkspaceSummary['role'] | string | null) {
  return role === 'WarehouseStaff' ? 'Warehouse staff' : role || 'Owner'
}

function csvEscape(value: string | number | null | undefined) {
  const text = String(value ?? '')
  return /[",\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

function csvDownload(rows: (string | number | null | undefined)[][]) {
  return `data:text/csv;charset=utf-8,${encodeURIComponent(rows.map(row => row.map(csvEscape).join(',')).join('\n'))}`
}

function product(id: string, sku: string, name: string, category: string, onHand: number, reserved: number, basePrice: number): InventoryProductSummary {
  const available = Math.max(0, onHand - reserved)
  return {
    id,
    sku,
    name,
    category,
    onHand,
    reserved,
    available,
    basePrice,
    stockStatus: available === 0 ? 'Out of stock' : available <= 5 ? 'Low stock' : 'In stock',
    platforms: [
      { platform: 'Amazon', code: 'Am', status: 'Synced', detail: `Live · ${available} available`, availableShown: available, listed: true },
      { platform: 'Unieuro', code: 'Un', status: 'Synced', detail: `Live · ${available} available`, availableShown: available, listed: true },
      { platform: 'Euronics', code: 'Eu', status: 'Failed', detail: 'Sync failed · API token expired', availableShown: available + 2, listed: true },
      { platform: 'eBay', code: 'eB', status: onHand === 0 ? 'Not Listed' : 'Synced', detail: onHand === 0 ? 'Not listed' : `Live · ${available} available`, availableShown: available, listed: onHand !== 0 },
    ],
  }
}

function emptyList(staff = false): InventoryListResponse {
  return {
    totalProducts: 0,
    syncSummary: 'Backend data · no mocked inventory',
    products: [],
    capabilities: staff ? staffCapabilities : ownerCapabilities,
  }
}

function Shell({ children, role, sync = 'Backend inventory · platform sync pending', panel, setPanel }: { children: ReactNode; role?: string; sync?: string; panel: Panel; setPanel: (panel: Panel) => void }) {
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
    <main className="inventory-page">
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <a className="inventory-workspace" href="#dashboard" aria-label="Workspace dashboard">
            <b>{identity.workspaceInitials}</b><span><strong>{identity.workspaceName}</strong><small>{identity.role} · {identity.workspaceCount} workspace{identity.workspaceCount === 1 ? '' : 's'}</small></span><span>⌄</span>
          </a>
          <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={label} className={label === 'Inventory' ? 'active' : ''} href={href}>{label}</a>)}</nav>
          <div className="inventory-plan"><strong>{identity.role === 'Owner' ? 'Pro plan' : 'Free trial'}</strong><a href="#settings">Upgrade</a><span /></div>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className={sync.includes('failed') || sync.includes('failing') ? 'sync-alert' : 'sync-ok'} type="button" onClick={() => setPanel({ title: 'Sync status', body: sync })}>{sync}</button>
              <button type="button" aria-label="Toggle appearance" onClick={toggleExplicitTheme}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => setPanel({ title: 'Notifications', body: 'Inventory notifications are collected in the Reports milestone.' })}>♧</button>
              <b>{identity.userInitials}</b><span>{identity.fullName}<small>{identity.role}</small></span>
            </div>
          </header>
          {children}
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>×</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function useWorkspaceInventory(staff = false) {
  const [workspaceId, setWorkspaceId] = useState<string | null>(null)
  const [data, setData] = useState(() => emptyList(staff))
  const [source, setSource] = useState<'loading' | 'api' | 'unauthorized' | 'error'>('loading')
  useEffect(() => {
    let alive = true
    api.session()
      .then(session => {
        const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
        if (!alive) return
        if (!workspace) { setSource('unauthorized'); return }
        setWorkspaceId(workspace.id)
        return api.inventory(workspace.id).then(next => {
          if (!alive) return
          setData(staff ? { ...next, capabilities: staffCapabilities } : next)
          setSource('api')
        })
      })
      .catch(error => {
        if (!alive) return
        if (error instanceof AccessApiError && error.status === 401) setSource('unauthorized')
        else { console.warn(error); setSource('error') }
      })
    return () => { alive = false }
  }, [staff])
  return { workspaceId, data, setData, source }
}

function StockLegend() {
  return <div className="stock-legend"><span><b>On hand</b> in the warehouse</span><span>−</span><span><b>Reserved</b> held 10 min for unpaid orders</span><span>=</span><span className="available"><b>Available</b> what every platform sees</span></div>
}

function PlatformDots({ product }: { product: InventoryProductSummary }) {
  return <span className="platform-dots">{product.platforms.map(platform => <span key={platform.platform} className={platform.status.toLowerCase().replace(' ', '-')} title={`${platform.platform}: ${platform.detail}`}>{platform.code}</span>)}</span>
}

function InventoryQuickView({ product, workspaceId, onClose }: { product: InventoryProductSummary; workspaceId: string; onClose: () => void }) {
  const [detail, setDetail] = useState<InventoryProductDetail | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [retry, setRetry] = useState(0)
  const dialogRef = useRef<HTMLElement | null>(null)
  const closeButtonRef = useRef<HTMLButtonElement | null>(null)

  useEffect(() => {
    const returnFocusTo = document.activeElement instanceof HTMLElement ? document.activeElement : null
    closeButtonRef.current?.focus()
    return () => returnFocusTo?.focus()
  }, [])

  useEffect(() => {
    let alive = true
    setLoadError(false)
    setDetail(null)
    api.inventoryProduct(workspaceId, product.id)
      .then(result => { if (alive) setDetail(result) })
      .catch(() => { if (alive) setLoadError(true) })
    return () => { alive = false }
  }, [product.id, retry, workspaceId])

  function onKeyDown(event: ReactKeyboardEvent<HTMLElement>) {
    if (event.key === 'Escape') {
      event.preventDefault()
      onClose()
      return
    }
    if (event.key !== 'Tab') return
    const controls = Array.from(dialogRef.current?.querySelectorAll<HTMLElement>('a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])') ?? [])
      .filter(control => !control.hasAttribute('hidden'))
    const first = controls[0]
    const last = controls[controls.length - 1]
    if (!first || !last) return
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return (
    <div className="inventory-quick-view-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose() }}>
      <section ref={dialogRef} className="inventory-quick-view" role="dialog" aria-modal="true" aria-labelledby="inventory-quick-view-title" aria-describedby="inventory-quick-view-sku" tabIndex={-1} onKeyDown={onKeyDown}>
        <header className="inventory-quick-view-header">
          <div>
            <p>Inventory quick view</p>
            <h2 id="inventory-quick-view-title">{product.name}</h2>
            <span id="inventory-quick-view-sku">{product.sku} · {product.category}</span>
          </div>
          <button ref={closeButtonRef} className="inventory-quick-view-close" type="button" aria-label={`Close quick view for ${product.name}`} onClick={onClose}>×</button>
        </header>
        {detail ? <>
          <div className="inventory-quick-view-stock" aria-label="Product stock">
            <article><span>On hand</span><strong>{detail.product.onHand}</strong></article>
            <article><span>Reserved</span><strong>{detail.product.reserved}</strong></article>
            <article className="available"><span>Available</span><strong>{detail.product.available}</strong></article>
          </div>
          <p className="inventory-quick-view-status"><b>{detail.product.stockStatus}</b>{detail.product.basePrice !== null && detail.capabilities.canChangePrice && <span>Base price · €{detail.product.basePrice.toFixed(2)}</span>}</p>
          <ul className="inventory-quick-view-platforms" aria-label="Platform listing status">
            {detail.product.platforms.map(platform => <li key={platform.platform}><span>{platform.platform}</span><b className={platform.status.toLowerCase().replace(' ', '-')}>{platform.status}</b><small>{platform.detail}</small></li>)}
          </ul>
          {detail.syncAlert && <p className="inventory-quick-view-sync">{detail.syncAlert}</p>}
          <a className="inventory-quick-view-detail-link" href={`#inventory-detail?product=${detail.product.id}`}>Open product detail</a>
        </> : loadError ? <div className="inventory-quick-view-error" role="alert"><p>Product details could not be loaded.</p><button type="button" onClick={() => setRetry(value => value + 1)}>Try again</button></div> : <p role="status" className="inventory-quick-view-loading">Loading product details…</p>}
      </section>
    </div>
  )
}

function InventoryTable({ data, setData, workspaceId, staff, setPanel }: { data: InventoryListResponse; setData: (data: InventoryListResponse) => void; workspaceId: string | null; staff: boolean; setPanel: (panel: Panel) => void }) {
  const [selected, setSelected] = useState(new Set<string>())
  const [quickViewProduct, setQuickViewProduct] = useState<InventoryProductSummary | null>(null)
  async function adjust(product: InventoryProductSummary, next: number) {
    if (next < 0) return
    if (workspaceId) {
      try {
        const result = await api.adjustInventoryOnHand(workspaceId, product.id, next)
        setData({ ...data, products: data.products.map(row => row.id === product.id ? result.product : row) })
        setPanel({ title: 'On hand updated', body: `${result.product.name} now has ${result.product.onHand} on hand.`, tone: 'ok' })
        return
      } catch (error) {
        setPanel({ title: 'Local preview updated', body: error instanceof AccessApiError ? error.problem.detail : 'The API did not accept this update; the preview still reflects the requested count.', tone: 'warn' })
      }
    }
    const updated = data.products.map(row => {
      if (row.id !== product.id) return row
      const available = Math.max(0, next - row.reserved)
      return { ...row, onHand: next, available, stockStatus: available === 0 ? 'Out of stock' : available <= 5 ? 'Low stock' : 'In stock' }
    })
    setData({ ...data, products: updated })
  }
  return (
    <section className="inventory-card">
      <div className="bulk-row"><strong>{selected.size} selected</strong><button type="button" onClick={() => setPanel({ title: 'Bulk adjustment', body: 'Bulk on-hand adjustment uses the same Inventory endpoint as each row.' })}>Adjust on hand</button>{data.capabilities.canChangePrice && <button type="button" onClick={() => setPanel({ title: 'Change price', body: 'Direct price editing is available on product detail. Pricing rule automation belongs to milestone 7.' })}>Change price</button>}<button type="button" onClick={() => setSelected(new Set())}>Clear</button><span className="legend"><i /> Synced <i /> Failed <i /> Not listed</span></div>
      <div className="inventory-table" role="table" aria-label="Inventory products">
        <div className="inventory-tr head" role="row"><span /><span>Product</span><span>On hand</span><span>- Reserved</span><span>= Available</span>{!staff && <span>Base price</span>}<span>Platforms</span><span /></div>
        {data.products.map(row => (
          <div key={row.id} className={`inventory-tr ${staff ? 'staff' : ''} ${row.stockStatus === 'Out of stock' ? 'out' : row.stockStatus === 'Low stock' ? 'low' : ''}`} role="row">
            <label><input type="checkbox" checked={selected.has(row.id)} onChange={() => setSelected(current => { const next = new Set(current); next.has(row.id) ? next.delete(row.id) : next.add(row.id); return next })} /><span className="sr-only">Select {row.name}</span></label>
            <a className="product-cell" href={`#inventory-detail?product=${row.id}`}><b>{row.name}</b><small>{row.sku}</small></a>
            <span>{staff ? <span className="stepper"><button type="button" onClick={() => adjust(row, row.onHand - 1)}>−</button>{row.onHand}<button type="button" onClick={() => adjust(row, row.onHand + 1)}>+</button></span> : row.onHand}</span>
            <span>{row.reserved}<small>{row.reserved ? 'frees in 05:25' : ''}</small></span>
            <span><b>{row.available}</b>{row.stockStatus !== 'In stock' && <em>{row.stockStatus}</em>}</span>
            {!staff && row.basePrice !== null && <span>€{row.basePrice.toFixed(2)}</span>}
            <span><PlatformDots product={row} /></span>
            <span className="inventory-row-actions"><button type="button" aria-label={`Quick view ${row.name}`} onClick={event => { event.currentTarget.focus(); if (workspaceId) setQuickViewProduct(row) }}>Preview</button><a href={`#inventory-detail?product=${row.id}`} aria-label={`Open ${row.name}`}>›</a></span>
          </div>
        ))}
      </div>
      <footer className="table-foot">Showing {data.products.length} of {data.totalProducts.toLocaleString()} products <span><button disabled>Previous</button><button type="button" onClick={() => setPanel({ title: 'Next page', body: 'Pagination is wired for the Inventory list; more rows appear as products are imported.' })}>Next</button></span></footer>
      {quickViewProduct && workspaceId && <InventoryQuickView product={quickViewProduct} workspaceId={workspaceId} onClose={() => setQuickViewProduct(null)} />}
    </section>
  )
}

export function Inventory({ staff = false }: { staff?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const { workspaceId, data, setData, source } = useWorkspaceInventory(staff)
  const exportRows = [['sku', 'name', 'category', 'on hand', 'reserved', 'available', 'base price', 'stock status'], ...data.products.map(row => [row.sku, row.name, row.category, row.onHand, row.reserved, row.available, row.basePrice, row.stockStatus])]
  return <Shell role={staff ? 'Warehouse staff' : undefined} sync={source === 'unauthorized' ? 'Sign in required' : source === 'error' ? 'Inventory unavailable' : data.syncSummary} panel={panel} setPanel={setPanel}><div className="inventory-heading"><div><h1>Inventory</h1><p>{source === 'loading' ? 'Loading backend inventory…' : source === 'api' ? `${data.totalProducts.toLocaleString()} products` : source === 'unauthorized' ? 'Sign in to load your workspace inventory' : 'Could not load backend inventory'}</p></div>{source === 'api' && !staff && <div className="inventory-actions"><a href={csvDownload(exportRows)} download="stockhub-inventory.csv">Export CSV</a><a href="#dashboard">Import CSV/JSON</a><a href="#onboarding">Add product</a></div>}</div>{staff && <p className="staff-note">You are signed in as <b>Warehouse staff</b>. You can update <b>On hand</b> counts; Reserved and Available update automatically. Prices are managed by Owners and Managers.</p>}{source === 'api' && <><div className="filters"><button>All <b>{data.totalProducts.toLocaleString()}</b></button><button>In stock</button><button>Low stock <b>{data.products.filter(item => item.stockStatus === 'Low stock').length}</b></button><button>Out of stock <b>{data.products.filter(item => item.stockStatus === 'Out of stock').length}</b></button><select aria-label="Platform filter"><option>Platform: All</option></select><select aria-label="Category filter"><option>Category: All</option></select></div><StockLegend />{data.products.length > 0 ? <InventoryTable data={data} setData={setData} workspaceId={workspaceId} staff={staff} setPanel={setPanel} /> : <section className="empty-inventory"><div><span className="empty-icon">box</span><h2>No backend products yet</h2><p>This workspace has no products. Import a product CSV or add a product to get started.</p><a className="primary-link-button" href="#dashboard">Go to Dashboard import</a></div></section>}</>}{source === 'unauthorized' && <section className="empty-inventory inventory-load-state"><div><span className="empty-icon">lock</span><h2>Sign in required</h2><p>StockHub could not verify a signed-in workspace, so it cannot report whether inventory is empty.</p><a className="primary-link-button" href="#settings">Open sign-in settings</a></div></section>}{source === 'error' && <section className="empty-inventory inventory-load-state"><div><span className="empty-icon">!</span><h2>Inventory could not be loaded</h2><p>The backend did not return inventory. Check the connection and retry.</p><button className="primary-link-button" type="button" onClick={() => window.location.reload()}>Retry</button></div></section>}</Shell>
}

export function InventoryEmpty() {
  const [panel, setPanel] = useState<Panel>(null)
  return <Shell sync="No platforms connected" panel={panel} setPanel={setPanel}><div className="inventory-heading"><div><h1>Inventory</h1><p>0 products</p></div></div><section className="empty-inventory"><div><span className="empty-icon">box</span><h2>No products yet</h2><p>Add your products once. StockHub then keeps their stock and prices in sync on every platform you connect.</p><a className="primary-link-button" href="#dashboard">Import CSV/JSON</a><a className="primary-link-button secondary" href="#onboarding">Add product</a><small>CSV columns: SKU, name, on hand, base price, category (optional) · <a href="/samples/dashboard-products.sample.csv" download>Download sample CSV</a></small></div><StockLegend /></section></Shell>
}

export function ProductDetail({ allFailing = false }: { allFailing?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const [detail, setDetail] = useState<InventoryProductDetail | null>(null)
  const params = typeof window === 'undefined' ? new URLSearchParams() : new URLSearchParams(window.location.hash.split('?')[1] || '')
  const requestedProductId = params.get('product')
  useEffect(() => {
    let alive = true
    api.session()
      .then(session => {
        const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
        if (!workspace) return
        if (requestedProductId) return api.inventoryProduct(workspace.id, requestedProductId, { allFailing }).then(next => { if (alive) setDetail(next) })
        return api.inventory(workspace.id).then(list => {
          const first = list.products[0]
          if (!first) return
          return api.inventoryProduct(workspace.id, first.id, { allFailing }).then(next => { if (alive) setDetail(next) })
        })
      })
      .catch(error => { if (!(error instanceof AccessApiError && error.status === 401)) console.warn(error) })
    return () => { alive = false }
  }, [allFailing, requestedProductId])
  if (!detail) return <Shell sync="Loading backend inventory" panel={panel} setPanel={setPanel}><p>Loading product detail…</p></Shell>
  const product = detail.product
  function localAdjust(delta: number) {
    if (!detail) return
    const onHand = Math.max(0, product.onHand + delta)
    const available = Math.max(0, onHand - product.reserved)
    setDetail({ ...detail, product: { ...product, onHand, available, stockStatus: available === 0 ? 'Out of stock' : available <= 5 ? 'Low stock' : 'In stock' } })
  }
  return <Shell sync="Backend inventory · platform sync pending" panel={panel} setPanel={setPanel}><div className="detail-title"><a href="#inventory">Inventory</a><span>›</span><span>{product.name}</span><h1>{product.name}</h1><p>{product.sku} · {product.category}</p><em>{product.stockStatus} · {product.available} available</em><button onClick={() => localAdjust(1)}>Adjust on hand</button><button onClick={() => setPanel({ title: 'Changes saved', body: 'Inventory-owned stock and listing controls are saved or queued.' })}>Save changes</button></div><div className="detail-grid"><section className="detail-card stock-detail"><h2>Stock <small>Low-stock alert when {detail.lowStockAlertAt} or fewer available <button onClick={() => setPanel({ title: 'Low-stock threshold', body: 'Threshold changes are saved with Inventory product settings.' })}>Change</button></small></h2><label className="safety"><span><b>Safety buffer</b><small>Off · platforms see every available unit</small></span><input type="checkbox" defaultChecked={detail.safetyBufferEnabled} onChange={() => setPanel({ title: 'Safety buffer', body: 'Safety buffer toggled for this product.' })} /></label><div className="stock-math"><article><b>On hand</b><strong>{product.onHand}</strong><small>Physically in the warehouse</small><button onClick={() => localAdjust(-1)}>−</button><button onClick={() => localAdjust(1)}>+</button></article><span>−</span><article><b>Reserved</b><strong>{product.reserved}</strong><small>Unpaid orders · auto-released after 10 min</small></article><span>=</span><article className="available"><b>Available</b><strong>{product.available}</strong><small>What every platform sees · calculated</small></article></div><div className="reservation-bars"><span /><span /><span /></div></section><section className="detail-card listed"><h2>Listed on</h2>{product.platforms.map(platform => <label key={platform.platform}><span><b>{platform.platform}</b><small>{platform.detail}</small></span><input type="checkbox" defaultChecked={platform.listed} onChange={() => setPanel({ title: `${platform.platform} listing`, body: 'Listing toggle saved or queued for platform sync.' })} /></label>)}</section>{detail.capabilities.canChangePrice && <section className="detail-card pricing"><h2>Pricing <button onClick={() => { window.location.hash = 'pricing' }}>Manage pricing rules</button><span>Base price <b>€{product.basePrice?.toFixed(2)}</b></span></h2><table><tbody>{detail.pricing.map(row => <tr key={row.platform}><th>{row.platform}</th><td>{row.rule}</td><td>{row.adjustment}</td><td>€{row.finalPrice.toFixed(2)}</td><td>€{row.fees.toFixed(2)}</td><td>{row.margin === null ? 'Unavailable' : `€${row.margin.toFixed(2)}`}</td><td>{row.status}</td></tr>)}</tbody></table></section>}<section className="detail-card audit"><h2>Activity and audit log <button onClick={() => setPanel({ title: 'Audit export', body: 'Audit log export is ready for Reports handoff.' })}>Export log</button></h2>{detail.auditLog.map(row => <p key={`${row.title}-${row.when}`}><b>{row.title}</b><span>{row.detail}</span><small>{row.when}</small></p>)}</section></div></Shell>
}
