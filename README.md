# The Background Experience

A .NET 9 **Clean Architecture** demonstration project showcasing a modern microservices architecture with **dual real-time notification system**. This solution demonstrates event-driven communication patterns between microservices with comprehensive real-time client notifications.

## 🎯 Main Purpose

This project demonstrates:
- **Event-Driven Architecture**: Domain events published via RabbitMQ for loose coupling between services
- **Real-Time Communications**: Dual notification system using both Server-Sent Events (SSE) and SignalR
- **Microservices Pattern**: Dedicated services for business logic, background processing, and real-time communications
- **Clean Architecture**: Separation of concerns with Domain, Application, Infrastructure, and Presentation layers
- **Modern .NET Patterns**: CQRS with Mediator, distributed caching, and structured logging

## 🏗️ Architecture Overview

The solution consists of **three main services**:

1. **Business WebAPI** (`TheBackgroundExperience.WebApi`): Core CRUD operations for students
2. **Background Worker** (`TheBackgroundExperience.Worker`): Processes domain events and manages background tasks
3. **Notifications Service** (`TheBackgroundExperience.NotificationsApi`): Dedicated real-time communication service

**Supporting Infrastructure:**
- **Database**: SQL Server with Entity Framework Core for data persistence
- **Message Broker**: RabbitMQ for event-driven communication
- **Caching**: Redis with FusionCache for distributed caching and SignalR backplane
- **Logging**: Serilog with Seq server for structured logging

This is a learning project to explore modern .NET application patterns and technologies. While not production-ready, it serves as a comprehensive starting point for understanding microservices architecture and real-time communication patterns.

## 🔄 System Architecture

```mermaid
flowchart LR
    %% Client Layer
    CLIENT[🖥️ Web Clients<br/>SSE & SignalR]
    
    %% Application Services Layer
    subgraph APP [" Application Services "]
        direction TB
        API[🌐 Business API<br/>:5000<br/>Student CRUD]
        WORKER[⚙️ Background Worker<br/>Event Processing]
        NOTIFY[🔔 Notifications API<br/>:5001<br/>Real-time Comms]
    end
    
    %% Infrastructure Layer
    subgraph INFRA [" Infrastructure Services "]
        direction TB
        DB[(🗄️ SQL Server<br/>:1433)]
        MQ[📨 RabbitMQ<br/>:5672/:15672]
        CACHE[💾 Redis<br/>:6379]
        LOG[📋 Seq<br/>:5341]
    end

    %% Main flow connections
    CLIENT <-->|Real-time| NOTIFY
    API -->|Events| MQ
    MQ -->|Process| WORKER
    WORKER -->|Notifications| MQ
    MQ -->|Broadcast| NOTIFY
    
    %% Data connections
    API <--> DB
    WORKER <--> DB
    API <--> CACHE
    WORKER <--> CACHE
    NOTIFY <--> CACHE
    
    %% Logging connections
    API -.->|Logs| LOG
    WORKER -.->|Logs| LOG
    NOTIFY -.->|Logs| LOG

    %% Styling
    classDef appService fill:#e3f2fd,stroke:#1976d2,stroke-width:2px,color:#000
    classDef infraService fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px,color:#000
    classDef client fill:#e8f5e8,stroke:#388e3c,stroke-width:2px,color:#000
    
    class API,WORKER,NOTIFY appService
    class DB,MQ,CACHE,LOG infraService
    class CLIENT client
```

## 🚀 Event-Driven Workflow

This diagram shows the complete flow from a student operation to real-time client notifications:

```mermaid
flowchart TD
    %% Step 1: Client Request
    START([🖥️ Client Request<br/>POST/PUT Student]) 
    
    %% Step 2: Business API Processing
    API[🌐 Business API<br/>Process CRUD]
    DB_SAVE[💾 Save to Database]
    DOMAIN_EVENT[⚡ Raise Domain Event<br/>StudentCreated/Updated]
    
    %% Step 3: Event Publishing
    PUB_EVENT[📤 Publish Event<br/>RabbitMQ]
    RESPONSE[📨 HTTP Response<br/>200/201 to Client]
    
    %% Step 4: Background Processing  
    CONSUME_EVENT[📥 Worker Consumes<br/>Domain Event]
    BG_PROCESS[⚙️ Background Processing<br/>Cache Updates, Validation]
    
    %% Step 5: Notification Publishing
    PUB_NOTIFY[📤 Publish Notification<br/>student.created/updated/cached/deleted]
    
    %% Step 6: Real-time Distribution
    CONSUME_NOTIFY[📥 Notifications API<br/>Consumes Event]
    
    subgraph REALTIME [" Real-time Broadcast "]
        direction LR
        SSE[📡 Server-Sent Events<br/>/api/notifications/stream]
        SIGNALR[⚡ SignalR Hub<br/>/hubs/notifications]
    end
    
    CLIENTS[🖥️ Connected Clients<br/>Receive Updates]

    %% Flow connections
    START --> API
    API --> DB_SAVE
    API --> DOMAIN_EVENT
    DOMAIN_EVENT --> PUB_EVENT
    PUB_EVENT --> RESPONSE
    PUB_EVENT --> CONSUME_EVENT
    CONSUME_EVENT --> BG_PROCESS
    BG_PROCESS --> PUB_NOTIFY
    PUB_NOTIFY --> CONSUME_NOTIFY
    CONSUME_NOTIFY --> SSE
    CONSUME_NOTIFY --> SIGNALR
    SSE --> CLIENTS
    SIGNALR --> CLIENTS

    %% Styling
    classDef startEnd fill:#4caf50,stroke:#2e7d32,stroke-width:2px,color:#fff
    classDef process fill:#2196f3,stroke:#1565c0,stroke-width:2px,color:#fff
    classDef event fill:#ff9800,stroke:#ef6c00,stroke-width:2px,color:#fff
    classDef realtime fill:#9c27b0,stroke:#6a1b9a,stroke-width:2px,color:#fff
    
    class START,CLIENTS startEnd
    class API,DB_SAVE,BG_PROCESS,RESPONSE process
    class DOMAIN_EVENT,PUB_EVENT,CONSUME_EVENT,PUB_NOTIFY,CONSUME_NOTIFY event
    class SSE,SIGNALR realtime
```

