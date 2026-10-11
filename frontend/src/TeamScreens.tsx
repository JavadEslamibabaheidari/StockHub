import { useEffect, useMemo, useRef, useState, type FormEvent, type KeyboardEvent, type ReactNode } from 'react'
import { toggleExplicitTheme } from './appearance'
import {
  AccessApiClient,
  AccessApiError,
  type PendingInvitationResponse,
  type TeamMemberResponse,
  type TeamResponse,
  type WorkspaceRole,
  type WorkspaceSummary,
} from './api/generated'

const api = new AccessApiClient()
const nav = [
  ['Dashboard', '#dashboard'], ['Inventory', '#inventory'], ['Orders', '#orders'],
  ['Reservations', '#reservations'], ['Platforms', '#platforms'], ['Pricing rules', '#pricing'],
  ['Reports', '#reports'], ['Team', '#team'], ['Settings', '#settings'],
] as const
const inviteRoles: WorkspaceRole[] = ['Admin', 'Manager', 'WarehouseStaff', 'Viewer']
const roleNames: Record<WorkspaceRole, string> = {
  Owner: 'Owner', Admin: 'Admin', Manager: 'Manager', WarehouseStaff: 'Warehouse staff', Viewer: 'Viewer',
}
const roleDescriptions: Record<string, string> = {
  Owner: 'Everything, including billing and Team',
  Manager: 'Stock, prices, and reports; cannot manage Team',
  WarehouseStaff: 'Updates on-hand counts; cannot see or change prices',
}
const matrixRows: { label: string; allows: WorkspaceRole[] }[] = [
  { label: 'View stock, orders, and reservations', allows: ['Owner', 'Admin', 'Manager', 'WarehouseStaff', 'Viewer'] },
  { label: 'Edit on-hand counts', allows: ['Owner', 'Admin', 'Manager', 'WarehouseStaff'] },
  { label: 'See prices, fees, and margins', allows: ['Owner', 'Admin', 'Manager'] },
  { label: 'Change prices and pricing rules', allows: ['Owner', 'Admin', 'Manager'] },
  { label: 'Connect, pause, or disconnect platforms', allows: ['Owner', 'Admin'] },
  { label: 'View reports and alert settings', allows: ['Owner', 'Admin', 'Manager'] },
  { label: 'Invite and manage Team members', allows: ['Owner', 'Admin'] },
  { label: 'Billing and plan', allows: ['Owner', 'Admin'] },
]
type Panel = { title: string; body: string; tone?: 'ok' | 'warn' } | null
type PendingAction = { kind: 'resend' | 'cancel' | 'remove'; id: string; label: string } | null
const errorMessage = (error: unknown, fallback: string) => error instanceof AccessApiError ? error.problem.detail || fallback : fallback
const initials = (value: string) => value.trim().split(/\s+/).slice(0, 2).map(part => part[0]?.toUpperCase()).join('') || 'SH'
const formatDate = (value: string) => {
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? 'Unavailable' : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}
const isEmail = (value: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())

