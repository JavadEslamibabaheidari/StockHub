# Project status and evidence

Last reconciled: 2026-09-29. Live [GitHub milestones](https://github.com/JavadEslamibabaheidari/StockHub/milestones), [issues](https://github.com/JavadEslamibabaheidari/StockHub/issues), and [Actions](https://github.com/JavadEslamibabaheidari/StockHub/actions) take precedence over this snapshot.

| Milestone | Code | Published image | Local dev | Local staging / production | Closure |
|---|---|---|---|---|---|
| [0 Cover](https://github.com/JavadEslamibabaheidari/StockHub/milestone/2) | Merged | Not applicable to the static Cover milestone | Not applicable | Not applicable | Closed; report `PASS` |
| [1 Access](https://github.com/JavadEslamibabaheidari/StockHub/milestone/1) | Implementation and [remediation PR #66](https://github.com/JavadEslamibabaheidari/StockHub/pull/66) merged | Main image pipeline exists; check the current SHA and run before claiming publication | Current `main` was started in the local dev Compose project, but real Google callback, outbound email, and deployed browser journeys remain unverified | Not deployed | Open: [#27](https://github.com/JavadEslamibabaheidari/StockHub/issues/27), [#35](https://github.com/JavadEslamibabaheidari/StockHub/issues/35), [#65](https://github.com/JavadEslamibabaheidari/StockHub/issues/65); report `BLOCKED` |
| [2 Dashboard](https://github.com/JavadEslamibabaheidari/StockHub/milestone/3) | [PR #68](https://github.com/JavadEslamibabaheidari/StockHub/pull/68) merged; [PR #69](https://github.com/JavadEslamibabaheidari/StockHub/pull/69) recorded evidence | [Main run 36616827440](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/36616827440) published the image for `444c1da` | Local `dev-stockhub` Compose project is running `146191e` on `localhost:8081`; `/ready` and frontend responded in the environment task on 2026-09-29. Automated dev-branch promotion is not yet verified | Not deployed; no validated Milestone 2 tag | Open: delivery gate [#47](https://github.com/JavadEslamibabaheidari/StockHub/issues/47); report `IN PROGRESS` |

[Issue #48](https://github.com/JavadEslamibabaheidari/StockHub/issues/48) owns future shared hosted staging and production activation and has no product milestone. The first dev, staging, and production targets are separate localhost-only Compose projects on this PC. The 14 open pull requests observed on 2026-09-29 are Dependabot dependency updates. Count them as dependency maintenance, not unfinished product features.

## Status rules

1. **Integrated on `dev`** means the commit is on GitHub `dev`. **Merged to `main`** means a promotion PR put that commit on GitHub `main`. Name the branch and source SHA. A local branch or open PR qualifies as neither.
2. **Published** means a successful `main` publish job names the source SHA and immutable image digest. A green PR check or temporary kind smoke test does not qualify.
3. **Deployed** means the target's deployment record identifies the source SHA or digest, migration and startup success, and a healthy target endpoint. Record dev, staging, and production separately. A local smoke test may prove the app runs without proving automated promotion. A skipped job or uninspected target is `unverified`.
4. **Milestone closed** means GitHub shows the milestone closed with zero open issues, the final report says `PASS`, and the evidence above is linked. Code merge and image publication alone do not close a milestone.

The existing `v0.1.0` tag and earlier Access `PASS` report predate the live review that reopened Access. Treat them as historical evidence, not current closure or deployment proof. Do not move or delete the tag to rewrite history; record a later verified release separately.

## Reconciliation routine

Refresh `main` and `dev` when it exists, run `scripts/check-tracking-status.sh`, inspect the current delivery workflow's individual jobs, and compare the target deployment record with its source SHA or published digest. Check each local Compose environment independently. Update this snapshot and the affected milestone report in the same PR whenever any status changes. If access is unavailable, state the exact unverified dimension rather than copying an older claim.
