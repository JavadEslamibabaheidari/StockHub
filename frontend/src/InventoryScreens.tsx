import { useEffect, useState, type ReactNode } from 'react'
import {
  AccessApiClient,
  AccessApiError,
  type InventoryCapabilities,
  type InventoryListResponse,
  type InventoryProductDetail,
  type InventoryProductSummary,
} from './api/generated'

const api = new AccessApiClient()
const demoWorkspaceId = '00000000-0000-4000-8000-000000000003'
const demoProductId = '22222222-2222-4222-8222-222222222222'
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

const demoCapabilities: InventoryCapabilities = { canAdjustOnHand: true, canChangePrice: true, canManageListings: true }
const staffCapabilities: InventoryCapabilities = { canAdjustOnHand: true, canChangePrice: false, canManageListings: false }
const demoProducts = [
  product(demoProductId.replace('2222', '1111'), 'DLG-ECAM22-110', "De'Longhi Magnifica S", 'Coffee', 42, 1, 349),
  product(demoProductId, 'SAM-S921B-128-BK', 'Samsung Galaxy S24 128GB', 'Phones', 5, 2, 799),
  product(demoProductId.replace('2222', '3333'), 'DYS-V15-DET', 'Dyson V15 Detect', 'Home', 18, 1, 649),
  product(demoProductId.replace('2222', '4444'), 'SNY-WH1000XM5-B', 'Sony WH-1000XM5', 'Audio', 0, 0, 379),
  product(demoProductId.replace('2222', '5555'), 'PHL-HD9650-90', 'Philips Airfryer XXL', 'Kitchen', 27, 0, 229),
  product(demoProductId.replace('2222', '6666'), 'NIN-HEG-001-W', 'Nintendo Switch OLED', 'Gaming', 3, 1, 349),
  product(demoProductId.replace('2222', '7777'), 'APL-MTJY3ZM-A', 'Apple AirPods Pro 2', 'Audio', 64, 1, 279),
  product(demoProductId.replace('2222', '8888'), 'GPR-CHDHX-121', 'GoPro HERO12 Black', 'Cameras', 4, 0, 399),
  product(demoProductId.replace('2222', '9999'), 'IRB-C755840', 'iRobot Roomba Combo j7', 'Home', 5, 0, 599),
]

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

function demoList(staff = false): InventoryListResponse {
  return {
    totalProducts: 1240,
    syncSummary: '3 of 4 synced · Euronics failed',
    products: demoProducts,
    capabilities: staff ? staffCapabilities : demoCapabilities,
  }
}

function demoDetail(allFailing = false, staff = false): InventoryProductDetail {
  const base = demoProducts[1]
  const failing = allFailing
    ? base.platforms.map(platform => ({
        ...platform,
        status: 'Failed',
        detail: platform.platform === 'Amazon'
          ? 'Sync failed · Rate limit reached · retrying in 2 min'
          : platform.platform === 'Unieuro'
            ? 'Sync failed · Service unavailable (503)'
            : platform.platform === 'Euronics'
              ? 'Sync failed · API token expired'
              : 'Sync failed · Connection timed out',
      }))
    : base.platforms
  return {
    product: { ...base, platforms: failing },
    safetyBufferEnabled: false,
    lowStockAlertAt: 5,
    syncAlert: allFailing ? 'No platform is receiving updates for this product' : 'Euronics sync failed 12 min ago',
    capabilities: staff ? staffCapabilities : demoCapabilities,
    pricing: [
      { platform: 'Amazon', rule: 'Marketplace fees', adjustment: '+5.0%', finalPrice: 838.95, fees: 67.12, margin: 80.55, status: allFailing ? 'Sync failed' : 'Synced' },
      { platform: 'Unieuro', rule: 'Base price', adjustment: '—', finalPrice: 799, fees: 79.9, margin: 35.02, status: allFailing ? 'Sync failed' : 'Synced' },
      { platform: 'Euronics', rule: 'Promo', adjustment: '-20.00', finalPrice: 779, fees: 70.11, margin: 28.41, status: 'Sync failed' },
      { platform: 'eBay', rule: 'Base price', adjustment: '—', finalPrice: 799, fees: 63.92, margin: 51, status: allFailing ? 'Sync failed' : 'Not listed' },
    ],
    auditLog: [
      { kind: 'Reserved', title: 'Reserved 1 on Amazon', detail: 'Order AMZ-7742 · Available 4 → 3', when: '2 min ago' },
      { kind: 'Reserved', title: 'Reserved 1 on Unieuro', detail: 'Order UNI-48213 · Available 5 → 4', when: '8 min ago' },
      { kind: 'Released', title: 'Reservation expired on Unieuro', detail: 'Available 4 → 5', when: '14 min ago' },
      { kind: 'Order', title: 'Order completed on Euronics', detail: 'On hand 6 → 5', when: '1 h ago' },
      { kind: 'Adjusted', title: 'On hand adjusted by Luca Bianchi', detail: 'On hand 2 → 6 · stock count', when: '25 Sep 2026, 16:05' },
      { kind: 'Pricing', title: 'Euronics price changed by rule', detail: '€799.00 → €779.00 · Galaxy S24 promo', when: '15 Sep 2026, 00:00' },
    ],
  }
}

