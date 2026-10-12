# Issue #123 phone-width Pricing journey check

Status: PASS for the local 390×844 browser acceptance journey. This report is
evidence for the milestone issue; it does not certify hosted staging or
production.

## Environment

- Source branch: `codex/issue-123-responsive-journey`, based on the locally
  available #65 implementation commit `8208185d54df358181640504cf808c81774213ff`.
- App: isolated Docker Compose project `stockhub-issue123`, served locally at
  `http://127.0.0.1:8181` with a new PostgreSQL database and a separate Data
  Protection key volume. No existing local database was used.
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
