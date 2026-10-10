# Project status and evidence

Last reconciled: 2026-10-11. Live [GitHub milestones](https://github.com/JavadEslamibabaheidari/StockHub/milestones), [issues](https://github.com/JavadEslamibabaheidari/StockHub/issues), and [Actions](https://github.com/JavadEslamibabaheidari/StockHub/actions) take precedence over this snapshot.

| Milestone | Code | Published image | Local dev | Local staging / production | Closure |
|---|---|---|---|---|---|
| [0 Cover](https://github.com/JavadEslamibabaheidari/StockHub/milestone/2) | Merged | Not applicable to the static Cover milestone | Not applicable | Not applicable | Closed; report `PASS` |
| [1 Access](https://github.com/JavadEslamibabaheidari/StockHub/milestone/1) | Merged | Main image pipeline exists; check the current SHA and run before claiming publication | Verified during closure evidence | See latest deployment record before claiming production freshness | Closed in GitHub; report records previous evidence |
| [2 Dashboard](https://github.com/JavadEslamibabaheidari/StockHub/milestone/3) | [PR #68](https://github.com/JavadEslamibabaheidari/StockHub/pull/68) merged; [PR #69](https://github.com/JavadEslamibabaheidari/StockHub/pull/69) recorded evidence | [Main run 36616827440](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/36616827440) published the image for `444c1da` | Local dashboard evidence recorded in `docs/reports/milestone-2-dashboard-closure.md` | See latest deployment record before claiming production freshness | Closed in GitHub with zero open issues |
| [3 Inventory](https://github.com/JavadEslamibabaheidari/StockHub/milestone/4) | [PR #79](https://github.com/JavadEslamibabaheidari/StockHub/pull/79) merged to `main` at `ca6b540` | [Main run 36866938760](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/36866938760) published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:805096315203e3d735c23eb8c0f3e727895ba675f070d64cdedd5a73c6ff4b55` | Browser smoke passed locally on `http://127.0.0.1:5173`; PR and main CI passed | Main run 36866938760 passed local Kubernetes migration and rollout smoke for merge commit `ca6b540` | Closed in GitHub with zero open issues; report `PASS` |
| [4 Orders](https://github.com/JavadEslamibabaheidari/StockHub/milestone/5) | [PR #88](https://github.com/JavadEslamibabaheidari/StockHub/pull/88) merged to `main` at `59e412a` | [Main run 37012789308](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/37012789308) published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:716bca43ac590947edaa348786d050f7889554b0cf955b9f18a8f18eb9b30a1e` | Browser smoke passed locally on `http://127.0.0.1:5173`; PR and main CI passed | Main run 37012789308 passed local Kubernetes migration and rollout smoke for merge commit `59e412a`; shared review-staging and production promotion jobs were skipped | Closed in GitHub with zero open issues; report `PASS` |
| [5 Reservations](https://github.com/JavadEslamibabaheidari/StockHub/milestone/6) | [PR #108](https://github.com/JavadEslamibabaheidari/StockHub/pull/108) merged to `main` at `1b2c90c` | [Main run 38064434715](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/38064434715) published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:8d2bdf8cec096ce58431654adbb72e6e99e7998953f4f419ac0ab0e51c5e23df` | Local browser smoke passed on `http://127.0.0.1:5173/#reservations`; PR and main CI passed | Main run 38064434715 passed local Kubernetes migration and rollout smoke for merge commit `1b2c90c`; persistent local deployment dispatch job passed; shared review-staging and production promotion jobs were skipped | Closed in GitHub with zero open issues; report `PASS` |
| [6 Platforms](https://github.com/JavadEslamibabaheidari/StockHub/milestone/7) | [PR #114](https://github.com/JavadEslamibabaheidari/StockHub/pull/114) merged to `main` at `0e6f6e9` | [Main run 38090844733](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/38090844733) published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:ed1e38a9b225cf9c23a5f3bd676dd61b82ae07177c4d12b77a34a6a597e063b8` | Local browser smoke passed on `http://127.0.0.1:5173/#platforms`; PR and main CI passed | Main run 38090844733 passed local Kubernetes migration and rollout smoke for merge commit `0e6f6e9`; persistent local deployment dispatch job passed; shared review-staging and production promotion jobs were skipped | Closed in GitHub with zero open issues; report `PASS` |

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

## Milestone 7 start

Pricing rules is open in [milestone 8](https://github.com/JavadEslamibabaheidari/StockHub/milestone/8),
with issues #116–#119 and [the execution plan](plans/milestone-7-pricing.md).
Refreshed `main` is `9b79701ada8b959529deb05aaad57f049c8934c0`; `dev` is
`21549b36342cb031dc94bc79b28ee502be5dbf9c`. No open PRs remained on inspection.
[Delivery run 38091518130](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/38091518130)
passed image publication, local Kubernetes smoke and persistent local deployment
dispatch. Shared staging and production jobs were skipped; those targets are
unverified. Dispatch success alone does not prove a persistent target rollout.
