# Milestone 9 Team

Status: execution plan drafted from the approved mockup; GitHub milestone `9
Team` (milestone #10) is open with issues #135–#138. Do not begin implementation
until the plan and issue acceptance criteria are recorded in GitHub.

## Goal and mockup source

Deliver the `9.1 Team` workspace screen from `docs/mockups/StockHub Final.html`
and page 2 of `docs/mockups/StockHub Design Review.pdf`. The mockup presents
member and pending-invitation counts, Owner/Manager/Warehouse staff role cards,
an active and pending roster, member removal, invitation resend/cancel, and a
role permission matrix.

## User-visible behavior

- Load the active workspace's memberships and pending invitations from the
  server; show loading, empty, authorization, and request-error states.
- Show the member count, pending invitation count, names, email addresses,
  roles, last-active values when the existing account model can provide them,
  and the mockup's permission matrix. Never invent membership data or an
  activity timestamp.
- Invite a valid email with a supported non-owner role. Show delivery success
  only after the configured email sender accepts the request. If invitation
  delivery is unavailable, display the actual error and do not report success.
- Resend and cancel a pending invitation; remove an active member only after
  an explicit confirmation. The server must enforce workspace scope and the
  role rules for every operation.
- Reconcile the visual role cards with the actual authorization policies. The
  matrix is documentation only where a capability is not yet enforced; do not
  imply a permission is enforced if it is not.
- Preserve routes and working behavior from milestones 0–8, shared shell
  controls, responsive layout, accessibility labels, and the stock status
  color/icon/label conventions.

## Contract decisions and boundaries

- Preserve the persisted roles. The visible cards remain Owner, Manager, and
  Warehouse staff; include the existing Admin and Viewer roles in the full
  permission matrix so the screen does not hide roles already present in the
  Access model. Admin keeps its existing manager-level operational access and
  existing Team invite authority. Owner and Admin can manage invitations and
  members; Manager, Warehouse staff, and Viewer cannot. This preserves current
  Admin behavior while making the mockup's Manager restriction explicit.
- Permission rows follow the existing API behavior plus the mockup: members
  can view stock, orders, and reservations; Warehouse staff can adjust on-hand
  stock but cannot access price data; Owner/Admin/Manager can see and change
  prices and pricing rules and can edit report alerts. Platform connection
  management and billing are owner/admin capabilities. Before implementation,
  verify each row against its endpoint and do not claim finer permissions
  unless those endpoints enforce them.
- Treat memberships and invitations as Access-domain state. Extend the Access
  store and workspace-scoped Access endpoints; do not query another module's
  tables directly.
- `Owner` is assigned only at workspace creation and cannot be invited or
  granted by this screen. A Manager cannot invite or manage members.
- Invitation tokens remain one-way hashed at rest. Resend must safely rotate
  the token and invalidate the old link. Cancellation must invalidate the
  invitation. Acceptance remains bound to the invited email and current
  expiry.
- Membership removal must prevent removing the last Owner and the current
  user's own active session should be invalidated or redirected after removal.
- Use the existing persistence and migration conventions. No new application
  or service is introduced, so keep the existing Docker/Compose health checks
  and CI image smoke gates intact.
- The mockup's `Last active` field is not currently represented by the
  membership contract. Show a neutral unavailable label until real activity
  tracking exists; do not use invitation time as a member's last-active time.
- Keep the existing invitation endpoint's configured email sender and
  72-hour expiry. A delivery failure must not leave a row that appears as a
  valid pending invite; retryable send behavior must be explicit. Pending
  invites are not memberships until accepted.
- Removing a membership takes effect on the next authorized request because
  workspace access is checked against current membership state per request.
  Preserve the session, but clear/redirect away from a workspace the user no
  longer belongs to.

## Proposed issue decomposition

Create implementation issues in the existing `9 Team` milestone (#10), with
direct mockup links and acceptance criteria:

1. **Freeze Team role, membership, and invitation contract**
   - Record the decisions above, including the existing Admin compatibility
     behavior, workspace ownership rules, invitation lifecycle, removal
     semantics, unavailable last-active field, and error states.
   - Confirm them against existing Access policies, auth/session behavior, and
     the Team mockup before API/UI implementation.
2. **Implement workspace-scoped membership and invitation management**
   - Add member/invitation reads plus safe resend, cancel, and removal
     operations with durable persistence and authorization on every request.
   - Cover owner constraints, tenant isolation, token rotation, expiry,
     delivery failure, and removed-member access.
3. **Implement Team screen and invitation interactions**
   - Match the roster, role cards, permission matrix, invite dialog, and
     pending-invitation actions; persist every action through the API.
   - Provide clear loading/empty/error/saved states and responsive layouts.
4. **Integrate, regress, and record Team milestone evidence**
   - Review the integrated changes, verify visible controls and earlier routes,
     run required frontend/backend/contract/container/CI checks, inspect PRs
     and delivery jobs, and prepare the PASS closure evidence.
   - Close only after implementation issues are resolved by merged PRs and the
     milestone closure checker passes.

The live implementation issues, all open in GitHub milestone #10 `9 Team`,
are:

- [#135 Freeze Team roles, membership, and invitation contract](https://github.com/JavadEslamibabaheidari/StockHub/issues/135)
- [#136 Implement workspace-scoped Team member and invitation management](https://github.com/JavadEslamibabaheidari/StockHub/issues/136)
- [#137 Implement responsive Team screen and invitation interactions](https://github.com/JavadEslamibabaheidari/StockHub/issues/137)
- [#138 Integrate, regress, and close Milestone 9 Team](https://github.com/JavadEslamibabaheidari/StockHub/issues/138)

## Dependencies and execution

Issue 1 is the start gate. Once its contract is recorded, API/persistence work
and Team screen structure may proceed in parallel only with a shared stable
contract and disjoint file ownership. Integration, test ownership, review, and
closure evidence stay with the coordinator.

## Verification and closure gate

- Run the milestone closure hook for all available earlier milestone reports;
  verify the live GitHub state and latest relevant delivery runs before making
  regression claims.
- Verify Owner, Manager, Warehouse staff, and Viewer behavior, workspace
  isolation, invitation acceptance, invitation invalidation, and removal.
- Verify Team actions at desktop and narrow widths; exercise all visible
  buttons, keyboard operation, validation, and request failures.
- Run project-required frontend/backend/contract checks and Compose/container
  readiness checks. Inspect CI jobs individually and distinguish image
  publication from local or hosted deployment verification.
- Before closure, refresh `main` and `dev`, run
  `scripts/check-tracking-status.sh`, update `docs/project-status.md`, the
  roadmap, knowledge docs, and the closure report in the delivery. Close GitHub
  milestone #10 only with zero open issues and a `PASS` closure report.