function TeamShell({
  children, workspace, workspaces, fullName, query, setQuery, panel, setPanel, onWorkspace,
}: {
  children: ReactNode
  workspace: WorkspaceSummary | null
  workspaces: WorkspaceSummary[]
  fullName: string
  query: string
  setQuery: (value: string) => void
  panel: Panel
  setPanel: (value: Panel) => void
  onWorkspace: (id: string) => void
}) {
  const [workspaceOpen, setWorkspaceOpen] = useState(false)
  const [accountOpen, setAccountOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)

  async function signOut() {
    try {
      await api.signOut()
      window.location.hash = '#signin'
    } catch (error) {
      setPanel({ title: 'Could not sign out', body: errorMessage(error, 'Please try again.'), tone: 'warn' })
    }
  }

  return (
    <main className={`inventory-page team-page ${collapsed ? 'team-collapsed' : ''}`}>
      <div className="inventory-shell">
        <aside className="inventory-sidebar">
          <button className="inventory-workspace reservation-workspace-button" type="button" aria-label="Switch workspace" aria-expanded={workspaceOpen} onClick={() => setWorkspaceOpen(open => !open)}>
            <b>{initials(workspace?.businessName || 'StockHub')}</b>
            <span><strong>{workspace?.businessName || 'Your workspace'}</strong><small>{roleNames[workspace?.role || 'Owner']} · {workspaces.length} workspace{workspaces.length === 1 ? '' : 's'}</small></span>
            <span aria-hidden="true">⌄</span>
          </button>
          {workspaceOpen && <div className="dashboard-popover workspace-menu" aria-label="Workspaces">
            {workspaces.map(item => <button key={item.id} type="button" aria-current={item.id === workspace?.id ? 'true' : undefined} onClick={() => { setWorkspaceOpen(false); onWorkspace(item.id) }}>{item.businessName}<small>{roleNames[item.role]}</small></button>)}
            <a href="#workspace">Create workspace</a>
          </div>}
          <nav aria-label="Main navigation">{nav.map(([label, href]) => <a key={href} className={label === 'Team' ? 'active' : ''} aria-current={label === 'Team' ? 'page' : undefined} href={href}>{label}</a>)}</nav>
          <div className="inventory-plan"><strong>Workspace Team</strong><a href="#settings">Plan settings</a><span /><small>Member access</small></div>
          <button className="sidebar-collapse-button" type="button" onClick={() => setCollapsed(value => !value)} aria-expanded={!collapsed}>{collapsed ? 'Expand navigation' : 'Collapse navigation'}</button>
        </aside>
        <section className="inventory-main">
          <header className="inventory-top">
            <label className="inventory-search"><span>Search members and invitations</span><input aria-label="Search members and invitations" value={query} onChange={event => setQuery(event.target.value)} placeholder="Search members and invitations…" /></label>
            <div className="inventory-user">
              <button className="sync-ok" type="button" onClick={() => setPanel({ title: 'Team data', body: 'Member and invitation data is loaded from the active workspace.' })}>Team data</button>
              <button type="button" aria-label="Toggle appearance" onClick={toggleExplicitTheme}>☾</button>
              <button type="button" aria-label="Notifications" aria-expanded={notificationsOpen} onClick={() => setNotificationsOpen(open => !open)}>!</button>
              <button className="reservation-account" type="button" onClick={() => setAccountOpen(open => !open)} aria-expanded={accountOpen}>
                <b>{initials(fullName)}</b><span>{fullName}<small>{roleNames[workspace?.role || 'Owner']}</small></span><span aria-hidden="true">⌄</span>
              </button>
            </div>
          </header>
          {notificationsOpen && <section className="dashboard-popover team-popover" role="status"><p>No new Team notifications.</p><button type="button" onClick={() => setNotificationsOpen(false)}>Close</button></section>}
          {accountOpen && <section className="dashboard-popover account-menu reservation-account-menu"><a href="#settings">Account settings</a><button type="button" onClick={() => setPanel({ title: 'Account profile', body: 'Account profile settings are available from Settings.' })}>View profile</button><button type="button" onClick={() => void signOut()}>Sign out</button></section>}
          {children}
          <nav className="team-mobile-nav" aria-label="Main navigation">{nav.map(([label, href]) => <a key={href} className={label === 'Team' ? 'active' : ''} aria-current={label === 'Team' ? 'page' : undefined} href={href}>{label}</a>)}</nav>
        </section>
      </div>
      {panel && <section className={`inventory-panel ${panel.tone || ''}`} role="status"><button type="button" aria-label="Close panel" onClick={() => setPanel(null)}>×</button><h2>{panel.title}</h2><p>{panel.body}</p></section>}
    </main>
  )
}

