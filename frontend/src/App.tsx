import React, { useEffect, useState } from 'react'
import { AccessApiClient, AccessApiError } from './api/generated'

type RoadmapSection = {
  number: string
  title: string
  screens?: string[]
}

const roadmapSections: RoadmapSection[] = [
  { number: '0', title: 'Cover', screens: ['0 Cover'] },
  { number: '1', title: 'Access', screens: ['1.1 Sign up', '1.2 Sign in', '1.3 Create workspace', '1.4 Onboarding checklist'] },
  { number: '2', title: 'Dashboard', screens: ['2.1 Dashboard', '2.2 Dashboard first use', '2.3 Dashboard first sync'] },
  { number: '3', title: 'Inventory', screens: ['3.1 Inventory', '3.2 Inventory empty state', '3.3 Inventory Warehouse staff view', '3.4 Product detail', '3.5 Product detail all platforms failing'] },
  { number: '4', title: 'Orders', screens: ['4.1 Orders', '4.2 Order detail', '4.3 Orders empty state', '4.4 Orders filters with no results', '4.5 Order detail return flow', '4.6 Order detail cancel confirmation', '4.7 Orders Warehouse staff view'] },
  { number: '5', title: 'Reservations', screens: ['5.1 Reservations'] },
  { number: '6', title: 'Platforms', screens: ['6.1 Platforms', '6.2 Add platform picker'] },
  { number: '7', title: 'Pricing rules', screens: ['7.1 Pricing rules'] },
  { number: '8', title: 'Reports', screens: ['8.1 Reports Daily digest', '8.2 Reports Weekly summary', '8.3 Reports Alert settings'] },
  { number: '9', title: 'Team', screens: ['9.1 Team'] },
  { number: '10', title: 'Settings', screens: ['10.1 Settings Billing', '10.2 Settings Appearance'] },
  { number: '11', title: 'Dark mode', screens: ['11.1 Dashboard dark', '11.2 Inventory dark quick view open', '11.3 Product detail dark'] },
  { number: '12', title: 'Design system', screens: ['12.1 Design system'] },
]

function Logo() {
  return (
    <div className="brand" aria-label="StockHub">
      <span className="brand-mark" aria-hidden="true">
        <span />
      </span>
      <span className="brand-name">StockHub</span>
    </div>
  )
}

function StockModel() {
  return (
    <section className="model-card" aria-labelledby="stock-model-title">
      <h2 id="stock-model-title">The three stock numbers</h2>
      <div className="stock-equation">
        <article className="number-card">
          <strong>On hand</strong>
          <span>Physically in the warehouse · the only number you edit</span>
        </article>
        <span className="equation-mark" aria-hidden="true">−</span>
        <article className="number-card reserved-card">
          <strong><span className="clock-icon" aria-hidden="true">◷</span> Reserved</strong>
          <span>Booked, not paid · held 10 min</span>
        </article>
        <span className="equation-mark" aria-hidden="true">=</span>
        <article className="number-card available-card">
          <strong>Available</strong>
          <span>What every platform sees</span>
        </article>
      </div>
      <h2 className="status-heading">Status colours · always with an icon or label</h2>
      <div className="status-list" aria-label="StockHub status meanings">
        <span className="status-pill healthy"><i aria-hidden="true" />Synced · healthy</span>
        <span className="status-pill warning"><i aria-hidden="true" />Low stock · expiring</span>
        <span className="status-pill danger"><i aria-hidden="true" />Failed · out of stock</span>
        <span className="status-pill neutral"><i aria-hidden="true" />Paused · reserved · neutral</span>
      </div>
    </section>
  )
}

