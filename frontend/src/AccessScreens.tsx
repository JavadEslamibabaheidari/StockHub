import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { AccessApiClient, AccessApiError, type SessionResponse, type WorkspaceSummary } from './api/generated'

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
  const [error, setError] = useState<string | null>(null)
  async function start() {
    setError(null)
    try {
      const response = await fetch('/api/auth/google/start', { credentials: 'include', redirect: 'manual' })
      if (response.status === 503) { setError('Google sign-in is not configured for this environment. Use email for now.'); return }
      if (response.type === 'opaqueredirect' || response.redirected) { window.location.assign(response.url || '/api/auth/google/start'); return }
      if (!response.ok) throw new Error('provider')
      window.location.assign('/api/auth/google/start')
    } catch { setError('Google sign-in is unavailable. Use email for now.') }
  }
  return <><button className="google-button" type="button" onClick={start}><span className="google-g" aria-hidden="true">G</span>Continue with Google</button><ErrorText value={error} /><div className="form-divider"><span>or with email</span></div></>
}

function PasswordInput({ value, change, autoComplete }: { value: string; change: (value: string) => void; autoComplete: string }) {
  const [visible, setVisible] = useState(false)
  return <span className="password-input"><input required minLength={autoComplete === 'new-password' ? 8 : undefined} type={visible ? 'text' : 'password'} value={value} onChange={event => change(event.target.value)} autoComplete={autoComplete} /><button type="button" aria-label={visible ? 'Hide password' : 'Show password'} aria-pressed={visible} onClick={() => setVisible(!visible)}>{visible ? 'Hide' : 'Show'}</button></span>
}

