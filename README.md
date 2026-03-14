# Box Tracking System - Prototype

Real-time event-driven system for tracking box operations (cleaning, repairing, loading) by warehouse workers.

## 🏗️ Architecture

```
Event Simulator (Blazor) / Mobile App (Future)
    ↓ HTTP POST
API (ASP.NET Core)
    ↓ Publish
RabbitMQ (Message Queue)
    ↓ Consume
Event Processor
    ↓ Update Metrics
Dashboard (Blazor Server)
```

## 🚀 Quick Start

### Option 1: Local Development with Docker Compose

**Prerequisites:**
- Docker & Docker Compose
- (Optional) .NET 8 SDK for local development

**Run with Docker Compose:**

```bash
# Start all services
docker-compose up --build

# Or run in background
docker-compose up -d --build
```

### Access the Services

- **Event Simulator**: http://localhost:5002 (Generate test events with UI)
- **Dashboard**: http://localhost:5001 (View metrics)
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### Option 2: Deploy to Azure ☁️

**Deploy to Azure Container Apps with one command:**

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars and set a unique acr_name
./deploy.sh
```

**What you get:**
- ✅ Production-ready infrastructure on Azure
- ✅ Automatic HTTPS with custom domains
- ✅ Auto-scaling based on load
- ✅ Centralized logging and monitoring
- ✅ ~€20-40/month (free tier available)

**Full documentation:** [terraform/README.md](terraform/README.md)

**Cleanup:**
```bash
cd terraform
./destroy.sh
```

## 📡 API Usage

### Health Check
```bash
curl http://localhost:5000/health
```

### Publish Events

**Box Cleaning Started:**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "boxId": "BOX-001",
    "workerId": "WORKER-01",
    "eventType": "BoxCleaningStarted"
  }'
```

**Box Cleaning Completed:**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "boxId": "BOX-001",
    "workerId": "WORKER-01",
    "eventType": "BoxCleaningCompleted"
  }'
```

**Box Loading Attempted (Success):**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "boxId": "BOX-001",
    "workerId": "WORKER-02",
    "eventType": "BoxLoadingAttempted"
  }'
```

**Box Loading Attempted (Failed - Uncleaned Box):**
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "boxId": "BOX-999",
    "workerId": "WORKER-02",
    "eventType": "BoxLoadingAttempted"
  }'
```

## 🧪 Testing the Flow

### Option 1: Using the Event Simulator (Easiest)

1. **Start all services:**
   ```bash
   docker-compose up --build
   ```

2. **Open Event Simulator:**
   - Go to http://localhost:5002
   - Click "Simulate Lifecycle" to send a complete workflow
   - Or send individual events manually

3. **Open Dashboard:**
   - Go to http://localhost:5001
   - Watch the metrics update in real-time

### Option 2: Using cURL

1. **Start all services:**
   ```bash
   docker-compose up --build
   ```

2. **Send test events:**
   ```bash
   # Clean a box
   curl -X POST http://localhost:5000/api/events \
     -H "Content-Type: application/json" \
     -d '{"boxId": "BOX-001", "workerId": "WORKER-01", "eventType": "BoxCleaningCompleted"}'

   # Try to load it
   curl -X POST http://localhost:5000/api/events \
     -H "Content-Type: application/json" \
     -d '{"boxId": "BOX-001", "workerId": "WORKER-02", "eventType": "BoxLoadingAttempted"}'
   ```

3. **Check RabbitMQ:**
   - Open http://localhost:15672
   - Login: guest/guest
   - Go to "Queues" tab
   - See "box-events" queue with messages

## 📦 Project Structure

```
box-tracking-prototype/
├── docker-compose.yml                 # Orchestrates all services
├── src/
│   ├── BoxTracking.Shared/           # Shared models & events
│   │   ├── Events/BoxEvent.cs
│   │   └── Models/DailyMetrics.cs
│   ├── BoxTracking.Api/              # REST API (receives events)
│   │   ├── Program.cs
│   │   └── Dockerfile
│   ├── BoxTracking.EventProcessor/   # Consumes from RabbitMQ
│   │   ├── Program.cs
│   │   └── Dockerfile
│   ├── BoxTracking.Dashboard/        # Blazor Server UI (metrics)
│   │   ├── Components/Pages/Home.razor
│   │   └── Dockerfile
│   └── BoxTracking.EventSimulator/   # Blazor Server UI (test events)
│       ├── Components/Pages/Home.razor
│       ├── Services/EventService.cs
│       └── Dockerfile
├── tests/
│   └── BoxTracking.IntegrationTests/ # Integration tests with Testcontainers
└── README.md
```

## 🎯 Event Types

| Event | Description |
|-------|-------------|
| `BoxCleaningStarted` | Worker starts cleaning a box |
| `BoxCleaningCompleted` | Box cleaning finished |
| `BoxRepairStarted` | Worker starts repairing a box |
| `BoxRepairCompleted` | Box repair finished |
| `BoxLoadingAttempted` | Worker tries to load box onto truck |

## 🔧 Development

### Run Locally (without Docker)

**Terminal 1 - RabbitMQ:**
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

**Terminal 2 - API:**
```bash
cd src/BoxTracking.Api
dotnet run
```

**Terminal 3 - Event Processor:**
```bash
cd src/BoxTracking.EventProcessor
dotnet run
```

**Terminal 4 - Dashboard:**
```bash
cd src/BoxTracking.Dashboard
dotnet run
```

## 📊 Current State (Prototype v0.1)

**✅ Working:**
- REST API for publishing events
- RabbitMQ message queue
- Event processor consuming messages
- Basic dashboard with metrics
- Docker Compose orchestration

**🚧 Not Yet Implemented:**
- SignalR real-time updates (dashboard polls every 5s for now)
- Mobile app (MAUI or PWA)
- Offline queue in mobile
- Business rule validation (can't load uncleaned box)
- Persistent database (currently in-memory)
- Authentication

**🎯 Next Steps:**
1. Add SignalR broadcasting from EventProcessor to Dashboard
2. Implement business rules (box state validation)
3. Add SQLite persistence
4. Create simple mobile app (Blazor PWA or MAUI)
5. Add offline queue support

## 🤝 Contributing

This is a prototype for learning event-driven architecture patterns.

## 📄 License

MIT License - feel free to use for learning and building!

## 🙏 Credits

Built following patterns from:
- James Eastham's EDA resources
- Open-source warehouse management systems
- Microsoft .NET documentation

---

**Built with:** .NET 8, RabbitMQ, Blazor Server, Docker
