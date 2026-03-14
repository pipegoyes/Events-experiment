# Sentry Error Tracking Setup

Sentry is integrated into all Box Tracking components for real-time error tracking and monitoring.

## What's Tracked

All components report errors to Sentry:
- **API**: Request failures, RabbitMQ connection issues
- **Event Processor**: Message processing errors, RabbitMQ consumer failures
- **Dashboard**: UI errors, rendering issues
- **Event Simulator**: API communication failures

## Setup Instructions

### 1. Create a Sentry Project

**Option A: Using the Sentry CLI (if available)**
```bash
# Install Sentry CLI
npm install -g @sentry/cli

# Login to Sentry
sentry-cli login

# Create a new project
sentry-cli projects create box-tracking \
  --organization your-org \
  --team your-team \
  --platform dotnet
```

**Option B: Using the Sentry Web UI**
1. Go to https://sentry.io
2. Sign up or log in
3. Create a new project
4. Choose platform: **.NET**
5. Name it: **box-tracking** (or any name you prefer)
6. Copy the DSN from the project settings

### 2. Get Your Sentry DSN

After creating the project, you'll get a DSN that looks like:
```
https://examplePublicKey@o0.ingest.sentry.io/0
```

Or find it at: **Settings → Projects → [Your Project] → Client Keys (DSN)**

### 3. Configure Sentry

#### For Local Development (Docker Compose)

1. Create a `.env` file in the project root:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add your DSN:
   ```bash
   SENTRY_DSN=https://your-key@o0.ingest.sentry.io/12345
   ```

3. Restart containers:
   ```bash
   docker-compose down
   docker-compose up --build
   ```

#### For Azure Deployment (Terraform)

1. Edit `terraform/terraform.tfvars`:
   ```hcl
   sentry_dsn = "https://your-key@o0.ingest.sentry.io/12345"
   ```

2. Apply Terraform changes:
   ```bash
   cd terraform
   terraform apply
   ```

   Or redeploy everything:
   ```bash
   ./deploy.sh
   ```

## Verify It's Working

### Test Error Reporting

#### API Test
```bash
# Trigger an intentional error
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{}'  # Invalid event (missing required fields)
```

#### Check Sentry Dashboard
1. Go to https://sentry.io
2. Open your **box-tracking** project
3. Go to **Issues** tab
4. You should see the error appear within seconds

### What Gets Tracked

**Automatic Tracking:**
- Unhandled exceptions
- Request failures (HTTP 4xx/5xx)
- Background task errors
- RabbitMQ connection failures

**Context Captured:**
- Environment (Development/Production)
- Release version (e.g., `boxtracking-api@1.0.0`)
- Request data (headers, query params)
- User-agent and IP
- Stack traces

## Sentry Features Used

### 1. Release Tracking
Each component reports its version:
- `boxtracking-api@1.0.0`
- `boxtracking-processor@1.0.0`
- `boxtracking-dashboard@1.0.0`
- `boxtracking-simulator@1.0.0`

You can track errors per version in Sentry.

### 2. Environment Separation
Errors are tagged by environment:
- `Development` - Local/test errors
- `Production` - Azure deployment errors

Filter by environment in Sentry dashboard.

### 3. Performance Monitoring
Transaction tracing is enabled (`TracesSampleRate = 1.0`):
- API request duration
- Background job timing
- External service calls

View in Sentry → Performance tab.

### 4. Privacy
`SendDefaultPii = false` - Personal data is **not** sent to Sentry by default.

## Configuration Options

Edit `appsettings.json` in each project to customize Sentry:

```json
{
  "Sentry": {
    "Dsn": "your-dsn-here",
    "TracesSampleRate": 1.0,
    "Environment": "Development",
    "SendDefaultPii": false
  }
}
```

**Options:**
- `TracesSampleRate`: Percentage of transactions to trace (0.0 to 1.0)
  - `1.0` = 100% (all requests tracked)
  - `0.1` = 10% (reduce overhead in production)
- `SendDefaultPii`: Send user IP, cookies, etc.
  - `false` = Privacy-focused (recommended)
  - `true` = More context for debugging

## Disable Sentry

### Temporarily Disable

**Docker Compose:**
```bash
# Remove SENTRY_DSN from .env or set it to empty
SENTRY_DSN=
```

**Azure:**
```hcl
# In terraform.tfvars
sentry_dsn = ""
```

### Permanently Remove

1. Remove Sentry NuGet packages from `.csproj` files:
   ```xml
   <!-- Delete these lines -->
   <PackageReference Include="Sentry.AspNetCore" Version="4.3.0" />
   <PackageReference Include="Sentry" Version="4.3.0" />
   <PackageReference Include="Sentry.Extensions.Logging" Version="4.3.0" />
   ```

2. Remove Sentry initialization from `Program.cs`:
   ```csharp
   // Delete this block
   builder.WebHost.UseSentry(options => { ... });
   ```

3. Rebuild and redeploy

## Pricing

**Sentry Free Tier:**
- ✅ 5,000 errors/month
- ✅ 10,000 performance units/month
- ✅ 1 year data retention
- ✅ Unlimited team members

For this prototype, the free tier is more than enough!

**Paid Plans:** Start at $26/month if you exceed free tier.

## Troubleshooting

### Errors not showing in Sentry

1. **Check DSN is set:**
   ```bash
   # Docker
   docker-compose exec api env | grep SENTRY

   # Azure
   az containerapp show --name boxtrack-api --resource-group rg-boxtracking \
     --query "properties.template.containers[0].env[?name=='Sentry__Dsn']"
   ```

2. **Check logs for Sentry initialization:**
   ```bash
   # Look for "Sentry integration initialized"
   docker-compose logs api | grep -i sentry
   ```

3. **Verify DSN format:**
   - Must start with `https://`
   - Must include `@` and `.ingest.sentry.io`
   - Example: `https://abc123@o0.ingest.sentry.io/12345`

4. **Test with an intentional error:**
   ```csharp
   // Add this endpoint to test
   app.MapGet("/test-error", () => throw new Exception("Test Sentry error"));
   ```

### Rate limiting

If you see "Event dropped due to rate limiting":
- You've exceeded your quota
- Upgrade your Sentry plan
- Or reduce `TracesSampleRate` to 0.1 (10%)

## Best Practices

1. **Use environments:** Separate Development/Staging/Production in Sentry
2. **Set up alerts:** Get notified on Slack/Email when errors spike
3. **Add breadcrumbs:** Log custom context before errors
4. **Link to GitHub:** Connect Sentry to your repo for direct commits from issues
5. **Review weekly:** Check Sentry dashboard every week for patterns

## Custom Error Tracking

### Log custom errors in code

```csharp
using Sentry;

try
{
    // Your code
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    // Handle gracefully
}
```

### Add custom context

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("box_id", boxId);
    scope.SetTag("worker_id", workerId);
    scope.SetExtra("event_type", eventType);
});
```

### Track performance

```csharp
var transaction = SentrySdk.StartTransaction("process-event", "task");
try
{
    // Your work
}
finally
{
    transaction.Finish();
}
```

## Support

- **Sentry Docs:** https://docs.sentry.io/platforms/dotnet/
- **Sentry CLI:** https://docs.sentry.io/product/cli/
- **Community:** https://forum.sentry.io/

---

**Ready to track errors? Just add your DSN and you're good to go!** 🐸