export function SignUp() {
  const [fullName, setFullName] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { await api.signUp({ fullName: fullName.trim(), email: email.trim(), password }); go('workspace') }
    catch (caught) { setError(message(caught, 'We could not create your account. Try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="auth signup" topRight={<span className="step-label">Step 1 of 2</span>}><div className="auth-grid"><section className="auth-main"><div className="auth-content"><h1>Create your account</h1><p className="access-lede">Manage stock and prices for every marketplace from one place.</p><GoogleButton /><form className="access-form" onSubmit={submit} aria-describedby="signup-guidance"><label>Full name<input required value={fullName} onChange={event => setFullName(event.target.value)} autoComplete="name" placeholder="Marco Rossi" /></label><label>Work email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" placeholder="you@company.it" /></label><label>Password<PasswordInput value={password} change={setPassword} autoComplete="new-password" /><span id="signup-guidance" className="field-hint">At least 8 characters</span></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Creating account…' : 'Create account'}</button></form><p className="legal-note"><a href="#legal">Terms</a> and <a href="#legal">Privacy policy</a> are pending approval. No legal acceptance is recorded.</p></div><p className="auth-footer">Already have an account? <a href="#signin">Sign in</a></p></section><aside className="auth-aside signup-aside"><div className="shape shape-peach" /><div className="shape shape-sage" /><div className="aside-content"><h2>Up and running in<br />three steps</h2><ol className="preview-steps"><li><b>1</b><span><strong>Create your workspace</strong><small>Business name, country and currency</small></span></li><li><b>2</b><span><strong>Import your products</strong><small>Upload a CSV or add them by hand</small></span></li><li><b>3</b><span><strong>Connect your platforms</strong><small>Amazon, Unieuro, Euronics, eBay</small></span></li></ol></div></aside></div></Frame>
}

export function SignIn() {
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { await api.signIn({ email: email.trim(), password }); const session = await api.session(); go(session.activeWorkspaceId || session.workspaces.length ? 'onboarding' : 'workspace') }
    catch (caught) { setError(caught instanceof AccessApiError && caught.status === 401 ? 'Email or password is incorrect.' : message(caught, 'We could not sign you in. Try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="auth signin"><div className="auth-grid"><section className="auth-main"><div className="auth-content"><h1>Sign in</h1><p className="access-lede">Welcome back. Sign in to manage stock across your marketplaces.</p><GoogleButton /><form className="access-form" onSubmit={submit}><label>Work email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" placeholder="you@company.it" /></label><label><span className="label-row">Password <a href="#forgot">Forgot password?</a></span><PasswordInput value={password} change={setPassword} autoComplete="current-password" /></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button></form></div><p className="auth-footer">New to StockHub? <a href="#signup">Start a free trial</a></p></section><aside className="auth-aside signin-aside"><div className="shape shape-sage" /><div className="shape shape-peach" /><div className="aside-content"><h2>One stock count for<br />every marketplace.</h2><div className="stock-preview"><strong>Samsung Galaxy S24 128GB</strong><div className="stock-preview-row"><span>On hand<b>5</b></span>−<span>Reserved<b>2</b></span>=<span className="available">Available<b>3</b></span></div><small>● Shown on Amazon, Unieuro, Euronics and eBay</small></div></div></aside></div></Frame>
}

export function Workspace() {
  const [businessName, setBusinessName] = useState(''); const [country, setCountry] = useState('IT'); const [currency, setCurrency] = useState('EUR'); const [vatNumber, setVatNumber] = useState(''); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  const retryKey = useRef<string | null>(null)
  useEffect(() => { api.session().catch(caught => { if (caught instanceof AccessApiError && caught.status === 401) go('signin') }) }, [])
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null)
    try { const workspace = await api.createWorkspace({ businessName: businessName.trim(), country, currency, vatNumber: vatNumber.trim() || null }, retryKey.current ??= newRequestKey()); await api.setActiveWorkspace(workspace.id); go('onboarding') }
    catch (caught) { setError(message(caught, 'We could not create this workspace. Try again.')) }
    finally { setBusy(false) }
  }
  return <Frame kind="workspace-page" topRight={<div className="step-chips"><span>✓ Account</span><span>2&nbsp; Workspace</span></div>}><div className="workspace-grid"><section className="workspace-main"><h1>Create your workspace</h1><p className="access-lede">A workspace holds your products, platforms and team. You can create more later.</p><form className="access-form" onSubmit={submit}><label>Business name<input required value={businessName} onChange={event => { setBusinessName(event.target.value); retryKey.current = null }} placeholder="e.g. Rossi Elettronica" /></label><div className="form-row"><label>Country<select value={country} onChange={event => { setCountry(event.target.value); retryKey.current = null }}><option value="IT">Italy</option><option value="GB">United Kingdom</option><option value="DE">Germany</option><option value="FR">France</option><option value="US">United States</option></select></label><label>Currency<select value={currency} onChange={event => { setCurrency(event.target.value); retryKey.current = null }}><option value="EUR">EUR · Euro (€)</option><option value="GBP">GBP · Pound (£)</option><option value="USD">USD · Dollar ($)</option></select></label></div><label>VAT number <span className="field-hint">Optional</span><input value={vatNumber} onChange={event => { setVatNumber(event.target.value); retryKey.current = null }} placeholder="IT 01234567890" /><span className="field-hint">Shown on your invoices. You can add it later in Settings.</span></label><ErrorText value={error} /><button className="primary-button" disabled={busy}>{busy ? 'Creating workspace…' : 'Create workspace'}</button></form></section><aside className="workspace-aside"><div className="workspace-preview"><span className="preview-label">Preview</span><div className="workspace-switch"><b>{businessName.trim().slice(0, 1).toUpperCase() || '?'}</b><span><strong>{businessName.trim() || 'Your business'}</strong><small>Owner · 1 workspace</small></span><span aria-hidden="true">⌄</span></div><dl><div><dt>Country</dt><dd>{{ IT: 'Italy', GB: 'United Kingdom', DE: 'Germany', FR: 'France', US: 'United States' }[country]}</dd></div><div><dt>Prices in</dt><dd>{currency} · {{ EUR: 'Euro (€)', GBP: 'Pound (£)', USD: 'Dollar ($)' }[currency]}</dd></div><div><dt>VAT</dt><dd>{vatNumber || 'Not added yet'}</dd></div></dl></div><div className="shape shape-sage" /></aside></div></Frame>
}

const nav = ['Dashboard', 'Inventory', 'Orders', 'Reservations', 'Platforms', 'Pricing rules', 'Reports', 'Team', 'Settings']
export function Onboarding() {
  const [session, setSession] = useState<SessionResponse | null>(null); const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null); const [error, setError] = useState<string | null>(null); const [open, setOpen] = useState(0)
  useEffect(() => { let alive = true; api.session().then(data => { if (!alive) return; const current = data.workspaces.find(item => item.id === data.activeWorkspaceId) || data.workspaces[0]; if (!current) { go('workspace'); return } setSession(data); setWorkspace(current); api.onboarding(current.id).catch(caught => { if (alive) setError(message(caught, 'Could not load checklist.')) }) }).catch(caught => { if (alive) { if (caught instanceof AccessApiError && caught.status === 401) go('signin'); else setError(message(caught, 'Could not load your workspace.')) } }); return () => { alive = false } }, [])
  async function select(key: string, route: string) { if (!workspace) return; try { await api.selectOnboardingAction(workspace.id, key); go(route) } catch (caught) { setError(message(caught, 'Could not open this step.')) } }
  return <main className="dashboard-page"><div className="dashboard-shell"><aside className="dashboard-sidebar"><div className="workspace-switch"><b>{workspace?.businessName.slice(0, 2).toUpperCase() || 'SH'}</b><span><strong>{workspace?.businessName || 'Your workspace'}</strong><small>Owner · {session?.workspaces.length || 1} workspace</small></span><span aria-hidden="true">⌄</span></div><nav aria-label="Main navigation">{nav.map((item, index) => <a key={item} className={index === 0 ? 'active' : ''} href={index === 0 ? '#onboarding' : '#deferred'}><span aria-hidden="true">{['▦','◇','🛒','◷','♧','◇','▥','♧','☷'][index]}</span>{item}</a>)}</nav><div className="sidebar-bottom"><strong>Free trial</strong><a href="#deferred">Upgrade</a><small>14 days left</small></div></aside><section className="dashboard-main"><header className="dashboard-top"><span className="search-placeholder">⌕ &nbsp; Search products, SKUs, orders...</span><div><span className="platform-pill">● &nbsp; No platforms connected</span><span>☾</span><span>♧</span><span className="user-avatar">{session?.fullName?.slice(0, 2).toUpperCase() || 'SH'}</span><span>{session?.fullName || 'Your account'}</span><a className="dashboard-cover-link" href="#cover">Cover</a><button className="signout-button" onClick={async () => { try { await api.signOut(); go('signin') } catch (caught) { if (caught instanceof AccessApiError && caught.status === 401) go('signin'); else setError(message(caught, 'Could not sign out. Try again.')) } }}>Sign out</button></div></header><div className="onboarding-content"><h1>Welcome, {session?.fullName?.split(' ')[0] || 'there'}</h1><p>Three steps to start syncing stock and prices for {workspace?.businessName || 'your business'}.</p><div className="progress-line"><strong>0 of 3 done</strong><span role="progressbar" aria-valuemin={0} aria-valuemax={3} aria-valuenow={0} aria-label="Onboarding progress" /></div><ErrorText value={error} /><div className="onboarding-steps"><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 0 ? -1 : 0)} aria-expanded={open === 0}><b>1</b><span><strong>Import products</strong><small>Upload a CSV or add products one by one</small></span><span aria-hidden="true">⌄</span></button>{open === 0 && <div className="step-body"><div className="step-tabs"><button className="selected" type="button">↥ Upload CSV</button><button type="button" onClick={() => select('import-products', 'deferred')}>＋ Add manually</button></div><label className="drop-zone"><input type="file" accept=".csv,text/csv" onChange={() => select('import-products', 'deferred')} /><span>↥</span><strong>Drop a CSV file here, or <u>browse</u></strong><small>Columns: SKU, name, on hand, base price, category (optional)</small></label><a href="data:text/csv;charset=utf-8,SKU%2Cname%2Con%20hand%2Cbase%20price%2Ccategory%0A" download="stockhub-products-template.csv">Download CSV template</a></div>}</section><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 1 ? -1 : 1)} aria-expanded={open === 1}><b>2</b><span><strong>Connect your first platform</strong><small>Choose where you sell. You can add more later.</small></span><span aria-hidden="true">⌄</span></button>{open === 1 && <div className="step-body"><p>Amazon, Unieuro, Euronics and eBay are planned for the Platforms milestone.</p><button className="secondary-button" onClick={() => select('connect-platform', 'deferred')}>View platform handoff</button></div>}</section><section className="onboarding-step"><button className="step-heading" onClick={() => setOpen(open === 2 ? -1 : 2)} aria-expanded={open === 2}><b>3</b><span><strong>Invite your team</strong><small>Give your staff their own login and role</small></span><span aria-hidden="true">⌄</span></button>{open === 2 && <div className="step-body"><button className="secondary-button" onClick={() => select('invite-team', 'invite')}>Invite a teammate</button></div>}</section></div></div></section></div></main>
}

