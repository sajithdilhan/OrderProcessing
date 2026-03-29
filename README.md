# Order Processing System – Event-Driven Microservices (.NET)

## Objective

Build a simplified event-driven microservices system using .NET that demonstrates:

clean API design
asynchronous communication between services
resilience patterns
testable architecture

The system simulates a simplified Order Processing workflow using multiple services communicating through RabbitMQ events.

## Architecture Overview

The solution consists of the following services:

- OMS API: Stores orders and exposes pending orders
- Order Orchestrator API: 	Coordinates order processing flows
- Inventory Service API:	Allocates and reserves inventory
- Payment Gateway API: Publishes payment confirmation events
- RabbitMQ:	Event broker

## Design Decisions
### Why Event-Driven?

Event-driven communication allows services to remain loosely coupled while enabling asynchronous workflows.

RabbitMQ is used to simulate real-world message-driven systems.

### Why Orchestrator Pattern?

The orchestrator centralizes business workflow coordination while keeping domain services focused on single responsibilities.

### Why In-Memory Stores?

For simplicity of the take-home assessment:
- processed message store
- dead letter store

are implemented in-memory.

In a production system these would typically use:
- Redis
- database
- durable message queues

## AI Assistance Disclosure

AI tools were used to assist development in the following ways:
- code scaffolding
- architectural brainstorming
- test generation
- documentation drafting

All generated outputs were reviewed, validated, and adapted to ensure correctness and alignment with the requirements of the assessment.

## Implemented Flows
### Flow 1 – Pending Order Synchronisation

OMS notifies the Orchestrator that pending orders should be processed.
Steps:

1. OMS triggers the orchestrator.
2. Orchestrator calls: `GET /orders/pending`
3. For each pending order: `POST /inventory/allocate`
Inventory allocation is executed asynchronously.

### Flow 2 – Payment Confirmation Event

Payment Gateway publishes an event when payment succeeds.
Steps:

1. Payment Gateway publishes: `PaymentConfirmedEvent`
2. Orchestrator consumes the event.
3. Orchestrator reserves inventory.
4. Idempotency ensures orders are processed only once.

## Technology Stack

- .NET 10	
- ASP.NET Core
- MassTransit	Messaging 
- RabbitMQ
- Polly / Http Resilience	
- Docker Compose	
- xUnit

## Reliability Features

The system includes several reliability mechanisms.

### HTTP Resilience

Typed HttpClient implementations include:
- retry
- timeout
- circuit breaker

## Dead Letter Handling

If processing ultimately fails, the event is stored in a dead letter store.
This simulates production behaviour where failed events are inspected or retried later.

## Health Checks

The orchestrator exposes: `/health`

Checks include:
- Inventory service connectivity
- RabbitMQ connectivity

## Running the System
Prerequisites:
- Docker
- .NET 10
- Git

### Steps to Run the Application

#### Clone the Repository
- ```git clone https://github.com/sajithdilhan/OrderProcessing.git```
- ```cd OrderProcessing```

#### Run the System with Docker
Start all services and infrastructure using Docker Compose.

```docker-compose up --build -d```

#### Open API endpoints
- Oms.Api: ```http://localhost:5001/scalar/```
- Order.Orchestrator.Api: ```http://localhost:5002/scalar/```
- Inventory.Service.Api: ```http://localhost:5003/scalar/```
- Payment.Gateway.Api: ```http://localhost:5004/scalar/```


