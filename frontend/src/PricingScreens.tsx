import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { AccessApiClient, AccessApiError, type PricingPreview, type PricingRule, type PricingRuleInput, type SessionResponse, type WorkspaceSummary } from './api/generated'

const api = new AccessApiClient()
const platforms = ['Amazon', 'Unieuro', 'Euronics', 'eBay'] as const
const blank = (): PricingRuleInput => ({ name: '', platform: 'Amazon', scope: 'All', scopeValue: null, adjustment: 0, unit: 'Percent', enabled: true, priority: 100, startDate: null, endDate: null })
const errorText = (error: unknown) => error instanceof AccessApiError ? error.problem.detail : 'Could not complete that request. Try again.'
const dateStatus = (rule: PricingRule) => !rule.enabled ? 'Paused' : rule.startDate && rule.startDate > new Date().toISOString().slice(0, 10) ? 'Scheduled' : rule.endDate && rule.endDate < new Date().toISOString().slice(0, 10) ? 'Expired' : 'Active'

export function Pricing() {
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null)
  const [rules, setRules] = useState<PricingRule[]>([])
  const [canEdit, setCanEdit] = useState(false)
  const [draft, setDraft] = useState<PricingRuleInput>(blank())
  const [selected, setSelected] = useState<string | null>(null)
  const [preview, setPreview] = useState<PricingPreview | null>(null)
  const [query, setQuery] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [collapsed, setCollapsed] = useState(false)
  const [theme, setTheme] = useState(() => typeof localStorage === 'undefined' ? 'light' : localStorage.getItem('stockhub-theme') || 'light')
  const [menu, setMenu] = useState('')
  const [loading, setLoading] = useState(true)
  const dirty = useMemo(() => {
    if (!selected) return JSON.stringify(draft) !== JSON.stringify(blank())
    const rule = rules.find(item => item.id === selected)
    if (!rule) return false
    const { id: _id, ...input } = rule
    return JSON.stringify(draft) !== JSON.stringify(input)
  }, [draft, rules, selected])
  useEffect(() => { document.documentElement.dataset.theme = theme; localStorage.setItem('stockhub-theme', theme) }, [theme])
  useEffect(() => { void load() }, [])
  useEffect(() => {
    const beforeUnload = (event: BeforeUnloadEvent) => { if (dirty) { event.preventDefault(); event.returnValue = '' } }
    window.addEventListener('beforeunload', beforeUnload)
    return () => window.removeEventListener('beforeunload', beforeUnload)
  }, [dirty])
  useEffect(() => {
    const guardNavigation = (event: MouseEvent) => {
      const target = event.target
      if (!(target instanceof Element)) return
      const link = target.closest<HTMLAnchorElement>('a[href^="#"]')
      if (!link || !dirty) return
      if (!window.confirm('Discard your unsaved pricing rule changes?')) { event.preventDefault(); event.stopPropagation(); return }
      setSelected(null); setDraft(blank()); setPreview(null)
    }
    document.addEventListener('click', guardNavigation, true)
    return () => document.removeEventListener('click', guardNavigation, true)
  }, [dirty])
  async function load(preferred?: string) {
    setLoading(true); setError('')
    try {
      const s = await api.session(); const w = s.workspaces.find(item => item.id === (preferred || s.activeWorkspaceId)) || s.workspaces[0]
      if (!w) { window.location.hash = 'workspace'; return }
      const list = await api.pricingRules(w.id); setSession(s); setWorkspace(w); setRules(list.rules); setCanEdit(list.canEdit); setSelected(null); setDraft(blank()); setPreview(null)
    } catch (e) { if (e instanceof AccessApiError && e.status === 401) window.location.hash = 'signin'; else setError(errorText(e)) }
    finally { setLoading(false) }
  }
  function confirmDiscard() { return !dirty || window.confirm('Discard your unsaved pricing rule changes?') }
  async function chooseWorkspace(id: string) {
    if (!confirmDiscard()) return
    setMenu(''); setLoading(true); setRules([]); setWorkspace(null); setSelected(null); setDraft(blank()); setPreview(null)
    try { await api.setActiveWorkspace(id); await load(id) }
    catch (e) { setError(errorText(e)); if (workspace) await load(workspace.id) }
  }
  function chooseRule(rule?: PricingRule) { if (!confirmDiscard()) return; setSelected(rule?.id ?? null); setDraft(rule ? (({ id: _id, ...input }) => input)(rule) : blank()); setPreview(null); setError(''); setNotice('') }
  async function save(event: FormEvent) {
    event.preventDefault(); if (!workspace || !canEdit) return
    setBusy(true); setError(''); setNotice('')
    try { if (selected) await api.updatePricingRule(workspace.id, selected, draft); else await api.createPricingRule(workspace.id, draft); await load(workspace.id); setNotice('Pricing rule saved.') }
    catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }
  async function remove() { if (!workspace || !selected || !window.confirm('Delete this pricing rule? Existing prices will be recalculated.')) return; setBusy(true); try { await api.deletePricingRule(workspace.id, selected); await load(workspace.id); setNotice('Pricing rule deleted.') } catch (e) { setError(errorText(e)) } finally { setBusy(false) } }
  async function showPreview() { if (!workspace) return; setBusy(true); setError(''); try { setPreview(await api.previewPricing(workspace.id, draft, selected)) } catch (e) { setError(errorText(e)) } finally { setBusy(false) } }
  async function signOut() { try { await api.signOut(); window.location.hash = 'signin' } catch (e) { setError(errorText(e)) } }
  const visible = rules.filter(rule => `${rule.name} ${rule.platform} ${rule.scope} ${rule.scopeValue || ''}`.toLowerCase().includes(query.toLowerCase()))
  return <main className={`pricing-app ${collapsed ? 'pricing-collapsed' : ''}`}>
    <aside className="pricing-sidebar">
      <a className="pricing-brand" href="#dashboard"><b>SH</b><strong>StockHub</strong></a>
      <button className="pricing-workspace" onClick={() => setMenu(menu === 'workspace' ? '' : 'workspace')}><b>{workspace?.businessName.slice(0,2).toUpperCase() || 'SH'}</b><span><strong>{workspace?.businessName || 'Loading workspace'}</strong><small>{workspace?.role} · {session?.workspaces.length || 0} workspace(s)</small></span><span>⌄</span></button>
      {menu === 'workspace' && <div className="pricing-popover">{session?.workspaces.map(item => <button key={item.id} onClick={() => void chooseWorkspace(item.id)}>{item.businessName}<small>{item.role}</small></button>)}<a href="#workspace">Create workspace</a></div>}
      <nav aria-label="Main navigation">{[['Dashboard','#dashboard','▦'],['Inventory','#inventory','◇'],['Orders','#orders','▤'],['Reservations','#reservations','◷'],['Platforms','#platforms','♧'],['Pricing rules','#pricing','€'],['Reports','#reports','▥'],['Team','#team','♧'],['Settings','#settings','⚙']].map(([label,href,icon])=><a key={href} className={href==='#pricing'?'selected':''} href={href}><i>{icon}</i>{label}</a>)}</nav>
      <div className="pricing-sidebottom"><strong>Pricing rules</strong><small>{rules.length} saved rule{rules.length===1?'':'s'}</small><a href="#settings">Upgrade plan</a><button onClick={()=>setCollapsed(value=>!value)}>{collapsed?'Expand':'Collapse'} sidebar</button></div>
    </aside>
    <section className="pricing-content"><header className="pricing-top"><div className="pricing-search"><span>⌕</span><input value={query} onChange={e=>setQuery(e.target.value)} placeholder="Search rules, platforms, scopes…" aria-label="Search pricing rules" /></div><div className="pricing-top-actions"><button onClick={()=>{setMenu(menu==='sync'?'':'sync')}} className="sync-pill">● &nbsp; Sync status</button><button aria-label="Toggle appearance" onClick={()=>setTheme(theme==='dark'?'light':'dark')}>{theme==='dark'?'☀':'☾'}</button><button aria-label="Notifications" onClick={()=>setMenu(menu==='notifications'?'':'notifications')}>♧</button><button onClick={()=>setMenu(menu==='account'?'':'account')}>{session?.fullName || 'Account'} ⌄</button></div>{menu==='account'&&<div className="pricing-popover top-pop"><button onClick={()=>void signOut()}>Sign out</button></div>}{menu==='notifications'&&<div className="pricing-popover top-pop"><strong>Platform sync</strong><p>Review connection health and recent sync activity.</p><a href="#platforms">Open platforms</a></div>}{menu==='sync'&&<div className="pricing-popover top-pop"><strong>Sync status</strong><p>Platform connection status is managed in Platforms.</p><a href="#platforms">Review platforms</a></div>}</header>
      <div className="pricing-heading"><div><p className="pricing-eyebrow">CATALOG · PRICING</p><h1>Pricing rules</h1><p>Set consistent marketplace prices with clear, ordered adjustments.</p></div><button className="pricing-primary" disabled={!canEdit} onClick={()=>chooseRule()}>＋ New rule</button></div>
      {error&&<p className="pricing-error" role="alert">{error}</p>}{notice&&<p className="pricing-notice" role="status">{notice}</p>}
      <div className="pricing-layout"><section className="pricing-list-panel"><div className="pricing-list-head"><div><h2>Rules</h2><span>{visible.length} of {rules.length}</span></div><small>Lower priority number runs first</small></div>
        {loading?<p className="pricing-empty">Loading pricing rules…</p>:visible.length===0?<div className="pricing-empty"><b>{query?'No matching rules':'No pricing rules yet'}</b><p>{query?'Try another search.':'Create a rule to calculate marketplace prices from your base prices.'}</p>{!query&&<button disabled={!canEdit} onClick={()=>chooseRule()}>Create first rule</button>}</div>:<div className="pricing-rule-list">{visible.map(rule=><button key={rule.id} className={`pricing-rule-card ${selected===rule.id?'active':''}`} onClick={()=>chooseRule(rule)}><span className="rule-card-top"><strong>{rule.name}</strong><i className={`rule-status ${dateStatus(rule).toLowerCase()}`}>{dateStatus(rule)}</i></span><span className="rule-card-meta"><b>{rule.platform}</b><span>{rule.scope==='All'?'All products':`${rule.scope}: ${rule.scopeValue}`}</span><span>Priority {rule.priority}</span></span><span className="rule-adjust">{rule.adjustment>0?'+':''}{rule.adjustment}{rule.unit==='Percent'?'%':' €'} <small>adjustment</small></span><span className="rule-card-date">{rule.startDate||'Any date'} → {rule.endDate||'No end date'}</span></button>)}</div>}
      </section>
      <section className="pricing-editor-panel"><div className="pricing-editor-title"><div><p className="pricing-eyebrow">{selected?'EDIT RULE':'RULE EDITOR'}</p><h2>{selected?draft.name||'Edit rule':'Create a pricing rule'}</h2></div>{selected&&<button className="pricing-delete" disabled={!canEdit||busy} onClick={()=>void remove()}>Delete</button>}</div>
        {!canEdit&&<p className="pricing-readonly">You can view pricing rules, but only workspace owners and admins can change them.</p>}
        <form onSubmit={e=>void save(e)}>
          <label className="pricing-field">Rule name<input required maxLength={100} disabled={!canEdit||busy} value={draft.name} onChange={e=>setDraft({...draft,name:e.target.value})} placeholder="e.g. Amazon seasonal uplift" /></label>
          <div className="pricing-fields-2"><label className="pricing-field">Marketplace<select disabled={!canEdit||busy} value={draft.platform} onChange={e=>setDraft({...draft,platform:e.target.value as PricingRuleInput['platform']})}>{platforms.map(p=><option key={p}>{p}</option>)}</select></label><label className="pricing-field">Applies to<select disabled={!canEdit||busy} value={draft.scope} onChange={e=>setDraft({...draft,scope:e.target.value as PricingRuleInput['scope'],scopeValue:e.target.value==='All'?null:''})}><option value="All">All products</option><option value="Category">Category</option><option value="Sku">Single SKU</option></select></label></div>
          {draft.scope!=='All'&&<label className="pricing-field">{draft.scope==='Category'?'Category name':'Product SKU'}<input required disabled={!canEdit||busy} value={draft.scopeValue||''} onChange={e=>setDraft({...draft,scopeValue:e.target.value})} /></label>}
          <div className="pricing-fields-2"><label className="pricing-field">Price adjustment<input type="number" step="0.01" required disabled={!canEdit||busy} value={draft.adjustment} onChange={e=>setDraft({...draft,adjustment:Number(e.target.value)})} /></label><label className="pricing-field">Unit<select disabled={!canEdit||busy} value={draft.unit} onChange={e=>setDraft({...draft,unit:e.target.value as PricingRuleInput['unit']})}><option value="Percent">Percent (%)</option><option value="Euro">Fixed amount (€)</option></select></label></div>
          <div className="pricing-fields-2"><label className="pricing-field">Starts on<input type="date" disabled={!canEdit||busy} value={draft.startDate||''} onChange={e=>setDraft({...draft,startDate:e.target.value||null})} /></label><label className="pricing-field">Ends on<input type="date" disabled={!canEdit||busy} value={draft.endDate||''} onChange={e=>setDraft({...draft,endDate:e.target.value||null})} /></label></div>
          <div className="pricing-fields-2"><label className="pricing-field">Priority (1 runs first)<input type="number" min="1" max="999" required disabled={!canEdit||busy} value={draft.priority} onChange={e=>setDraft({...draft,priority:Number(e.target.value)})} /></label><label className="pricing-toggle"><input type="checkbox" disabled={!canEdit||busy} checked={draft.enabled} onChange={e=>setDraft({...draft,enabled:e.target.checked})} /> Rule is enabled</label></div>
          <div className="pricing-editor-actions"><button className="pricing-preview-button" type="button" disabled={busy||!draft.name.trim()} onClick={()=>void showPreview()}>{busy?'Working…':'Preview prices'}</button>{canEdit&&<button className="pricing-primary" type="submit" disabled={busy}>{selected?'Save changes':'Save rule'}</button>}{(selected||dirty)&&<button type="button" onClick={()=>chooseRule()}>Discard</button>}</div>
        </form>
        {preview&&<div className="pricing-preview"><div><span>Estimated final price</span><strong>{preview.finalPrice==null?'—':new Intl.NumberFormat('en-IE',{style:'currency',currency:'EUR'}).format(preview.finalPrice)}</strong></div><p>{preview.productName?`${preview.productName} · `:''}{preview.affectedProducts} product{preview.affectedProducts===1?'':'s'} affected. {preview.explanation}</p>{preview.margin==null&&<small>Margin unavailable until product cost is recorded.</small>}</div>}
        <p className="pricing-footnote">Rules run in priority order. Prices round to cents after each rule. VAT is 22%; marketplace fee assumptions are estimates. Margin stays blank until cost data is available.</p>
      </section></div>
    </section>
  </main>
}
