# Tech Notes
## Queue Decision Matrix
### Queue Chosen

RabbitMQ (via MassTransit)

Why RabbitMQ?
RabbitMQ was chosen because it provides a mature, production-ready message broker with strong support for event-driven architectures.

Reasons for selecting RabbitMQ:

- Runs easily in Docker for local development
- No cloud credentials required
- Supports durable queues, acknowledgements, retries, and DLQ patterns
- Closely resembles real production message brokers

This approach allows the system to demonstrate real event-driven behaviour without requiring external cloud services.

Why not Channel<T> ?
- No cross-service communication. The assessment required demonstrating microservice communication, which requires an external message broker.

Why not LocalStack SQS ?
- Additional container and configuration complexity.

## Resilience Strategy
External HTTP calls use .NET HTTP Resilience with Polly-based policies.

## Idempotency Strategy
Queue systems can deliver messages more than once.
To avoid duplicate processing, the orchestrator stores processed keys.

## Dead Letter Strategy
If inventory reservation fails after the HTTP resilience policies complete, the message is stored in a dead letter store.
For the purpose of this assessment the dead letter store is in-memory.

