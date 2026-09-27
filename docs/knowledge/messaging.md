# StockHub Messaging Knowledge

Modules depend on `StockHub.Messaging.Abstractions`. Implementations are
separate infrastructure projects for in-process mediation, Kafka, and future
RabbitMQ transport.

Commands target one handler. Events may have multiple handlers. Integration
messages are durable outbox records and use at-least-once delivery, retries,
dead-letter handling, correlation IDs, and idempotent consumers.

The initial runtime uses asynchronous in-process dispatch. The composition
root owns registration through extensions such as
`AddProducerMessaging<TMessage>()` and
`AddConsumerMessaging<TMessage, THandler>()`.
