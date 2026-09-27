# GitHub issue hygiene

Status: active repository policy

GitHub issues are the source of truth for active work. Every issue should have
one clear objective, acceptance criteria, a type label, a priority label, and a
status label. Work that is a child of another issue must use GitHub's parent
issue relationship; references in prose alone do not establish hierarchy.

Before implementation:

1. Confirm the issue is open and unparented or identify its parent.
2. Confirm the milestone and labels match the work's delivery scope.
3. Check dependencies and blocked status against the repository and accepted
   architecture decisions.

During implementation:

- Keep the pull request linked to its issue with `Refs #N` or `Closes #N`.
- Record deliberate scope changes as follow-up issues.
- Keep milestone and status metadata synchronized with the actual delivery
  state.

After implementation:

- Attach verification evidence to the pull request.
- Close the issue only after its acceptance criteria are met in the merged
  repository state.
- Update supporting plans when the GitHub state changes.

The repository currently has no accessible GitHub Project board because the
authenticated CLI token lacks the `read:project` scope. Board synchronization
must be completed after that scope is granted; no board state is inferred from
issue labels or milestones.
