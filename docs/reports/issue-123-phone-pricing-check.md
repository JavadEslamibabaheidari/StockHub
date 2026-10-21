# Issue #123 phone-width Pricing journey check

Status: PASS for the local 390×844 browser acceptance journey and merged local
deployment. This report does not certify hosted staging or production.

## Environment

- Browser-tested worktree commit: `aa1fc74`, containing the tested CSS change on
  the #65 implementation tree. PR #174 source branch
  `codex/issue-123-responsive-main` at `bff2b87d5bd4f602a1e0dd2d6d0cea50bfcd2a59`
  was rebased onto current main and merged at `892fdfb9ddf78ed1287599ebd527ad2eb6373a14`.
- App: isolated Docker Compose project `stockhub-issue123`, served locally at
  `http://127.0.0.1:8181` with a new PostgreSQL database and a separate Data
  Protection key volume. No existing local database was used.
- Published image: main delivery run [38188227025](https://github.com/JavadEslamibabaheidari/StockHub/actions/runs/38188227025)
  published `ghcr.io/javadeslamibabaheidari/stockhub@sha256:e2217943fd1101bec373f86b8d3f306298ee907bbf599e22cd97f26cda9b48db`
  for merge SHA `892fdfb9ddf78ed1287599ebd527ad2eb6373a14`.
- Persistent local deployment: run [38188507403](https://github.com/JavadEslamibabaheidari/StockHub-Deployment/actions/runs/38188507403)
  records the same SHA and digest; migration completed, rollout reached 1/1
  available, and `http://127.0.0.1:8080/health` returned HTTP 200.
- Hosted shared staging and production were skipped and remain unverified.
- Browser viewport: 390×844 CSS pixels. At this browser's scrollbar settings,
  `innerWidth` was 390 and the document client width was 375.
- Account and workspace were created through the app's sign-up and workspace
  flows. All records were written through the real API to the disposable
  PostgreSQL database.

## Journey and evidence

1. Created the disposable account `issue123-20261012@example.test`, workspace
   `Issue 123 Main Test`, and a manual product (`ISSUE123-TEST-01`, base price
   €129.50) through the app. Inventory confirmed one saved product.
2. Opened Pricing rules at 390×844. Its main navigation had a 351px visible
   width and 821px scroll width, so it remained horizontally scrollable. The
   document and body scroll widths were both 375px, equal to the document
   client width; there was no document-level horizontal overflow.
3. Previewed a new eBay +5% rule against the persisted product. The API returned
   one affected product and an estimated final price of €135.98. Saved the
   rule, reopened it, changed the adjustment to +7%, previewed €138.57, and
   saved the edit. The refreshed rule card showed +7%.
4. Created `Issue 123 Secondary Test`, switched to it, and confirmed its Pricing
   list was empty. Switched back and confirmed the original +7% rule remained.
5. Signed out from Pricing. The app returned to Sign in; reopening protected
   Pricing redirected to Sign in. No browser console warnings or errors were
   captured.

## Defect fixed

The first 390px run found document overflow to 821px. The mobile Pricing
navigation's intrinsic width forced the grid item wider than the viewport, and
the search and account actions competed on one header row. The phone breakpoint
now constrains the sidebar grid item and wraps the search above the action row.
The rebuilt app then measured 375px document/body scroll width at a 375px
client width, with the 821px nav available through its own horizontal scroller.

## Reproduction

Run the app with an isolated Compose project and an 8181 host port, create a
disposable account/workspace, then set the browser viewport to 390×844. Add a
manual product, open Pricing, create and preview a rule, save it, edit and save
it again, create and switch to a second workspace, switch back, sign out, and
open Pricing again. At every stage check:

```js
document.documentElement.scrollWidth <= document.documentElement.clientWidth
```

Also verify the rule card and preview values shown above. Remove the isolated
Compose project and its volumes after the run; its records are test-only.