function Modal({ title, onClose, returnFocusTo, children }: { title: string; onClose: () => void; returnFocusTo: HTMLElement | null; children: ReactNode }) {
  const dialogRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const firstControl = dialogRef.current?.querySelector<HTMLElement>('[data-team-autofocus]') || dialogRef.current?.querySelector<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), [href], [tabindex="0"]')
    ;(firstControl || dialogRef.current)?.focus()
    return () => returnFocusTo?.focus()
  }, [returnFocusTo])
  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') { event.preventDefault(); onClose(); return }
    if (event.key !== 'Tab') return
    const items = [...(event.currentTarget.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), [href], [tabindex="0"]'))]
      .filter(item => !item.hasAttribute('hidden') && item.getAttribute('aria-hidden') !== 'true')
    if (!items.length) return
    const first = items[0]
    const last = items[items.length - 1]
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
  }
  return <div className="team-modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose() }}><div ref={dialogRef} className="team-modal" role="dialog" aria-modal="true" aria-labelledby="team-dialog-title" tabIndex={-1} onKeyDown={onKeyDown}><button className="team-modal-close" type="button" aria-label="Close dialog" onClick={onClose}>×</button><h2 id="team-dialog-title">{title}</h2>{children}</div></div>
}

function RoleCards({ members }: { members: TeamMemberResponse[] }) {
  return <section className="team-role-cards" aria-label="Team roles">
    {(['Owner', 'Manager', 'WarehouseStaff'] as const).map(role => <article className={`team-role-card team-role-${role.toLowerCase()}`} key={role}>
      <span>{roleNames[role]}</span><strong>{members.filter(member => member.role === role).length}</strong><small>{roleDescriptions[role]}</small>
    </article>)}
  </section>
}

function PermissionMatrix() {
  const roles: WorkspaceRole[] = ['Owner', 'Admin', 'Manager', 'WarehouseStaff', 'Viewer']
  return <section className="inventory-card team-permissions" aria-labelledby="team-permissions-title">
    <header><div><h2 id="team-permissions-title">What each role can do</h2><p>Access follows the active workspace role.</p></div></header>
    <div className="team-matrix-scroll" role="region" aria-label="Team permission matrix" tabIndex={0}>
      <table><thead><tr><th scope="col">Permission</th>{roles.map(role => <th scope="col" key={role}>{roleNames[role]}</th>)}</tr></thead>
        <tbody>{matrixRows.map(row => <tr key={row.label}><th scope="row">{row.label}</th>{roles.map(role => <td key={role}><span className={row.allows.includes(role) ? 'team-permission-yes' : 'team-permission-no'}>{row.allows.includes(role) ? 'Yes' : 'No'}</span></td>)}</tr>)}</tbody>
      </table>
    </div>
    <p className="team-matrix-note">Owners and Admins manage Team access. Managers can manage stock and pricing. Warehouse staff and Viewers cannot access price data or manage Team members.</p>
  </section>
}