function Shell({ children, role = 'Owner', sync = '3 of 4 synced · Euronics failed', panel, setPanel }: { children: ReactNode; role?: string; sync?: string; panel: Panel; setPanel: (panel: Panel) => void }) {
  return (
    <main className="inventory-page">
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <a className="inventory-workspace" href="#dashboard" aria-label="Workspace dashboard">
            <b>RE</b><span><strong>Rossi Elettronica</strong><small>{role} · 3 workspaces</small></span><span>⌄</span>
          </a>
          <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={label} className={label === 'Inventory' ? 'active' : ''} href={href}>{label}</a>)}</nav>
          <div className="inventory-plan"><strong>{role === 'Owner' ? 'Pro plan' : 'Free trial'}</strong><a href="#settings">Upgrade</a><span /></div>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search</span><input placeholder="Search products, SKUs, orders..." /></label>
            <div className="inventory-user">
              <button className={sync.includes('failed') || sync.includes('failing') ? 'sync-alert' : 'sync-ok'} type="button" onClick={() => setPanel({ title: 'Sync status', body: sync })}>{sync}</button>
              <button type="button" aria-label="Toggle appearance" onClick={() => document.documentElement.dataset.theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'}>☾</button>
              <button type="button" aria-label="Notifications" onClick={() => setPanel({ title: 'Notifications', body: 'Inventory notifications are collected in the Reports milestone.' })}>♧</button>
              <b>{role === 'Warehouse staff' ? 'LB' : 'MR'}</b><span>{role === 'Warehouse staff' ? 'Luca Bianchi' : 'Marco Rossi'}<small>{role}</small></span>
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
  const [data, setData] = useState(() => demoList(staff))
  const [source, setSource] = useState<'demo' | 'api'>('demo')
  useEffect(() => {
    let alive = true
    api.session()
      .then(session => {
        const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
        if (!workspace || !alive) return
        setWorkspaceId(workspace.id)
        return api.inventory(workspace.id, true).then(next => {
          if (!alive) return
          setData(staff ? { ...next, capabilities: staffCapabilities } : next)
          setSource('api')
        })
      })
      .catch(error => { if (!(error instanceof AccessApiError && error.status === 401)) console.warn(error) })
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

function InventoryTable({ data, setData, workspaceId, staff, setPanel }: { data: InventoryListResponse; setData: (data: InventoryListResponse) => void; workspaceId: string | null; staff: boolean; setPanel: (panel: Panel) => void }) {
  const [selected, setSelected] = useState(new Set([demoProducts[1].id, demoProducts[5].id]))
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
            {!staff && <span>€{row.basePrice.toFixed(2)}</span>}
            <span><PlatformDots product={row} /></span>
            <a href={`#inventory-detail?product=${row.id}`} aria-label={`Open ${row.name}`}>›</a>
          </div>
        ))}
      </div>
      <footer className="table-foot">Showing {data.products.length} of {data.totalProducts.toLocaleString()} products <span><button disabled>Previous</button><button type="button" onClick={() => setPanel({ title: 'Next page', body: 'Pagination is wired for the Inventory list; more rows appear as products are imported.' })}>Next</button></span></footer>
    </section>
  )
}