function Contents() {
  return (
    <nav className="contents-card" aria-label="StockHub roadmap contents">
      <h2>Contents</h2>
      <ol>
        {roadmapSections.map((section) => {
          const current = section.number === '0'
          return (
            <li key={section.number} className={current ? 'current' : undefined}>
              <a href={current ? '#cover' : '#roadmap'} aria-current={current ? 'page' : undefined}>
                <span className="section-number">{section.number}</span>
                <span>{section.title}</span>
              </a>
              {section.screens && section.number !== '0' && (
                <ul>
                  {section.screens.map((screen) => <li key={screen}>{screen}</li>)}
                </ul>
              )}
            </li>
          )
        })}
      </ol>
      <p id="roadmap" className="roadmap-note">
        Cover is the first live section. The remaining sections are the approved
        roadmap for the StockHub product track.
      </p>
    </nav>
  )
}

const api = new AccessApiClient()

function AccessLayout({ title, children }: { title: string; children: React.ReactNode }) {
  return <main className="access-shell"><div className="access-card"><a className="back-link" href="#cover">← Back to Cover</a><Logo /><h1 className="access-title">{title}</h1>{children}</div></main>
}

function ErrorMessage({ error }: { error: string | null }) { return error ? <p className="form-error" role="alert">{error}</p> : null }

function SignUp() {
  const [fullName, setFullName] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [show, setShow] = useState(false); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  async function submit(event: React.FormEvent) { event.preventDefault(); setBusy(true); setError(null); try { await api.signUp({ fullName, email, password }); window.location.hash = 'workspace' } catch (caught) { setError(caught instanceof AccessApiError ? caught.problem.detail : 'We could not create your account. Try again.'); } finally { setBusy(false) } }
  return <AccessLayout title="Create your account"><p className="access-lede">Start with one calm place for every marketplace you sell on.</p><form className="access-form" onSubmit={submit} aria-describedby="signup-guidance"><label>Full name<input required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" /></label><label>Work email<input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" /></label><label>Password <span className="field-hint">8 characters minimum</span><span className="password-field"><input required minLength={8} type={show ? 'text' : 'password'} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" /><button type="button" aria-pressed={show} onClick={() => setShow(!show)}>{show ? 'Hide' : 'Show'}</button></span></label><p id="signup-guidance" className="form-hint">Your password is stored securely and never appears in logs.</p><ErrorMessage error={error} /><button className="primary-button" disabled={busy}>{busy ? 'Creating account…' : 'Create account'}</button></form><p className="secondary-action">Already have an account? <a href="#signin">Sign in</a></p><p className="deferred-note"><a href="/terms">Terms</a> · <a href="/privacy">Privacy</a> — legal acceptance is not recorded in this milestone.</p></AccessLayout>
}

function SignIn() {
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [show, setShow] = useState(false); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null)
  async function submit(event: React.FormEvent) { event.preventDefault(); setBusy(true); setError(null); try { await api.signIn({ email, password }); window.location.hash = 'workspace' } catch (caught) { setError(caught instanceof AccessApiError && caught.status === 401 ? 'Email or password is incorrect.' : 'We could not sign you in. Try again.'); } finally { setBusy(false) } }
  return <AccessLayout title="Welcome back"><p className="access-lede">Sign in to keep your stock count steady.</p><form className="access-form" onSubmit={submit}><label>Work email<input required type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" /></label><label>Password<span className="password-field"><input required type={show ? 'text' : 'password'} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" /><button type="button" aria-pressed={show} onClick={() => setShow(!show)}>{show ? 'Hide' : 'Show'}</button></span></label><ErrorMessage error={error} /><button className="primary-button" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button></form><p className="secondary-action"><a href="#forgot">Forgot password?</a> · <a href="#signup">Create an account</a></p></AccessLayout>
}