export function Team() {
  const [workspace, setWorkspace] = useState<WorkspaceSummary | null>(null)
  const [workspaces, setWorkspaces] = useState<WorkspaceSummary[]>([])
  const [fullName, setFullName] = useState('Your account')
  const [userId, setUserId] = useState('')
  const [team, setTeam] = useState<TeamResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [workspaceError, setWorkspaceError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [reload, setReload] = useState(0)
  const [panel, setPanel] = useState<Panel>(null)
  const [showInvite, setShowInvite] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState<WorkspaceRole>('Manager')
  const [inviteError, setInviteError] = useState<string | null>(null)
  const [inviteBusy, setInviteBusy] = useState(false)
  const [pendingAction, setPendingAction] = useState<PendingAction>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [returnFocusTo, setReturnFocusTo] = useState<HTMLElement | null>(null)

  useEffect(() => {
    let active = true
    api.session().then(session => {
      if (!active) return
      const selected = session.workspaces.find(item => item.id === session.activeWorkspaceId) || session.workspaces[0] || null
      setUserId(session.userId)
      setFullName(session.fullName || 'Your account')
      setWorkspaces(session.workspaces)
      if (!selected) { window.location.hash = '#workspace'; return }
      setWorkspace(selected)
    }).catch(reason => {
      if (!active) return
      if (reason instanceof AccessApiError && reason.status === 401) window.location.hash = '#signin'
      else setError(errorMessage(reason, 'Could not load your workspace.'))
      setLoading(false)
    })
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!workspace) return
    let active = true
    setLoading(true)
    setError(null)
    setTeam(null)
    api.team(workspace.id).then(result => { if (active) setTeam(result) }).catch(reason => {
      if (!active) return
      if (reason instanceof AccessApiError && reason.status === 401) window.location.hash = '#signin'
      else setError(errorMessage(reason, 'Could not load Team members.'))
    }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [workspace?.id, reload])

  const filteredMembers = useMemo(() => (team?.members || []).filter(member => `${member.fullName} ${member.email} ${roleNames[member.role]}`.toLowerCase().includes(query.trim().toLowerCase())), [team, query])
  const filteredInvitations = useMemo(() => (team?.invitations || []).filter(invitation => `${invitation.email} ${roleNames[invitation.role]}`.toLowerCase().includes(query.trim().toLowerCase())), [team, query])

  async function switchWorkspace(id: string) {
    if (id === workspace?.id) return
    setWorkspaceError(null)
    setTeam(null)
    setLoading(true)
    try {
      await api.setActiveWorkspace(id)
      const next = workspaces.find(item => item.id === id)
      if (next) setWorkspace(next)
    } catch (reason) {
      setWorkspaceError(errorMessage(reason, 'Could not switch workspace.'))
      setLoading(false)
    }
  }

  async function submitInvite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const email = inviteEmail.trim()
    if (!isEmail(email)) { setInviteError('Enter a valid email address.'); return }
    if (!inviteRoles.includes(inviteRole)) { setInviteError('Choose a role that can be invited.'); return }
    if (!workspace) return
    setInviteBusy(true)
    setInviteError(null)
    try {
      await api.invite(workspace.id, { email, role: inviteRole })
      setShowInvite(false)
      setInviteEmail('')
      setPanel({ title: 'Invitation sent', body: `The invitation email was sent to ${email}.`, tone: 'ok' })
      setReload(value => value + 1)
    } catch (reason) {
      setInviteError(errorMessage(reason, 'The invitation email could not be sent. Try again.'))
    } finally { setInviteBusy(false) }
  }

  async function executeAction() {
    if (!pendingAction || !workspace) return
    setActionBusy(true)
    setActionError(null)
    const action = pendingAction
    try {
      if (action.kind === 'resend') await api.resendInvitation(workspace.id, action.id)
      else if (action.kind === 'cancel') await api.cancelInvitation(workspace.id, action.id)
      else await api.removeTeamMember(workspace.id, action.id)
      const title = action.kind === 'resend' ? 'Invitation resent' : action.kind === 'cancel' ? 'Invitation cancelled' : 'Member removed'
      const body = action.kind === 'resend' ? `A new invitation email was sent to ${action.label}. The previous link is no longer valid.` : action.kind === 'cancel' ? `The pending invitation for ${action.label} was cancelled.` : `${action.label} no longer has access to ${workspace.businessName}.`
      setPendingAction(null)
      setPanel({ title, body, tone: 'ok' })
      setReload(value => value + 1)
    } catch (reason) {
      setActionError(errorMessage(reason, 'The Team action could not be completed. Try again.'))
    } finally { setActionBusy(false) }
  }

  function requestAction(action: Exclude<PendingAction, null>, trigger: HTMLElement) {
    setReturnFocusTo(trigger)
    setActionError(null)
    setPendingAction(action)
  }

  const members = team?.members || []
  const invitations = team?.invitations || []
  return <TeamShell workspace={workspace} workspaces={workspaces} fullName={fullName} query={query} setQuery={setQuery} panel={panel} setPanel={setPanel} onWorkspace={id => void switchWorkspace(id)}>
    <header className="inventory-heading team-heading">
      <div><span className="inventory-eyebrow">WORKSPACE ACCESS</span><h1>Team</h1><p>{workspace ? `${workspace.businessName} · ${members.length} member${members.length === 1 ? '' : 's'} · ${invitations.length} pending invitation${invitations.length === 1 ? '' : 's'}` : 'Manage workspace access'}</p></div>
      {team?.canManage && <button className="team-primary-button" type="button" onClick={event => { setReturnFocusTo(event.currentTarget); setInviteError(null); setShowInvite(true) }}>Invite member</button>}
    </header>
    {workspaceError && <div className="team-inline-error" role="alert">{workspaceError}</div>}
    {team && !team.canManage && <div className="team-readonly-note" role="status"><strong>Read-only access</strong><span>Only workspace Owners and Admins can invite or manage Team members.</span></div>}
    {loading && <div className="inventory-card team-state" role="status">Loading Team members…</div>}
    {!loading && error && <div className="inventory-card team-state team-error" role="alert"><strong>Team could not be loaded</strong><p>{error}</p><button type="button" onClick={() => setReload(value => value + 1)}>Try again</button></div>}
    {!loading && !error && team && <>
      <RoleCards members={members} />
      <section className="inventory-card team-roster" aria-labelledby="team-members-title">
        <header className="team-section-heading"><div><h2 id="team-members-title">Team members</h2><p>{members.length} member{members.length === 1 ? '' : 's'} · {invitations.length} pending invitation{invitations.length === 1 ? '' : 's'} · {workspace?.businessName}</p></div><span className="team-count">{members.length} active</span></header>
        {members.length === 0 ? <div className="team-empty"><strong>No members yet</strong><p>Workspace members will appear here when they join.</p></div> : <div className="team-table-scroll" role="region" aria-label="Team members" tabIndex={0}><table className="team-table"><thead><tr><th scope="col">Member</th><th scope="col">Email</th><th scope="col">Role</th><th scope="col">Last active</th>{team.canManage && <th scope="col"><span className="sr-only">Actions</span></th>}</tr></thead><tbody>
          {filteredMembers.map(member => <tr key={member.userId}><th scope="row"><span className="team-person"><b aria-hidden="true">{initials(member.fullName)}</b><span>{member.fullName}{member.userId === userId && <small>You</small>}</span></span></th><td>{member.email}</td><td><span className={`team-role-pill role-${member.role.toLowerCase()}`}>{roleNames[member.role]}</span></td><td>{member.lastActive || 'Unavailable'}</td>{team.canManage && <td className="team-row-actions">{member.userId !== userId && <button type="button" className="team-text-action danger" aria-label={`Remove ${member.fullName}`} onClick={event => requestAction({ kind: 'remove', id: member.userId, label: member.fullName }, event.currentTarget)}>Remove</button>}</td>}</tr>)}
          {filteredMembers.length === 0 && query.trim() && <tr><td colSpan={team.canManage ? 5 : 4} className="team-filter-empty">No members match “{query.trim()}”.</td></tr>}
        </tbody></table></div>}
      </section>
      <section className="inventory-card team-invitations" aria-labelledby="team-invitations-title">
        <header className="team-section-heading"><div><h2 id="team-invitations-title">Pending invitations</h2><p>Invitations expire after 72 hours.</p></div><span className="team-count">{invitations.length} pending</span></header>
        {filteredInvitations.length === 0 ? <div className="team-empty"><strong>{query.trim() ? 'No invitations match your search' : 'No pending invitations'}</strong><p>{query.trim() ? 'Try a different name or email.' : team.canManage ? 'Invited people will appear here until they join or the link expires.' : 'There are no open invitations for this workspace.'}</p></div> : <div className="team-table-scroll" role="region" aria-label="Pending invitations" tabIndex={0}><table className="team-table team-invite-table"><thead><tr><th scope="col">Email</th><th scope="col">Role</th><th scope="col">Invited</th><th scope="col">Expires</th>{team.canManage && <th scope="col"><span className="sr-only">Actions</span></th>}</tr></thead><tbody>{filteredInvitations.map(invitation => <tr key={invitation.id}><th scope="row"><span className="team-pending-email">{invitation.email}<small>Invite pending</small></span></th><td><span className={`team-role-pill role-${invitation.role.toLowerCase()}`}>{roleNames[invitation.role]}</span></td><td>{formatDate(invitation.createdAt)}</td><td>{formatDate(invitation.expiresAt)}</td>{team.canManage && <td className="team-row-actions"><button type="button" className="team-text-action" onClick={event => requestAction({ kind: 'resend', id: invitation.id, label: invitation.email }, event.currentTarget)}>Resend</button><button type="button" className="team-text-action danger" onClick={event => requestAction({ kind: 'cancel', id: invitation.id, label: invitation.email }, event.currentTarget)}>Cancel invite</button></td>}</tr>)}</tbody></table></div>}
      </section>
      <PermissionMatrix />
    </>}

    {showInvite && <Modal title="Invite a team member" returnFocusTo={returnFocusTo} onClose={() => { if (!inviteBusy) { setShowInvite(false); setInviteError(null) } }}><p className="team-modal-intro">Send an invitation to join {workspace?.businessName || 'this workspace'}. The link expires after 72 hours.</p><form className="team-form" noValidate onSubmit={event => void submitInvite(event)}>
      <label>Email address<input autoFocus data-team-autofocus type="email" autoComplete="email" value={inviteEmail} onChange={event => { setInviteEmail(event.target.value); setInviteError(null) }} placeholder="name@example.com" aria-invalid={Boolean(inviteError)} aria-describedby={inviteError ? 'team-invite-error' : undefined} /></label>
      <label>Workspace role<select value={inviteRole} onChange={event => setInviteRole(event.target.value as WorkspaceRole)}>{inviteRoles.map(role => <option key={role} value={role}>{roleNames[role]}</option>)}</select></label>
      {inviteError && <p className="team-form-error" id="team-invite-error" role="alert">{inviteError}</p>}
      <p className="team-form-note">Owners cannot be invited. Only Owners and Admins can manage Team access.</p>
      <div className="team-modal-actions"><button type="button" disabled={inviteBusy} onClick={() => { setShowInvite(false); setInviteError(null) }}>Cancel</button><button type="submit" className="team-primary-button" disabled={inviteBusy}>{inviteBusy ? 'Sending…' : 'Send invitation'}</button></div>
    </form></Modal>}
    {pendingAction && <Modal title={pendingAction.kind === 'resend' ? 'Resend invitation?' : pendingAction.kind === 'cancel' ? 'Cancel invitation?' : 'Remove team member?'} returnFocusTo={returnFocusTo} onClose={() => { if (!actionBusy) { setPendingAction(null); setActionError(null) } }}>
      <p className="team-modal-intro">{pendingAction.kind === 'resend' ? `A new invitation email will be sent to ${pendingAction.label}. The existing invitation link will stop working.` : pendingAction.kind === 'cancel' ? `The invitation for ${pendingAction.label} will be invalidated. They will no longer be able to use its link.` : `${pendingAction.label} will lose access to ${workspace?.businessName || 'this workspace'} immediately.`}</p>
      {actionError && <p className="team-form-error" role="alert">{actionError}</p>}
      <div className="team-modal-actions"><button type="button" disabled={actionBusy} onClick={() => { setPendingAction(null); setActionError(null) }}>Cancel</button><button type="button" className={pendingAction.kind === 'resend' ? 'team-primary-button' : 'team-danger-button'} disabled={actionBusy} onClick={() => void executeAction()}>{actionBusy ? 'Working…' : pendingAction.kind === 'resend' ? 'Resend invitation' : pendingAction.kind === 'cancel' ? 'Cancel invitation' : 'Remove member'}</button></div>
    </Modal>}
  </TeamShell>
}