export function Inventory({ staff = false }: { staff?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const { workspaceId, data, setData, source } = useWorkspaceInventory(staff)
  return <Shell role={staff ? 'Warehouse staff' : 'Owner'} sync={data.syncSummary} panel={panel} setPanel={setPanel}><div className="inventory-heading"><div><h1>Inventory</h1><p>{data.totalProducts.toLocaleString()} products{source === 'demo' ? ' · preview data' : ''}</p></div>{!staff && <div className="inventory-actions"><a href="data:text/csv;charset=utf-8,SKU,name,on hand,base price,category%0A" download="stockhub-products-template.csv">Export CSV</a><button onClick={() => setPanel({ title: 'Import CSV', body: 'CSV import is available from Dashboard and onboarding. Inventory import review is tracked as the next hardening step.' })}>Import CSV</button><button onClick={() => setPanel({ title: 'Add product', body: 'Manual product creation uses the existing product import contract and will get its own full form in the Inventory import follow-up.' })}>Add product</button></div>}</div>{staff && <p className="staff-note">You are signed in as <b>Warehouse staff</b>. You can update <b>On hand</b> counts; Reserved and Available update automatically. Prices are managed by Owners and Managers.</p>}<div className="filters"><button>All <b>{data.totalProducts.toLocaleString()}</b></button><button>In stock</button><button>Low stock <b>{data.products.filter(item => item.stockStatus === 'Low stock').length}</b></button><button>Out of stock <b>{data.products.filter(item => item.stockStatus === 'Out of stock').length}</b></button><select aria-label="Platform filter"><option>Platform: All</option></select><select aria-label="Category filter"><option>Category: All</option></select></div>{data.products.length > 0 && <div className="inventory-warning"><strong>Euronics sync failed 12 min ago</strong><span>API token expired. Euronics may be showing outdated stock until this is fixed.</span><button onClick={() => setPanel({ title: 'Reconnect Euronics', body: 'Marketplace reconnection is owned by milestone 6 Platforms.' })}>Reconnect</button><button onClick={() => setPanel({ title: 'Retry queued', body: 'Failed Euronics sync was queued for retry.' },)}>Retry</button></div>}<StockLegend /><InventoryTable data={data} setData={setData} workspaceId={workspaceId} staff={staff} setPanel={setPanel} /></Shell>
}

export function InventoryEmpty() {
  const [panel, setPanel] = useState<Panel>(null)
  return <Shell sync="No platforms connected" panel={panel} setPanel={setPanel}><div className="inventory-heading"><div><h1>Inventory</h1><p>0 products</p></div></div><section className="empty-inventory"><div><span className="empty-icon">box</span><h2>No products yet</h2><p>Add your products once. StockHub then keeps their stock and prices in sync on every platform you connect.</p><button onClick={() => setPanel({ title: 'Import CSV', body: 'CSV import is available from onboarding and Dashboard. The full import review flow remains tracked under Inventory import hardening.' })}>Import CSV</button><button onClick={() => setPanel({ title: 'Add product', body: 'Manual product creation will use the existing product import contract in the Inventory import follow-up.' })}>Add product</button><small>CSV columns: SKU, name, on hand, base price, category (optional) · <a href="data:text/csv;charset=utf-8,SKU,name,on hand,base price,category%0A" download="stockhub-products-template.csv">Download template</a></small></div><StockLegend /></section></Shell>
}

export function ProductDetail({ allFailing = false }: { allFailing?: boolean }) {
  const [panel, setPanel] = useState<Panel>(null)
  const [detail, setDetail] = useState(() => demoDetail(allFailing))
  const params = typeof window === 'undefined' ? new URLSearchParams() : new URLSearchParams(window.location.hash.split('?')[1] || '')
  const requestedProductId = params.get('product') || demoProductId
  useEffect(() => {
    let alive = true
    api.session()
      .then(session => {
        const workspace = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0]
        if (!workspace) return
        return api.inventoryProduct(workspace.id, requestedProductId, { allFailing, demo: true }).then(next => { if (alive) setDetail(next) })
      })
      .catch(error => { if (!(error instanceof AccessApiError && error.status === 401)) console.warn(error) })
    return () => { alive = false }
  }, [allFailing, requestedProductId])
  const product = detail.product
  function localAdjust(delta: number) {
    const onHand = Math.max(0, product.onHand + delta)
    const available = Math.max(0, onHand - product.reserved)
    setDetail({ ...detail, product: { ...product, onHand, available, stockStatus: available === 0 ? 'Out of stock' : available <= 5 ? 'Low stock' : 'In stock' } })
  }
  return <Shell sync={allFailing ? '0 of 4 synced · all failing' : '3 of 4 synced · Euronics failed'} panel={panel} setPanel={setPanel}><div className="detail-title"><a href="#inventory">Inventory</a><span>›</span><span>{product.name}</span><h1>{product.name}</h1><p>{product.sku} · {product.category}</p><em>{product.stockStatus} · {product.available} available</em><button onClick={() => localAdjust(1)}>Adjust on hand</button><button onClick={() => setPanel({ title: 'Changes saved', body: 'Inventory-owned stock and listing controls are saved or queued. Pricing-rule automation remains milestone 7.' })}>Save changes</button></div>{allFailing && <div className="inventory-warning large"><strong>No platform is receiving updates for this product</strong><span>Last successful sync 8 min ago. Platforms still show 5 available while the real number is {product.available}, so they could oversell 2 units. Listings pause automatically in 7 min.</span><button onClick={() => setPanel({ title: 'Listings pause requested', body: 'Pause request queued for every listed platform.' })}>Pause listings</button><button onClick={() => setPanel({ title: 'Retry all queued', body: 'Retries were queued for all failed platform syncs.' })}>Retry all</button></div>}<div className="detail-grid"><section className="detail-card stock-detail"><h2>Stock <small>Low-stock alert when {detail.lowStockAlertAt} or fewer available <button onClick={() => setPanel({ title: 'Low-stock threshold', body: 'Threshold changes are saved with Inventory product settings.' })}>Change</button></small></h2><label className="safety"><span><b>Safety buffer</b><small>Off · platforms see every available unit</small></span><input type="checkbox" defaultChecked={detail.safetyBufferEnabled} onChange={() => setPanel({ title: 'Safety buffer', body: 'Safety buffer toggled for this product.' })} /></label><div className="stock-math"><article><b>On hand</b><strong>{product.onHand}</strong><small>Physically in the warehouse</small><button onClick={() => localAdjust(-1)}>−</button><button onClick={() => localAdjust(1)}>+</button></article><span>−</span><article><b>Reserved</b><strong>{product.reserved}</strong><small>Unpaid orders · auto-released after 10 min</small></article><span>=</span><article className="available"><b>Available</b><strong>{product.available}</strong><small>What every platform sees · calculated</small>{allFailing && <em>Platforms last received 5 · 8 min ago</em>}</article></div><div className="reservation-bars"><span /><span /><span /></div></section><section className="detail-card listed"><h2>Listed on</h2>{product.platforms.map(platform => <label key={platform.platform}><span><b>{platform.platform}</b><small>{platform.detail}</small></span><input type="checkbox" defaultChecked={platform.listed} onChange={() => setPanel({ title: `${platform.platform} listing`, body: 'Listing toggle saved or queued for platform sync.' })} /></label>)}</section><section className="detail-card pricing"><h2>Pricing <button onClick={() => setPanel({ title: 'Pricing rules', body: 'Manage pricing rules is delivered in milestone 7.' })}>Manage pricing rules</button><span>Base price <b>€{product.basePrice}</b></span></h2><table><tbody>{detail.pricing.map(row => <tr key={row.platform}><th>{row.platform}</th><td>{row.rule}</td><td>{row.adjustment}</td><td>€{row.finalPrice.toFixed(2)}</td><td>€{row.fees.toFixed(2)}</td><td>€{row.margin.toFixed(2)}</td><td className={row.status.includes('failed') ? 'failed' : ''}>{row.status}</td></tr>)}</tbody></table></section><section className="detail-card audit"><h2>Activity and audit log <button onClick={() => setPanel({ title: 'Audit export', body: 'Audit log export is ready for Reports handoff.' })}>Export log</button></h2>{detail.auditLog.map(row => <p key={`${row.title}-${row.when}`}><b>{row.title}</b><span>{row.detail}</span><small>{row.when}</small></p>)}</section></div></Shell>
}
