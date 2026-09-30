import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { AccessApiClient, AccessApiError, type ProductRequest, type ProductResponse, type SessionResponse, type WorkspaceSummary } from './api/generated'

const api = new AccessApiClient()
const go = (route: string) => { window.location.hash = route }
const newRequestKey = () => `request-${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`
const message = (error: unknown, fallback: string) => error instanceof AccessApiError ? error.problem.detail || fallback : fallback

function Logo() {
  return <a className="brand access-brand" href="#cover" aria-label="StockHub home"><span className="brand-mark" aria-hidden="true"><span /></span><span className="brand-name">StockHub</span></a>
}

function Frame({ children, kind, topRight }: { children: ReactNode; kind: string; topRight?: ReactNode }) {
  return <main className={`access-page ${kind}`}><div className="access-panel"><header className="access-header"><Logo />{topRight}</header>{children}</div></main>
}

function ErrorText({ value }: { value: string | null }) { return value && <p className="form-error" role="alert">{value}</p> }

function GoogleButton() {
  const [available, setAvailable] = useState<boolean | null>(null)
  useEffect(() => {
    let active = true
    fetch('/api/auth/google/availability')
      .then(response => response.ok ? response.json() : { available: false })
      .then(data => { if (active) setAvailable(data.available === true) })
      .catch(() => { if (active) setAvailable(false) })
    return () => { active = false }
  }, [])
  return <>{available ? <a className="google-button" href="/api/auth/google/start"><span className="google-g" aria-hidden="true">G</span>Continue with Google</a> : <button className="google-button" type="button" disabled><span className="google-g" aria-hidden="true">G</span>Continue with Google</button>}{available === false && <p className="field-hint" role="status">Google sign-in is unavailable while its provider is being configured.</p>}<div className="form-divider"><span>or with email</span></div></>
}

function PasswordInput({ value, change, autoComplete }: { value: string; change: (value: string) => void; autoComplete: string }) {
  const [visible, setVisible] = useState(false)
  return <span className="password-input"><input required minLength={autoComplete === 'new-password' ? 8 : undefined} type={visible ? 'text' : 'password'} value={value} onChange={event => change(event.target.value)} autoComplete={autoComplete} /><button type="button" aria-label={visible ? 'Hide password' : 'Show password'} aria-pressed={visible} onClick={() => setVisible(!visible)}>{visible ? 'Hide' : 'Show'}</button></span>
}

export function SignUp() {
  const [fullName, setFullName] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null); const [created, setCreated] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    let accountCreated = false
    try {
      await api.signUp({ fullName: fullName.trim(), email: email.trim(), password })
      accountCreated = true
      setCreated(true)
      const pendingInvite = sessionStorage.getItem('stockhub-pending-invite')
      if (pendingInvite) { sessionStorage.removeItem('stockhub-pending-invite'); go(`accept?token=${pendingInvite}`); return }
      await api.session()
      go('workspace')
    }
    catch (caught) {
      setError(accountCreated
        ? 'Your account was created, but the session could not be confirmed. Please sign in.'
        : message(caught, 'We could not create your account. Try again.'))
    }
    finally { setBusy(false) }
  }
  return <Frame kind="auth signup" topRight={<span className="step-label">Step 1 of 2</span>}><div className="auth-grid"><section className="auth-main"><div className="auth-content"><h1>Create your account</h1><p className="access-lede">Manage stock and prices for every marketplace from one place.</p><GoogleButton /><form className="access-form" onSubmit={submit} aria-describedby="signup-guidance"><label>Full name<input required value={fullName} onChange={event => setFullName(event.target.value)} autoComplete="name" placeholder="Marco Rossi" /></label><label>Email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" placeholder="you@example.com" /></label><label>Password<PasswordInput value={password} change={setPassword} autoComplete="new-password" /><span id="signup-guidance" className="field-hint">At least 8 characters</span></label><ErrorText value={error} />{error && <p className="form-help"><a href="#signin">Sign in</a> · <a href="#forgot">Reset password</a></p>}<button className="primary-button" disabled={busy || created}>{busy ? 'Creating account…' : created ? 'Account created' : 'Create account'}</button></form><p className="legal-note"><a href="#legal">Terms</a> and <a href="#legal">Privacy policy</a> are pending approval. No legal acceptance is recorded.</p></div><p className="auth-footer">Already have an account? <a href="#signin">Sign in</a></p></section><aside className="auth-aside signup-aside"><div className="shape shape-peach" /><div className="shape shape-sage" /><div className="aside-content"><h2>Up and running in<br />three steps</h2><ol className="preview-steps"><li><b>1</b><span><strong>Create your workspace</strong><small>Business name, country and currency</small></span></li><li><b>2</b><span><strong>Import your products</strong><small>Upload a CSV or add them by hand</small></span></li><li><b>3</b><span><strong>Connect your platforms</strong><small>Amazon, Unieuro, Euronics, eBay</small></span></li></ol></div></aside></div></Frame>
}

