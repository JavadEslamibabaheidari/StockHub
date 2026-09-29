import { useEffect, useState } from 'react'
import { AcceptInvite, Deferred, ForgotPassword, Invite, Onboarding, ResetPassword, SignIn, SignUp, Workspace } from './AccessScreens'

const accessRouteHashes = [
  '#signup',
  '#signin',
  '#workspace',
  '#onboarding',
  '#invite',
  '#forgot',
  '#accept',
  '#reset',
  '#legal',
  '#deferred',
  '#inventory',
  '#orders',
  '#reservations',
  '#platforms',
  '#pricing',
  '#reports',
  '#team',
  '#settings',
] as const

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
        <span className="equation-mark" aria-hidden="true">
          −
        </span>
        <article className="number-card reserved-card">
          <strong>
            <span className="clock-icon" aria-hidden="true">
              ◷
            </span>{' '}
            Reserved
          </strong>
          <span>Booked, not paid · held 10 min</span>
        </article>
        <span className="equation-mark" aria-hidden="true">
          =
        </span>
        <article className="number-card available-card">
          <strong>Available</strong>
          <span>What every platform sees</span>
        </article>
      </div>

      <h2 className="status-heading">Status colours · always with an icon or label</h2>
      <div className="status-list" aria-label="StockHub status meanings">
        <span className="status-pill healthy">
          <i aria-hidden="true" />
          Synced · healthy
        </span>
        <span className="status-pill warning">
          <i aria-hidden="true" />
          Low stock · expiring
        </span>
        <span className="status-pill danger">
          <i aria-hidden="true" />
          Failed · out of stock
        </span>
        <span className="status-pill neutral">
          <i aria-hidden="true" />
          Paused · reserved · neutral
        </span>
      </div>
    </section>
  )
}

export default function App() {
  const [hash, setHash] = useState(typeof window === 'undefined' ? '' : window.location.hash)
  useEffect(() => {
    const update = () => setHash(window.location.hash)
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])
  if (accessRouteHashes.includes(hash as (typeof accessRouteHashes)[number]) || hash.startsWith('#reset?') || hash.startsWith('#accept?')) {
    if (hash === '#signup') return <SignUp />
    if (hash === '#signin') return <SignIn />
    if (hash === '#workspace') return <Workspace />
    if (hash === '#onboarding') return <Onboarding />
    if (hash === '#invite') return <Invite />
    if (hash === '#forgot') return <ForgotPassword />
    if (hash === '#accept' || hash.startsWith('#accept?')) return <AcceptInvite />
    if (hash === '#reset' || hash.startsWith('#reset?')) return <ResetPassword />
    if (hash === '#legal') return <Deferred legal />
    return <Deferred />
  }

  return (
    <main id="cover" className="cover-shell">
      <div className="cover-grid">
        <section className="hero-column" aria-labelledby="hero-title">
          <Logo />
          <h1 id="hero-title">StockHub</h1>
          <p className="hero-copy">
            One stock count and one price list for every marketplace you sell on, kept in sync in real time, in the
            browser.
          </p>
          <p className="access-actions">
            <a className="primary-button" href="#signup">
              Create your account
            </a>
            <a className="text-button" href="#signin">
              Sign in
            </a>
          </p>
          <StockModel />
        </section>
      </div>
    </main>
  )
}