## Getting Started

This project is just a starting point for a more complex application.
To keep it simple and directly runnable, I intended to check in configuration which contains otherwise sensitive information.
This is not recommended for production applications, but it is useful for development and testing purposes.

_The password is the same for all services running in docker and can be found in the `appsettings.json` or `docker-compose.yml` files_

**Please never check in sensitive information into a public repository!**

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started)

### Running the Application

```bash
docker compose up -d
```

This will start a new network with the following services:
  - `db`: The SQL Server database. (localhost, 1433)
  - `rabbitmq`: The RabbitMQ message broker. (http://localhost:15672/)
  - `redis`: The Redis cache. (localhost, 6379)
  - `seq`: The Seq logging server. (http://localhost:5341/)
  - `webapi`: The Business WebAPI application. (http://localhost:5000/)
  - `worker`: The background worker service that processes domain events.
  - `notifications`: The Notifications API service with real-time communications. (http://localhost:5001/) 

The first start of the applications will also create the database if it does not yet exist.
But no migrations will be applied (for now), so make sure to delete the database if you want to change something in the entities / schema, and restart either the `worker` or the `webapi`.

To use `Seq` you can access it at http://localhost:5341, use the predefined credentials and create an API key.
Update the appsettings.{env}.json files in the `WebApi` and `Worker` projects with the API key inside the `Serilog:WriteTo` => `Seq` configuration.

The development environment uses `localhost` to access the services running in the Docker container (`appsettings.Development.json`).
In production, you should use the service names defined in the `docker-compose.yml` file (see `appsettings.json`).

### 🔔 Testing Real-Time Notifications

After starting all services, you can test the real-time notification system:

1. **Access Test Pages** (served by NotificationsApi):
   - **Landing Page**: http://localhost:5001/
   - **SSE Test**: http://localhost:5001/sse-test.html
   - **SignalR Test**: http://localhost:5001/signalr-test.html

2. **Perform Student Operations** via Business API:
   ```bash
   # Create a student (triggers real-time notifications)
   curl -X POST http://localhost:5000/api/students \
     -H "Content-Type: application/json" \
     -d '{"firstName":"John","lastName":"Doe","email":"john.doe@example.com"}'
   
   # Update a student (triggers real-time notifications)
   curl -X PUT http://localhost:5000/api/students/1 \
     -H "Content-Type: application/json" \
     -d '{"firstName":"Jane","lastName":"Doe","email":"jane.doe@example.com"}'
   ```

3. **Watch Real-Time Updates**: Open the test pages in your browser and perform operations via the API to see live notifications.

## 🛠️ Project Structure

```
src/
├── TheBackgroundExperience.Domain/          # Core domain entities and events
├── TheBackgroundExperience.Application/     # Business logic with CQRS/Mediator
├── TheBackgroundExperience.Infrastructure/  # Data access and external services
├── TheBackgroundExperience.WebApi/          # Business REST API (Port 5000)
├── TheBackgroundExperience.Worker/          # Background processing service
└── TheBackgroundExperience.NotificationsApi/ # Real-time communications (Port 5001)
```

## 🔧 Development Commands

```bash
# Build entire solution
dotnet build

# Add Entity Framework migration
dotnet ef migrations add <MigrationName> --project src/TheBackgroundExperience.Infrastructure --startup-project src/TheBackgroundExperience.WebApi

# Update database
dotnet ef database update --project src/TheBackgroundExperience.Infrastructure --startup-project src/TheBackgroundExperience.WebApi

# Run services individually (requires Docker infrastructure)
cd src/TheBackgroundExperience.WebApi && dotnet run       # Business API
cd src/TheBackgroundExperience.Worker && dotnet run       # Background Worker  
cd src/TheBackgroundExperience.NotificationsApi && dotnet run  # Notifications Service
```

## 🚀 Future Enhancements

### Core Features
- **Authentication & Authorization**: JWT tokens, role-based access control
- **Validation**: FluentValidation with comprehensive business rules  
- **Error Handling**: Global exception handling with detailed error responses
- **API Versioning**: Versioned endpoints for backward compatibility

### Infrastructure
- **Database Migrations**: Automated migration deployment
- **Database Seeding**: Sample data and initial setup
- **Health Checks**: Service health monitoring endpoints
- **Docker Optimization**: Multi-stage builds, health checks

### Testing & Quality
- **Unit Tests**: Comprehensive test coverage for all layers
- **Integration Tests**: API and database integration testing
- **End-to-End Tests**: Full workflow testing with real services
- **Performance Testing**: Load testing for real-time notifications

### Observability
- **Distributed Tracing**: OpenTelemetry integration
- **Metrics Collection**: Prometheus integration
- **Application Insights**: Advanced monitoring and alerting

### Client Applications
- **Web Frontend**: React/Angular SPA consuming the APIs
- **Mobile Apps**: Real-time notifications on mobile platforms
- **Admin Dashboard**: Service management and monitoring interface