export function SignIn() {
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  const [providerError] = useState(() => {
    if (typeof window === 'undefined') return null
    const reason = new URLSearchParams(window.location.search).get('authError')
    if (reason === 'google-unverified') return 'Google did not confirm this email address. Try another Google account.'
    if (reason === 'google-link') return 'This Google account needs to be linked after signing in with email.'
    if (reason === 'google') return 'Google sign-in could not be completed. Please try again.'
    return null
  })
  useEffect(() => {
    if (typeof window !== 'undefined' && window.location.search.includes('authError') && window.history) {
      window.history.replaceState(null, '', `${window.location.pathname}${window.location.hash}`)
    }
  }, [])
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { await api.signIn({ email: email.trim(), password }); const pendingInvite = sessionStorage.getItem('stockhub-pending-invite'); if (pendingInvite) { sessionStorage.removeItem('stockhub-pending-invite'); go(`accept?token=${pendingInvite}`); return } const session = await api.session(); go(session.activeWorkspaceId || session.workspaces.length ? 'dashboard' : 'workspace') }
    catch (caught) { setError(caught instanceof AccessApiError && caught.status === 401 ? 'Email or password is incorrect.' : message(caught, 'We could not sign you in. Try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="auth signin"><div className="auth-grid"><section className="auth-main"><div className="auth-content"><h1>Sign in</h1><p className="access-lede">Welcome back. Sign in to manage stock across your marketplaces.</p><ErrorText value={providerError} /><GoogleButton /><form className="access-form" onSubmit={submit}><label>Email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" placeholder="you@example.com" /></label><label><span className="label-row">Password <a href="#forgot">Forgot password?</a></span><PasswordInput value={password} change={setPassword} autoComplete="current-password" /></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button></form></div><p className="auth-footer">New to StockHub? <a href="#signup">Start a free trial</a></p></section><aside className="auth-aside signin-aside"><div className="shape shape-sage" /><div className="shape shape-peach" /><div className="aside-content"><h2>One stock count for<br />every marketplace.</h2><div className="stock-preview"><strong>Samsung Galaxy S24 128GB</strong><div className="stock-preview-row"><span>On hand<b>5</b></span>−<span>Reserved<b>2</b></span>=<span className="available">Available<b>3</b></span></div><small>● Shown on Amazon, Unieuro, Euronics and eBay</small></div></div></aside></div></Frame>
}

export function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [sent, setSent] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { await api.requestPasswordReset({ email: email.trim() }); setSent(true) }
    catch (caught) { setError(message(caught, 'We could not send a reset link. Please try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="recovery-page"><section className="recovery-content"><h1>Reset your password</h1><p className="access-lede">Enter your email address and we’ll send a link to choose a new password.</p>{sent ? <p role="status">If an account uses this email, a reset link is on its way. Check your inbox.</p> : <form className="access-form" onSubmit={submit}><label>Email<input required type="email" autoComplete="email" value={email} onChange={event => setEmail(event.target.value)} placeholder="you@example.com" /></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Sending…' : 'Send reset link'}</button></form>}<p className="form-help"><a href="#signin">Back to sign in</a></p></section></Frame>
}

export function ResetPassword() {
  const token = typeof window === 'undefined' ? '' : new URLSearchParams(window.location.hash.split('?')[1] || '').get('token') || ''
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [complete, setComplete] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { await api.confirmPasswordReset({ token, newPassword: password }); setComplete(true) }
    catch (caught) { setError(message(caught, 'This reset link is invalid or has expired. Request another link.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="recovery-page"><section className="recovery-content"><h1>Choose a new password</h1>{complete ? <><p role="status">Your password has been changed. Sign in with the new password.</p><a className="primary-button" href="#signin">Sign in</a></> : token ? <form className="access-form" onSubmit={submit}><label>New password<PasswordInput value={password} change={setPassword} autoComplete="new-password" /><span className="field-hint">At least 8 characters</span></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Updating…' : 'Update password'}</button></form> : <><p>This reset link is missing a token.</p><a href="#forgot">Request a new link</a></>}</section></Frame>
}

export function Workspace() {
  const [businessName, setBusinessName] = useState(''); const [country, setCountry] = useState('IT'); const [currency, setCurrency] = useState('EUR'); const [vatNumber, setVatNumber] = useState(''); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  const retryKey = useRef<string | null>(null)
  useEffect(() => { api.session().catch(caught => { if (caught instanceof AccessApiError && caught.status === 401) go('signin') }) }, [])
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { const workspace = await api.createWorkspace({ businessName: businessName.trim(), country, currency, vatNumber: vatNumber.trim() || null }, retryKey.current ??= newRequestKey()); await api.setActiveWorkspace(workspace.id); go('dashboard') }
    catch (caught) { setError(message(caught, 'We could not create this workspace. Try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="workspace-page" topRight={<div className="step-chips"><span>✓ Account</span><span>2&nbsp; Workspace</span></div>}><div className="workspace-grid"><section className="workspace-main"><h1>Create your workspace</h1><p className="access-lede">A workspace holds your products, platforms and team. You can create more later.</p><form className="access-form" onSubmit={submit}><label>Business name<input required value={businessName} onChange={event => { setBusinessName(event.target.value); retryKey.current = null }} placeholder="e.g. Rossi Elettronica" /></label><div className="form-row"><label>Country<select value={country} onChange={event => { setCountry(event.target.value); retryKey.current = null }}><option value="IT">Italy</option><option value="GB">United Kingdom</option><option value="DE">Germany</option><option value="FR">France</option><option value="US">United States</option></select></label><label>Currency<select value={currency} onChange={event => { setCurrency(event.target.value); retryKey.current = null }}><option value="EUR">EUR · Euro (€)</option><option value="GBP">GBP · Pound (£)</option><option value="USD">USD · Dollar ($)</option></select></label></div><label>VAT number <span className="field-hint">Optional</span><input value={vatNumber} onChange={event => { setVatNumber(event.target.value); retryKey.current = null }} placeholder="IT 01234567890" /><span className="field-hint">Shown on your invoices. You can add it later in Settings.</span></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Creating workspace…' : 'Create workspace'}</button></form></section><aside className="workspace-aside"><div className="workspace-preview"><span className="preview-label">Preview</span><div className="workspace-switch"><b>{businessName.trim().slice(0, 1).toUpperCase() || '?'}</b><span><strong>{businessName.trim() || 'Your business'}</strong><small>Owner · 1 workspace</small></span><span aria-hidden="true">⌄</span></div><dl><div><dt>Country</dt><dd>{{ IT: 'Italy', GB: 'United Kingdom', DE: 'Germany', FR: 'France', US: 'United States' }[country]}</dd></div><div><dt>Prices in</dt><dd>{currency} · {{ EUR: 'Euro (€)', GBP: 'Pound (£)', USD: 'Dollar ($)' }[currency]}</dd></div><div><dt>VAT</dt><dd>{vatNumber || 'Not added yet'}</dd></div></dl></div><div className="shape shape-sage" /></aside></div></Frame>
}

const nav = ['Dashboard', 'Inventory', 'Orders', 'Reservations', 'Platforms', 'Pricing rules', 'Reports', 'Team', 'Settings']
const navRoutes = ['#dashboard', '#inventory', '#orders', '#reservations', '#platforms', '#pricing', '#reports', '#team', '#settings']
const platformOptions = [
  ['amazon', 'Amazon', 'Seller Central authorization comes in the Platforms milestone.'],
  ['unieuro', 'Unieuro', 'Marketplace authorization comes in the Platforms milestone.'],
  ['euronics', 'Euronics', 'Marketplace authorization comes in the Platforms milestone.'],
  ['ebay', 'eBay', 'OAuth authorization comes in the Platforms milestone.'],
] as const

function parseCsv(text: string): ProductRequest[] {
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
    const product = {
      sku: sku?.trim() || '',
      name: name?.trim() || '',
      onHand: Number(onHand),
      basePrice: Number(String(basePrice || '').replace(',', '.')),
      category: category?.trim() || null,
    }
    if (!product.sku || !product.name || !Number.isFinite(product.onHand) || !Number.isFinite(product.basePrice)) {
      throw new Error(`Row ${index + 2} needs SKU, name, on hand, and base price.`)
    }
    return product
  })
}

function ProductImporter({ workspaceId, products, refresh }: { workspaceId: string | null; products: ProductResponse[]; refresh: (products: ProductResponse[]) => void }) {
  const [mode, setMode] = useState<'csv' | 'manual'>('csv')
  const [draft, setDraft] = useState<ProductRequest>({ sku: '', name: '', onHand: 0, basePrice: 0, category: '' })
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  async function save(items: ProductRequest[]) {
    if (!workspaceId) return
    setBusy(true); setError(null); setStatus(null)
    try { const saved = await api.importProducts(workspaceId, items); refresh(await api.products(workspaceId)); setStatus(`${saved.length} product${saved.length === 1 ? '' : 's'} saved.`) }
    catch (caught) { setError(message(caught, 'Could not save products.')) }
    finally { setBusy(false) }
  }
  async function upload(file: File | null) {
    if (!file) return
    try { await save(parseCsv(await file.text())) }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Could not read this CSV.') }
  }
  async function submit(event: FormEvent) {
    event.preventDefault()
    await save([{ ...draft, sku: draft.sku.trim(), name: draft.name.trim(), category: draft.category?.trim() || null }])
    setDraft({ sku: '', name: '', onHand: 0, basePrice: 0, category: '' })
  }
  return <div className="step-body"><div className="step-tabs"><button className={mode === 'csv' ? 'selected' : ''} type="button" onClick={() => setMode('csv')}>Upload CSV</button><button className={mode === 'manual' ? 'selected' : ''} type="button" onClick={() => setMode('manual')}>Add manually</button></div>{mode === 'csv' ? <label className="drop-zone"><input type="file" accept=".csv,text/csv" disabled={busy || !workspaceId} onChange={event => upload(event.target.files?.[0] ?? null)} /><span>↥</span><strong>Drop a CSV file here, or <u>browse</u></strong><small>Columns: SKU, name, on hand, base price, category (optional)</small></label> : <form className="manual-product-form" onSubmit={submit}><input required placeholder="SKU" value={draft.sku} onChange={event => setDraft({ ...draft, sku: event.target.value })} /><input required placeholder="Product name" value={draft.name} onChange={event => setDraft({ ...draft, name: event.target.value })} /><input required type="number" min="0" placeholder="On hand" value={draft.onHand} onChange={event => setDraft({ ...draft, onHand: Number(event.target.value) })} /><input required type="number" min="0" step="0.01" placeholder="Base price" value={draft.basePrice} onChange={event => setDraft({ ...draft, basePrice: Number(event.target.value) })} /><input placeholder="Category" value={draft.category ?? ''} onChange={event => setDraft({ ...draft, category: event.target.value })} /><button className="primary-button" disabled={busy || !workspaceId}>{busy ? 'Saving...' : 'Save product'}</button></form>}<a href="data:text/csv;charset=utf-8,SKU%2Cname%2Con%20hand%2Cbase%20price%2Ccategory%0A" download="stockhub-products-template.csv">Download CSV template</a>{products.length > 0 && <ul className="product-mini-list">{products.slice(0, 3).map(product => <li key={product.id}><b>{product.sku}</b><span>{product.name}</span><small>{product.onHand} on hand</small></li>)}</ul>}<ErrorText value={error} />{status && <p className="form-success" role="status">{status}</p>}</div>
}

export function Onboarding() {
  const [session, setSession] = useState<SessionResponse | null>(null); const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null); const [products, setProducts] = useState<ProductResponse[]>([]); const [error, setError] = useState<string | null>(null); const [open, setOpen] = useState(0)
  const [collapsed, setCollapsed] = useState(false); const [theme, setTheme] = useState(() => typeof localStorage === 'undefined' ? 'light' : localStorage.getItem('stockhub-theme') || 'light'); const [menu, setMenu] = useState<'workspace' | 'notifications' | 'account' | null>(null); const [query, setQuery] = useState(''); const [platform, setPlatform] = useState<(typeof platformOptions)[number] | null>(null)
  const done = products.length > 0 ? 1 : 0
  useEffect(() => { document.documentElement.dataset.theme = theme; localStorage.setItem('stockhub-theme', theme) }, [theme])
  useEffect(() => { let alive = true; api.session().then(async data => { if (!alive) return; const current = data.workspaces.find(item => item.id === data.activeWorkspaceId) || data.workspaces[0]; if (!current) { go('workspace'); return } setSession(data); setWorkspace(current); setProducts(await api.products(current.id)); await api.onboarding(current.id) }).catch(caught => { if (alive) { if (caught instanceof AccessApiError && caught.status === 401) go('signin'); else setError(message(caught, 'Could not load your workspace.')) } }); return () => { alive = false } }, [])
  async function chooseWorkspace(workspaceId: string) { try { await api.setActiveWorkspace(workspaceId); const data = await api.session(); const current = data.workspaces.find(item => item.id === workspaceId) || null; setSession(data); setWorkspace(current); setProducts(current ? await api.products(current.id) : []); setMenu(null) } catch (caught) { setError(message(caught, 'Could not switch workspace.')) } }
  async function select(key: string, route: string) { if (!workspace) return; try { await api.selectOnboardingAction(workspace.id, key); go(route) } catch (caught) { setError(message(caught, 'Could not open this step.')) } }
  const matches = query.trim() ? products.filter(product => `${product.sku} ${product.name} ${product.category ?? ''}`.toLowerCase().includes(query.trim().toLowerCase())) : []
  return <main className={`dashboard-page ${collapsed ? 'sidebar-collapsed' : ''}`}><div className="dashboard-shell"><aside className="dashboard-sidebar"><button className="workspace-switch" type="button" onClick={() => setMenu(menu === 'workspace' ? null : 'workspace')}><b>{workspace?.businessName.slice(0, 2).toUpperCase() || 'SH'}</b><span><strong>{workspace?.businessName || 'Your workspace'}</strong><small>{workspace?.role || 'Owner'} · {session?.workspaces.length || 1} workspace</small></span><span aria-hidden="true">⌄</span></button>{menu === 'workspace' && <div className="dashboard-popover workspace-menu">{session?.workspaces.map(item => <button key={item.id} type="button" onClick={() => chooseWorkspace(item.id)}>{item.businessName}<small>{item.role}</small></button>)}<a href="#workspace">Create workspace</a></div>}<nav aria-label="Main navigation">{nav.map((item, index) => <a key={item} className={index === 0 ? 'active' : ''} href={navRoutes[index]}><span aria-hidden="true">{['▦','◇','🛒','◷','♧','◇','▥','♧','☷'][index]}</span>{item}</a>)}</nav><div className="sidebar-bottom"><strong>Free trial</strong><a href="#deferred">Upgrade</a><small>14 days left</small><button type="button" onClick={() => setCollapsed(!collapsed)}>Collapse</button></div></aside><section className="dashboard-main"><header className="dashboard-top"><div className="dashboard-search"><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search products, SKUs, orders..." aria-label="Search products, SKUs, orders" />{query && <div className="dashboard-popover search-results">{matches.length ? matches.map(product => <button key={product.id} type="button" onClick={() => { setQuery(product.sku); setOpen(0) }}><b>{product.sku}</b><span>{product.name}</span></button>) : <p>No products found yet.</p>}</div>}</div><div><span className="platform-pill">● &nbsp; No platforms connected</span><button type="button" className="icon-button" onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}>{theme === 'dark' ? '☀' : '☾'}</button><button type="button" className="icon-button" onClick={() => setMenu(menu === 'notifications' ? null : 'notifications')}>♧</button><button type="button" className="account-button" onClick={() => setMenu(menu === 'account' ? null : 'account')}><span className="user-avatar">{session?.fullName?.slice(0, 2).toUpperCase() || 'SH'}</span><span>{session?.fullName || 'Your account'}</span></button></div>{menu === 'notifications' && <div className="dashboard-popover notification-menu"><strong>No notifications</strong><p>Connection and invite updates will appear here.</p></div>}{menu === 'account' && <div className="dashboard-popover account-menu"><a href="#cover">Cover page</a><button type="button" onClick={async () => { try { await api.signOut(); go('signin') } catch (caught) { if (caught instanceof AccessApiError && caught.status === 401) go('signin'); else setError(message(caught, 'Could not sign out. Try again.')) } }}>Sign out</button></div>}</header><div className="onboarding-content"><h1>Welcome, {session?.fullName?.split(' ')[0] || 'there'}</h1><p>Three steps to start syncing stock and prices for {workspace?.businessName || 'your business'}.</p><div className="progress-line"><strong>{done} of 3 done</strong><span role="progressbar" aria-valuemin={0} aria-valuemax={3} aria-valuenow={done} aria-label="Onboarding progress"><i style={{ width: `${done / 3 * 100}%` }} /></span></div><ErrorText value={error} /><div className="onboarding-steps"><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 0 ? -1 : 0)} aria-expanded={open === 0}><b>1</b><span><strong>Import products</strong><small>Upload a CSV or add products one by one</small></span><span aria-hidden="true">⌄</span></button>{open === 0 && <ProductImporter workspaceId={workspace?.id || null} products={products} refresh={setProducts} />}</section><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 1 ? -1 : 1)} aria-expanded={open === 1}><b>2</b><span><strong>Connect your first platform</strong><small>Choose where you sell. You can add more later.</small></span><span aria-hidden="true">⌄</span></button>{open === 1 && <div className="step-body"><div className="platform-picker">{platformOptions.map(option => <button key={option[0]} type="button" onClick={async () => { setPlatform(option); await select('connect-platform', 'onboarding') }}><strong>{option[1]}</strong><small>Open setup handoff</small></button>)}</div>{platform && <p className="form-success" role="status">{platform[1]} picker opened. {platform[2]}</p>}</div>}</section><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 2 ? -1 : 2)} aria-expanded={open === 2}><b>3</b><span><strong>Invite your team</strong><small>Give your staff their own login and role</small></span><span aria-hidden="true">⌄</span></button>{open === 2 && <div className="step-body"><button className="secondary-button" onClick={() => select('invite-team', 'invite')}>Invite a teammate</button></div>}</section></div></div></section></div></main>
}

export function Invite() {
  const [email, setEmail] = useState(''); const [role, setRole] = useState<'Viewer' | 'WarehouseStaff' | 'Manager' | 'Admin'>('Viewer')
  const [workspaceId, setWorkspaceId] = useState<string | null>(null); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null); const [sent, setSent] = useState(false)
  useEffect(() => { api.session().then(data => setWorkspaceId(data.activeWorkspaceId || data.workspaces[0]?.id || null)).catch(() => go('signin')) }, [])
  async function submit(event: FormEvent) { event.preventDefault(); if (!workspaceId) return; setBusy(true); setError(null); try { await api.invite(workspaceId, { email: email.trim(), role }); setSent(true) } catch (caught) { setError(message(caught, 'Could not send the invitation.')) } finally { setBusy(false) } }
  return <Frame kind="deferred-page"><div className="deferred-content"><h1>Invite your team</h1><p>Give a teammate their own login and role in this workspace.</p>{sent ? <p role="status">Invitation email sent. The link can be used once within 72 hours.</p> : <form className="access-form" onSubmit={submit}><label>Email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} placeholder="teammate@company.it" /></label><label>Role<select value={role} onChange={event => setRole(event.target.value as typeof role)}><option value="Viewer">Viewer</option><option value="WarehouseStaff">Warehouse staff</option><option value="Manager">Manager</option><option value="Admin">Admin</option></select></label><ErrorText value={error} /><button className="primary-button" disabled={busy || !workspaceId}>{busy ? 'Sending...' : 'Send invitation'}</button></form>}<p><a href="#onboarding">Return to the checklist</a></p></div></Frame>
}

export function AcceptInvite() {
  const token = typeof window === 'undefined' ? '' : new URLSearchParams(window.location.hash.split('?')[1] || '').get('token') || ''
  const [status, setStatus] = useState('Checking invitation...')
  const [error, setError] = useState<string | null>(null)
  useEffect(() => { if (!token) { setError('This invitation link is missing a token.'); return } api.invitation(token).then(invitation => setStatus(`Invitation for ${invitation.email} as ${invitation.role}. Sign in with that email, then accept.`)).catch(caught => setError(message(caught, 'This invitation link is invalid or expired.'))) }, [token])
  async function accept() { setError(null); try { await api.acceptInvitation(token); go('dashboard') } catch (caught) { if (caught instanceof AccessApiError && caught.status === 401) { sessionStorage.setItem('stockhub-pending-invite', token); go('signin'); return } setError(message(caught, 'Could not accept this invitation.')) } }
  return <Frame kind="deferred-page"><div className="deferred-content"><h1>Accept invitation</h1><p>{status}</p><ErrorText value={error} />{token && <button className="primary-button" type="button" onClick={accept}>Accept invitation</button>}<p><a href="#signin">Sign in</a> · <a href="#signup">Create account</a></p></div></Frame>
}

export function Deferred({ legal = false }: { legal?: boolean }) {
  return <Frame kind="deferred-page"><div className="deferred-content"><h1>{legal ? 'Legal documents' : 'Coming next'}</h1><p>{legal ? 'Terms and Privacy content is pending approval. No legal acceptance has been recorded.' : 'This capability is planned for a later milestone. No action has been marked complete.'}</p><a className="secondary-button" href={legal ? '#signup' : '#dashboard'}>Go back</a></div></Frame>
}
