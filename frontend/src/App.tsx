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

export default function App() {
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
          <p className="design-note">Final design · source of truth · 26 Sep 2026 · 1440 px, reflows to 1024 px</p>
          <StockModel />
        </section>
        <Contents />
      </div>
    </main>
  )
}
