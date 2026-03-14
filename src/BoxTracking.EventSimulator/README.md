# Box Tracking Event Simulator

A Blazor Server web application for simulating warehouse box events and sending them to the Box Tracking API.

## Features

- **Single Event Sending**: Send individual events with custom Box ID, Worker ID, and Event Type
- **Box Lifecycle Simulation**: Simulate a complete box workflow (Cleaning Started → Cleaning Completed → Loading Attempted)
- **Random Event Generation**: Generate and send batches of random events for load testing
- **Real-time Event Log**: Track all sent events with timestamps

## Event Types Supported

- `BoxCleaningStarted` - Box enters cleaning station
- `BoxCleaningCompleted` - Box cleaning is finished
- `BoxRepairStarted` - Box enters repair station
- `BoxRepairCompleted` - Box repair is finished
- `BoxLoadingAttempted` - Worker attempts to load box onto truck

## Running the Simulator

### With Docker Compose (Recommended)

```bash
docker-compose up simulator
```

Access at: http://localhost:5002

### Standalone

```bash
cd src/BoxTracking.EventSimulator
dotnet run
```

Access at: http://localhost:5001

## Configuration

Edit `appsettings.json` to change the API URL:

```json
{
  "BoxTrackingApi": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

## Usage Examples

### 1. Send a Single Event
- Enter Box ID (e.g., BOX-001)
- Enter Worker ID (e.g., WORKER-01)
- Select Event Type
- Click "Send Event"

### 2. Simulate Box Lifecycle
- Enter Box ID and Worker ID
- Click "Simulate Lifecycle"
- Automatically sends 3 sequential events with delays

### 3. Send Random Batch
- Enter number of events (1-100)
- Click "Send Random Batch"
- Generates random combinations of boxes, workers, and event types
