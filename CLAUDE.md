# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9 Clean Architecture demonstration project showcasing a WebAPI with background processing and **dual real-time notification system**. The project implements:

- **WebAPI**: RESTful API for CRUD operations on students + real-time endpoints
- **Worker Service**: Background worker that processes domain events and manages notifications
- **Real-Time Notifications**: Both Server-Sent Events (SSE) and SignalR implementations
- **Message Queuing**: RabbitMQ for event-driven communication between services
- **Caching**: Redis with FusionCache for distributed caching + notification backplane
- **Database**: SQL Server with Entity Framework Core
- **Logging**: Serilog with Seq server for structured logging

## Architecture

The solution follows Clean Architecture principles with these layers:

- **Domain**: Core entities, value objects, domain events (`TheBackgroundExperience.Domain`)
- **Application**: Business logic, commands/queries with Mediator pattern (`TheBackgroundExperience.Application`)
- **Infrastructure**: Data persistence, external services, caching (`TheBackgroundExperience.Infrastructure`)
- **WebApi**: REST API controllers and configuration (`TheBackgroundExperience.WebApi`)
- **Worker**: Background processing service (`TheBackgroundExperience.Worker`)

## Common Development Commands

### Building and Running
```bash
# Build entire solution
dotnet build

# Run WebAPI (requires Docker services)
cd src/TheBackgroundExperience.WebApi
dotnet run

# Run Worker (requires Docker services)
cd src/TheBackgroundExperience.Worker
dotnet run

# Start all services with Docker
docker compose up -d
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project src/TheBackgroundExperience.Infrastructure --startup-project src/TheBackgroundExperience.WebApi

# Update database
dotnet ef database update --project src/TheBackgroundExperience.Infrastructure --startup-project src/TheBackgroundExperience.WebApi

# Generate optimized DbContext
dotnet ef dbcontext optimize --project src/TheBackgroundExperience.Infrastructure --startup-project src/TheBackgroundExperience.WebApi
```

### Code Analysis
The project enforces strict code analysis with:
- `TreatWarningsAsErrors=true`
- `EnforceCodeStyleInBuild=true`
- `AnalysisLevel=latest`
- Nullable reference types enabled

## Key Patterns and Conventions

### Domain Events
- Events are defined in `Domain/Events/` and inherit from `BaseEvent`
- Domain entities can raise events via `AddDomainEvent()`
- Events are automatically dispatched after `SaveChangesAsync()` via `DispatchEventsInterceptor`
- Event handlers are in `Application/Students/EventHandlers/`

### CQRS with Mediator
- Commands and Queries are separated in `Application/Students/`
- All requests implement `IRequest<T>` from Mediator library
- Handlers implement `IRequestHandler<TRequest, TResponse>`

### Caching Strategy
- Entities implementing `ICachable` get automatic caching via `CachingBehaviour`
- Cache keys are centralized in `Domain/Common/Caching/`
- Uses FusionCache with Redis backplane for distributed scenarios

### Queue Processing
- RabbitMQ queues are managed via `IQueueManager` interface
- Workers extend `QueueWorkerBase` for consistent error handling
- JSON serialization used for message payloads

## Project Dependencies

### Core Frameworks
- .NET 9.0 with nullable reference types
- Entity Framework Core 9.0.7 with SQL Server
- Mediator 3.0.0-preview.65 for CQRS pattern

### Infrastructure
- RabbitMQ.Client 7.1.2 for message queuing
- FusionCache 2.3.0 with Redis backplane for distributed caching
- Serilog 9.0.0 with Seq sink for structured logging

### Development
- Central package management enabled (`Directory.Packages.props`)
- Strict analyzer rules with extended analysis enabled

## Real-Time Notification Architecture (Microservices)

### **Dedicated Notifications Service**
The project follows **microservices best practices** with a dedicated service for real-time communications:

**Architecture:** `Business API → Worker → RabbitMQ → Notifications Service → Clients`

### **Service Responsibilities:**
1. **Business WebAPI (Port 5000)**: Core student CRUD operations, domain events
2. **Background Worker**: Processes domain events, publishes notifications to RabbitMQ
3. **Notifications Service (Port 5001)**: Dedicated real-time communication service
   - Consumes notifications from RabbitMQ
   - Broadcasts via both SSE and SignalR simultaneously
   - Independent scaling based on connection load

### **Message Flow:**
```
Student Operation → Domain Event → Event Handler → RabbitMQ Topic
↓
Notifications Service Consumer → Broadcast to:
├── SSE clients (/api/notifications/students/stream)
└── SignalR clients (/hubs/notifications)
```

### **Notification Topics (RabbitMQ)**
- Exchange: `notifications` (topic)  
- Routing Keys: `student.created`, `student.updated`, `student.cached`, `student.deleted`
- Queue: `notifications-queue` (consumed by Notifications Service)
- Routing Pattern: `student.*` (binds to all student events)

### **Notification Payload Structure**
```json
{
  "eventType": "StudentCreated",
  "routingKey": "student.created", 
  "student": {
    "id": "guid",
    "firstName": "string",
    "lastName": "string", 
    "fullName": "string",
    "updates": "number",
    "created": "dateTimeOffset",
    "createdBy": "string?",
    "lastModified": "dateTimeOffset?",
    "lastModifiedBy": "string?"
  },
  "timestamp": "dateTime",
  "userId": "string?"
}

### **Testing Real-Time Notifications**
```bash
# 1. Start all services
docker compose up -d

# 2. Access services
Business API: http://localhost:5000/api/students
Notifications: http://localhost:5001/

# 3. Open test pages (served by Notifications Service)
http://localhost:5001/sse-test.html      # SSE test page  
http://localhost:5001/signalr-test.html  # SignalR test page

# 4. Create/update students via Business API and watch real-time notifications
```

## Service URLs (Docker)
- **Business WebAPI**: http://localhost:5000 (REST API only)
- **Notifications Service**: http://localhost:5001 (Real-time communications)
- **Notifications Landing**: http://localhost:5001/index.html
- **SSE Test**: http://localhost:5001/sse-test.html
- **SignalR Test**: http://localhost:5001/signalr-test.html
- **RabbitMQ Management**: http://localhost:15672 (sa / SuperSecureWith100%Chance)
- **Seq Logging**: http://localhost:5341 (admin / SuperSecureWith100%Chance)
- **Redis**: localhost:6379
- **SQL Server**: localhost:1433 (sa / SuperSecureWith100%Chance)

## Development Notes

- Database migrations are not automatically applied - delete database to reset schema
- Sensitive configuration is hardcoded for development convenience (not production ready)
- Both WebAPI and Worker initialize the database on startup
- The `students.http` file in WebApi project contains example HTTP requests
- **Worker project** handles all background processing (separated from WebAPI)
- **SignalR client in Worker** connects to WebAPI hub for notification distribution
- **Redis backplane** enables SignalR scaling across multiple WebAPI instances