function Workspace() {
  const [businessName, setBusinessName] = useState(''); const [country, setCountry] = useState('IT'); const [currency, setCurrency] = useState('EUR'); const [vatNumber, setVatNumber] = useState(''); const [error, setError] = useState<string | null>(null); const [busy, setBusy] = useState(false)
  async function submit(event: React.FormEvent) { event.preventDefault(); setBusy(true); setError(null); try { await api.createWorkspace({ businessName, country, currency, vatNumber: vatNumber || null }, crypto.randomUUID()); window.location.hash = 'onboarding' } catch (caught) { setError(caught instanceof AccessApiError ? caught.problem.detail : 'We could not create this workspace. Try again.'); } finally { setBusy(false) } }
  return <AccessLayout title="Create your workspace"><p className="access-lede">A workspace keeps your team and stock scope together.</p><form className="access-form" onSubmit={submit}><label>Business name<input required value={businessName} onChange={(e) => setBusinessName(e.target.value)} /></label><div className="form-row"><label>Country<input required maxLength={2} value={country} onChange={(e) => setCountry(e.target.value.toUpperCase())} /></label><label>Currency<input required maxLength={3} value={currency} onChange={(e) => setCurrency(e.target.value.toUpperCase())} /></label></div><label>VAT number <span className="field-hint">Optional</span><input value={vatNumber} onChange={(e) => setVatNumber(e.target.value)} /></label><ErrorMessage error={error} /><button className="primary-button" disabled={busy}>{busy ? 'Creating workspace…' : 'Continue'}</button></form></AccessLayout>
}

function Onboarding() { return <AccessLayout title="A steady start"><p className="access-lede">Choose the next step. Each action stays honest about what is ready today.</p><div className="checklist" aria-label="Onboarding checklist"><a href="#deferred"><strong>Import products</strong><span>CSV template, preview, or manual add — Inventory will complete the import.</span></a><a href="#deferred"><strong>Connect a platform</strong><span>See the approved platform catalog — Platforms will complete the connection.</span></a><a href="#invite"><strong>Invite your team</strong><span>Create a secure invitation request — Team will complete administration and delivery.</span></a></div><button className="secondary-button" onClick={() => { window.location.hash = 'cover' }}>Back to Cover</button></AccessLayout> }

function Deferred({ invite = false }: { invite?: boolean }) { return <AccessLayout title={invite ? 'Invite your team' : 'Coming next'}><p className="access-lede">{invite ? 'Invitation requests will be secured here. Email delivery and full team administration are delivered by the Team milestone.' : 'This handoff is visible, but the owning capability is not complete yet.'}</p><p className="deferred-note">Nothing has been marked complete or sent. <a href="#onboarding">Return to the checklist</a>.</p></AccessLayout> }

function AccessRoute() { const [hash, setHash] = useState(window.location.hash); useEffect(() => { const update = () => setHash(window.location.hash); window.addEventListener('hashchange', update); return () => window.removeEventListener('hashchange', update) }, []); if (hash === '#signup') return <SignUp />; if (hash === '#signin') return <SignIn />; if (hash === '#workspace') return <Workspace />; if (hash === '#onboarding') return <Onboarding />; if (hash === '#invite') return <Deferred invite />; if (hash === '#forgot' || hash === '#deferred') return <Deferred />; return null }

export default function App() {
  if (typeof window !== 'undefined' && ['#signup', '#signin', '#workspace', '#onboarding', '#invite', '#forgot', '#deferred'].includes(window.location.hash)) return <AccessRoute />
  return (
    <main id="cover" className="cover-shell">
      <div className="cover-grid">
        <section className="hero-column" aria-labelledby="hero-title">
          <Logo />
          <h1 id="hero-title">StockHub</h1>
          <p className="hero-copy">
            One stock count and one price list for every marketplace you sell
            on, kept in sync in real time, in the browser.
          </p>
          <p className="access-actions"><a className="primary-button" href="#signup">Create your account</a><a className="text-button" href="#signin">Sign in</a></p>
          <p className="design-note">Final design · source of truth · 26 Sep 2026 · 1440 px, reflows to 1024 px</p>
          <StockModel />
        </section>
        <Contents />
      </div>
    </main>
  )
}
