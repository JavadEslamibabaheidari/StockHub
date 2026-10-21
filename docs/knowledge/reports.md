# Reports module

Reports is a read and preference surface over the existing workspace Orders
and Inventory data. Keep cross-module reads behind the owning module
interfaces; Reports storage owns reorder selections and alert preferences,
rule state, delivery jobs, in-app notifications, and encrypted workspace Slack
integration data (`007_reports.sql` and `010_business_notifications.sql`).

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

The hosted alert worker evaluates persisted product and reservation data
through `IProductStore` and `IOrderStore`. Active workspace members receive
configured alerts. In-app delivery is stored per workspace and recipient;
instant alerts deduplicate for an active incident, while hourly and daily
settings use time buckets. Email is queued only when SMTP configuration is
complete and the recipient address is valid. Slack is queued only when a
workspace webhook is configured. Webhooks are restricted to Slack's HTTPS
incoming-hook host and protected with ASP.NET Data Protection; deployments
must persist the Data Protection key ring across restarts and replicas.

The notification outbox leases jobs before sending external requests, then
records delivery independently of the provider call. Failures retain a safe
exception type, retry after 30 seconds with exponential backoff capped at one
hour, and become dead after eight attempts. A successful external send followed
by a database outage can be repeated after the lease expires; external delivery
is therefore at-least-once and downstream consumers should tolerate duplicates.
In-app insertion and job completion share a transaction and the job id is
unique, so retries cannot create a second in-app notification. Delivery status
is visible in Alert settings. Sync-failure rules remain unavailable until the
Platforms module exposes persisted sync status; never infer a failed sync from
presentation state.

The Reports routes and user interaction contract are in
[`milestone-8-reports.md`](../plans/milestone-8-reports.md), with closure
verification in [`milestone-8-reports-closure.md`](../reports/milestone-8-reports-closure.md).
M14 alert delivery implementation and disposable PostgreSQL end-to-end evidence
are recorded in [`issue-132-alert-delivery.md`](../reports/issue-132-alert-delivery.md).
