# Project status and evidence

Last reconciled: 2026-10-02. Live [GitHub milestones](https://github.com/JavadEslamibabaheidari/StockHub/milestones), [issues](https://github.com/JavadEslamibabaheidari/StockHub/issues), and [Actions](https://github.com/JavadEslamibabaheidari/StockHub/actions) take precedence over this snapshot.

| Milestone | Code | Published image | Local dev | Local staging / production | Closure |
|---|---|---|---|---|---|
| [0 Cover](https://github.com/JavadEslamibabaheidari/StockHub/milestone/2) | Merged | Not applicable to the static Cover milestone | Not applicable | Not applicable | Closed; report `PASS` |
| [1 Access](https://github.com/JavadEslamibabaheidari/StockHub/milestone/1) | Merged | Main image pipeline exists; check the current SHA and run before claiming publication | Verified during closure evidence | See latest deployment record before claiming production freshness | Closed in GitHub; report records previous evidence |
| [2 Dashboard](https://github.com/JavadEslamibabaheidari/StockHub/milestone/3) | [PR #68](https://github.com/JavadEslamibabaheidari/StockHub/pull/68) merged; [PR #69](https://github.com/JavadEslamibabaheidari/StockHub/pull/69) recorded evidence | [Main run 36616827440](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/36616827440) published the image for `444c1da` | Local dashboard evidence recorded in `docs/reports/milestone-2-dashboard-closure.md` | See latest deployment record before claiming production freshness | Closed in GitHub with zero open issues |
| [3 Inventory](https://github.com/JavadEslamibabaheidari/StockHub/milestone/4) | [PR #79](https://github.com/JavadEslamibabaheidari/StockHub/pull/79) merged to `main` at `ca6b540` | [Main run 36866938760](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/36866938760) published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:805096315203e3d735c23eb8c0f3e727895ba675f070d64cdedd5a73c6ff4b55` | Browser smoke passed locally on `http://127.0.0.1:5173`; PR and main CI passed | Main run 36866938760 passed local Kubernetes migration and rollout smoke for merge commit `ca6b540` | Closed in GitHub with zero open issues; report `PASS` |
| [4 Orders](https://github.com/JavadEslamibabaheidari/StockHub/milestone/5) | [PR #88](https://github.com/JavadEslamibabaheidari/StockHub/pull/88) open from branch `codex/milestone-4-orders`; not merged to `dev` or `main` | Not published; PR delivery jobs skipped publish/deploy steps | Local frontend/backend checks and browser smoke passed; PR CI passed frontend, backend, Compose, tracking, Trivy, and local Kubernetes smoke | Not deployed | GitHub milestone is open with 7 open issues (#81-#87); PR #88 links them for closure on merge |

[Issue #48](https://github.com/JavadEslamibabaheidari/StockHub/issues/48) owns future shared hosted staging and production activation and has no product milestone. The first dev, staging, and production targets are separate localhost-only Compose projects on this PC. The 14 open pull requests observed on 2026-09-29 are Dependabot dependency updates. Count them as dependency maintenance, not unfinished product features.

## Status rules

1. **Integrated on `dev`** means the commit is on GitHub `dev`. **Merged to `main`** means a promotion PR put that commit on GitHub `main`. Name the branch and source SHA. A local branch or open PR qualifies as neither.
2. **Published** means a successful `main` publish job names the source SHA and immutable image digest. A green PR check or temporary kind smoke test does not qualify.
3. **Deployed** means the target's deployment record identifies the source SHA or digest, migration and startup success, and a healthy target endpoint. Record dev, staging, and production separately. A local smoke test may prove the app runs without proving automated promotion. A skipped job or uninspected target is `unverified`.
4. **Milestone closed** means GitHub shows the milestone closed with zero open issues, the final report says `PASS`, and the evidence above is linked. Code merge and image publication alone do not close a milestone.

The existing `v0.1.0` tag remains historical evidence. Do not move or delete
tags to rewrite history; record later verified releases separately.

## Reconciliation routine

Refresh `main` and `dev` when it exists, run `scripts/check-tracking-status.sh`, inspect the current delivery workflow's individual jobs, and compare the target deployment record with its source SHA or published digest. Check each local Compose environment independently. Update this snapshot and the affected milestone report in the same PR whenever any status changes. If access is unavailable, state the exact unverified dimension rather than copying an older claim.