export function Invite() {
  const [email, setEmail] = useState(''); const [role, setRole] = useState<'Viewer' | 'WarehouseStaff' | 'Manager' | 'Admin'>('Viewer')
  const [workspaceId, setWorkspaceId] = useState<string | null>(null); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null); const [sent, setSent] = useState(false)
  useEffect(() => { api.session().then(data => setWorkspaceId(data.activeWorkspaceId || data.workspaces[0]?.id || null)).catch(() => go('signin')) }, [])
  async function submit(event: FormEvent) { event.preventDefault(); if (!workspaceId) return; setBusy(true); setError(null); try { await api.invite(workspaceId, { email: email.trim(), role }); setSent(true) } catch (caught) { setError(message(caught, 'Could not create the invitation request.')) } finally { setBusy(false) } }
  return <Frame kind="deferred-page"><div className="deferred-content"><h1>Invite your team</h1><p>Give a teammate their own role in this workspace. Email delivery is planned for the Team milestone.</p>{sent ? <p role="status">Invitation request created. No email has been sent.</p> : <form className="access-form" onSubmit={submit}><label>Work email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} placeholder="teammate@company.it" /></label><label>Role<select value={role} onChange={event => setRole(event.target.value as typeof role)}><option value="Viewer">Viewer</option><option value="WarehouseStaff">Warehouse staff</option><option value="Manager">Manager</option><option value="Admin">Admin</option></select></label><ErrorText value={error} /><button className="primary-button" disabled={busy || !workspaceId}>{busy ? 'Creating request…' : 'Create invitation request'}</button></form>}<p><a href="#onboarding">Return to the checklist</a></p></div></Frame>
}

export function Deferred({ forgot = false, legal = false }: { forgot?: boolean; legal?: boolean }) {
  return <Frame kind="deferred-page"><div className="deferred-content"><h1>{forgot ? 'Password recovery' : legal ? 'Legal documents' : 'Coming next'}</h1><p>{forgot ? 'Password reset is planned for a later milestone. No email has been sent.' : legal ? 'Terms and Privacy content is pending approval. No legal acceptance has been recorded.' : 'This capability is planned for a later milestone. No action has been marked complete.'}</p><a className="secondary-button" href={forgot ? '#signin' : legal ? '#signup' : '#onboarding'}>Go back</a></div></Frame>
}
