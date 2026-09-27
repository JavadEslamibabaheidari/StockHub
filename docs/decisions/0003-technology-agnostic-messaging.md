# ADR 0003: Technology-agnostic messaging

Status: Accepted in PR #14 (remote verification pending in this workspace)

## Decision

Modules depend on messaging abstractions, not Kafka, RabbitMQ, or mediator
packages. Infrastructure provides separate implementations for in-process
mediation, Kafka, and future RabbitMQ transport.

The initial runtime uses in-process mediation. Integration messages use a
transactional outbox and are delivered with at-least-once semantics. Consumers
must be idempotent.

The composition root registers producers and consumers through generic
extensions such as `AddProducerMessaging<TMessage>()` and
`AddConsumerMessaging<TMessage, THandler>()`.
