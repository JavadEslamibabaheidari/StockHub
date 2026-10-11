# Reports module

Reports is a read and preference surface over the existing workspace Orders
and Inventory data. Keep cross-module reads behind the owning module
interfaces; Reports storage owns only the reorder-list selections and alert
preference rows added by migration `007_reports.sql`.

Daily reports use the most recent complete UTC calendar day. Weekly reports use
the previous complete Monday–Sunday ISO week in UTC. Display the inclusive
start, exclusive end, and workspace currency. Never combine values in
unlike currencies. Revenue excludes awaiting-payment, cancelled, and refunded
orders. A reservation still awaiting payment after ten minutes is a separate
lost-sale signal and is never revenue.

Top products rank by units in the report period. Bottom products rank by the
fewest units over the trailing 30 days. Restock suggestions use durable sales
over 14 days and available inventory (on-hand minus reserved) to estimate a
14-day target; supplier lead times and purchase-order data are not available.

Alert preferences persist per workspace. They do not send notifications until
a dispatcher and channel adapters exist; see Maintenance issue #132. The
Reports routes and user interaction contract are in
[`milestone-8-reports.md`](../plans/milestone-8-reports.md), with closure
verification in [`milestone-8-reports-closure.md`](../reports/milestone-8-reports-closure.md).
