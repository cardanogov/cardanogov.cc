# Cardano Governance Data Synchronization System

A comprehensive Cardano Governance data synchronization system with 6 microservices and Main API Gateway, designed to sync Cardano blockchain data into PostgreSQL database.

## 🏗️ System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Cardano FE    │────│   Main API       │────│   PostgreSQL    │
│   (Frontend)    │    │   (Gateway)      │    │   Database      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              │
                    ┌─────────┴─────────┐
                    │                   │
            ┌───────▼────────┐  ┌──────▼──────┐
            │   Redis Cache  │  │ 6 Sync      │
            │   (Performance)│  │ Services    │
            └────────────────┘  └─────────────┘
```

## 🚀 Key Features

### ✅ 6 Data Synchronization Microservices
- **CommitteeSyncService**: Sync Committee and Treasury data
- **DrepSyncService**: Sync DReps information and Voting Power
- **EpochSyncService**: Sync Epoch data and Protocol Parameters
- **PoolSyncService**: Sync Stake Pools information
- **ProposalSyncService**: Sync Proposals and Governance data
- **VotingSyncService**: Sync Voting data and Results

### ✅ Main API Gateway
- RESTful API with versioning (v1.0)
- API Key authentication with 3 tiers (Free, Premium, Enterprise)
- Rate limiting and Redis caching
- Health checks and monitoring
- Swagger documentation

### ✅ Performance & Scalability
- Redis caching for optimal performance
- Docker containerization
- Health checks for all services
- Structured logging with Serilog
- Memory optimization for production

## 📋 System Requirements

### Prerequisites
- **Docker & Docker Compose** (recommended)
- **.NET 8 SDK** (for local development)
- **PostgreSQL 12+** (database server)
- **Redis 6+** (caching server)
- **Koios API Key** (to access Cardano blockchain data)

### Memory Requirements
- **Minimum**: 4GB RAM
- **Recommended**: 8GB+ RAM (for all services)
- **Production**: 16GB+ RAM

## 🛠️ Installation and Setup

### Option 1: Docker Compose (Recommended)

#### 1. Clone Repository
```bash
git clone <repository-url>
cd cardano-sync
```

#### 2. Configure Database Connection
**IMPORTANT**: The configuration files have been secured with placeholder values. You must replace these with your actual credentials.

**Option A: Using Environment Variables (Recommended)**
```bash
# Copy the template file
cp env.template .env

# Edit .env with your actual values
DB_HOST=your_database_host
DB_PORT=5432
DB_NAME=cardano
DB_USERNAME=your_database_username
DB_PASSWORD=your_secure_database_password
KOIOS_API_KEY=your_koios_api_key
```

**Option B: Direct Configuration**
Create `config/mainapi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_DB_HOST;Port=YOUR_DB_PORT;Database=YOUR_DB_NAME;Username=YOUR_DB_USERNAME;Password=YOUR_DB_PASSWORD;Include Error Detail=true;",
    "RedisConnection": "YOUR_REDIS_CONNECTION_STRING"
  },
  "Koios": {
    "ApiKey": "YOUR_KOIOS_API_KEY"
  },
  "Redis": {
    "ConnectionString": "YOUR_REDIS_CONNECTION_STRING",
    "InstanceName": "CardanoMainAPI:"
  }
}
```

#### 3. Configure API Keys
Create API key configuration for each microservice in `config/{service}/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_DB_HOST;Port=YOUR_DB_PORT;Database=YOUR_DB_NAME;Username=YOUR_DB_USERNAME;Password=YOUR_DB_PASSWORD;"
  },
  "Koios": {
    "ApiKey": "YOUR_KOIOS_API_KEY"
  }
}
```

**Security Note**: Replace all placeholder values (YOUR_DB_HOST, YOUR_KOIOS_API_KEY, etc.) with your actual credentials. See `CONFIGURATION_GUIDE.md` for detailed setup instructions.

#### 4. Run All Services
```bash
# Run API and Redis
docker-compose -f docker-compose-api.yml up -d

# Run 6 sync services
docker-compose -f docker-compose-sync.yml up -d
```

#### 5. Check Status
```bash
# Check all containers
docker ps

# Check logs
docker logs mainapi
docker logs committeesync
docker logs drepsync
docker logs epochsync
docker logs poolsync
docker logs proposalsync
docker logs votingsync
```

### Option 2: Local Development

#### 1. Setup Database
```bash
# Create database
createdb cardano

# Run migration scripts
psql -d cardano -f Database/Scripts/003_unique_indexes.sql
psql -d cardano -f Database/Scripts/004_proposals_withdrawal_hash.sql
```

#### 2. Setup Redis
```bash
# Run Redis server
redis-server
```

#### 3. Run Individual Services
```bash
# Run Main API
cd MainAPI
dotnet restore
dotnet run

# Run each microservice
cd Microservices/CommitteeSyncService
dotnet restore
dotnet run

cd ../DrepSyncService
dotnet restore
dotnet run

# ... similar for other services
```

## 🔑 API Key Management

### Creating API Keys

#### Method 1: Via API Endpoint
```bash
curl -X POST "http://localhost:5000/api/apikey/create" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Application",
    "description": "API key for my application",
    "type": 0,
    "createdBy": "user@example.com"
  }'
```

#### Method 2: Via Database (Production)
```sql
INSERT INTO ApiKeys (Key, Name, Description, Type, IsActive, CreatedAt, CreatedBy)
VALUES ('cardano_my_custom_key_123456789', 'My App', 'Custom API Key', 0, 1, NOW(), 'admin@example.com');
```

### Using API Keys

#### Header Method (Recommended)
```bash
curl -H "X-API-Key: your_api_key_here" \
  "http://localhost:5000/api/drep/total_drep"
```

#### Query String Method
```bash
curl "http://localhost:5000/api/drep/total_drep?api_key=your_api_key_here"
```

#### Authorization Header Method
```bash
curl -H "Authorization: Bearer your_api_key_here" \
  "http://localhost:5000/api/drep/total_drep"
```

### Rate Limits

| Plan | Per Minute | Per Hour | Per Day | Burst |
|------|------------|----------|---------|-------|
| **Free** | 60 | 1,000 | 10,000 | 10 |
| **Premium** | 300 | 10,000 | 100,000 | 50 |
| **Enterprise** | 1,000 | 50,000 | 1,000,000 | 200 |

### Test API Keys (Development)
```
Free: free_test_key_123456789
Premium: premium_test_key_987654321
Enterprise: enterprise_test_key_456789123
```

## 📊 API Endpoints

### Health Checks
```bash
GET /health                    # Basic health check
GET /health/detailed          # Detailed health with all checks
GET /health/ready             # Kubernetes readiness probe
GET /health/live              # Kubernetes liveness probe
```

### Committee API (v1.0)
```bash
GET /api/v1.0/committee/info              # Committee information
GET /api/v1.0/committee/votes             # Committee votes
GET /api/v1.0/committee/treasury          # Treasury withdrawals
GET /api/v1.0/committee/totals            # Committee totals
```

### DRep API (v1.0)
```bash
GET /api/v1.0/drep/list                   # DRep list
GET /api/v1.0/drep/info/{drepId}          # DRep information
GET /api/v1.0/drep/delegators             # DRep delegators
GET /api/v1.0/drep/voting-power           # DRep voting power
GET /api/v1.0/drep/total_drep             # Total DRep count
```

### Epoch API (v1.0)
```bash
GET /api/v1.0/epoch/current               # Current epoch info
GET /api/v1.0/epoch/protocol-parameters   # Protocol parameters
GET /api/v1.0/epoch/adastat               # Adastat epoch data
```

### Pool API (v1.0)
```bash
GET /api/v1.0/pool/list                   # Pool list
GET /api/v1.0/pool/info/{poolId}          # Pool information
GET /api/v1.0/pool/statistics             # Pool statistics
```

### Proposal API (v1.0)
```bash
GET /api/v1.0/proposal/list               # Proposal list
GET /api/v1.0/proposal/info/{proposalId}  # Proposal information
GET /api/v1.0/proposal/votes              # Proposal votes
```

### Voting API (v1.0)
```bash
GET /api/v1.0/voting/results              # Voting results
GET /api/v1.0/voting/statistics           # Voting statistics
```

### API Key Management
```bash
POST /api/apikey/create                   # Create new API key
GET /api/apikey/list                      # List API keys
GET /api/apikey/validate/{key}            # Validate API key
GET /api/apikey/rate-limit/{key}          # Check rate limit status
PUT /api/apikey/update/{key}              # Update API key
DELETE /api/apikey/delete/{key}           # Delete API key
```

## 🗄️ Database Configuration

### PostgreSQL Setup

#### 1. Create Database
```sql
CREATE DATABASE cardano;
CREATE USER cardano_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE cardano TO cardano_user;
```

#### 2. Connection String Format
```
Host=YOUR_HOST;Port=5432;Database=cardano;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;Include Error Detail=true;
```

#### 3. Required Extensions
```sql
-- Run in cardano database
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
```

### Database Migration Scripts
```bash
# Run migration scripts
psql -d cardano -f Database/Scripts/003_unique_indexes.sql
psql -d cardano -f Database/Scripts/004_proposals_withdrawal_hash.sql
```

## 🔧 Configuration Files

### Main API Configuration (`config/mainapi/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_DB_HOST;Port=5432;Database=cardano;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;Include Error Detail=true;",
    "RedisConnection": "redis:6379"
  },
  "Koios": {
    "ApiKey": "YOUR_KOIOS_API_KEY"
  },
  "Redis": {
    "ConnectionString": "redis:6379",
    "InstanceName": "CardanoMainAPI:"
  },
  "ApiKeySettings": {
    "DefaultKeyLength": 32,
    "KeyPrefix": "cardano_",
    "EnableKeyRotation": true,
    "KeyRotationDays": 90
  },
  "RateLimiting": {
    "EnableApiKeyRateLimiting": true
  }
}
```

### Microservice Configuration (`config/{service}/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_DB_HOST;Port=5432;Database=cardano;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;"
  },
  "Koios": {
    "ApiKey": "YOUR_KOIOS_API_KEY"
  },
  "DatabaseSync": {
    "MaxConcurrentDbOperations": 2,
    "SyncIntervalMinutes": 5,
    "EnableHealthChecks": true
  }
}
```

## 📈 Monitoring & Logging

### Health Checks
```bash
# Check health of all services
curl http://localhost:5000/health/detailed

# Check individual microservice
curl http://localhost:5000/health
```

### Logs Location
```
logs/
├── mainapi/
│   └── log-YYYY-MM-DD.txt
├── committeesync/
│   └── log-YYYY-MM-DD.txt
├── drepsync/
│   └── log-YYYY-MM-DD.txt
├── epochsync/
│   └── log-YYYY-MM-DD.txt
├── poolsync/
│   └── log-YYYY-MM-DD.txt
├── proposalsync/
│   └── log-YYYY-MM-DD.txt
└── votingsync/
    └── log-YYYY-MM-DD.txt
```

### Monitoring Commands
```bash
# View real-time logs
docker logs -f mainapi
docker logs -f committeesync

# Check resource usage
docker stats

# Check database connections
psql -d cardano -c "SELECT * FROM pg_stat_activity WHERE datname='cardano';"
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Database Connection Failed
```bash
# Check connection string
echo $DATABASE_URL

# Test connection
psql -h YOUR_HOST -p 5432 -U YOUR_USERNAME -d cardano -c "SELECT 1;"

# Check firewall
telnet YOUR_HOST 5432
```

#### 2. Redis Connection Failed
```bash
# Check Redis server
redis-cli ping

# Test connection from container
docker exec -it mainapi redis-cli ping
```

#### 3. API Key Issues
```bash
# Validate API key
curl "http://localhost:5000/api/apikey/validate/your_key"

# Check rate limit
curl "http://localhost:5000/api/apikey/rate-limit/your_key"
```

#### 4. Microservice Sync Issues
```bash
# Check logs
docker logs committeesync
docker logs drepsync

# Check health
docker exec committeesync dotnet CommitteeSyncService.dll --health-check
```

### Performance Optimization

#### 1. Memory Optimization
```yaml
# In docker-compose-sync.yml
environment:
  - DOTNET_GCServer=0  # Use workstation GC
  - DOTNET_GCConcurrent=1
  - DatabaseSync__MaxConcurrentDbOperations=2
```

#### 2. Database Optimization
```sql
-- Check indexes
SELECT schemaname, tablename, indexname, indexdef 
FROM pg_indexes 
WHERE schemaname = 'public';

-- Analyze tables
ANALYZE;
```

#### 3. Redis Optimization
```bash
# Check Redis memory
redis-cli info memory

# Clear cache if needed
redis-cli flushdb
```

## 🔒 Security Best Practices

### 1. Configuration Security
- **NEVER commit sensitive data** to version control
- Replace all placeholder values with your actual credentials
- Use environment variables for sensitive configuration
- Follow the `CONFIGURATION_GUIDE.md` for secure setup

### 2. API Key Security
- Never commit API keys to source code
- Use environment variables or secure vaults
- Rotate keys regularly (90 days)
- Monitor usage patterns
- Use different keys for different environments

### 3. Database Security
- Use strong, unique passwords
- Limit database user permissions to minimum required
- Enable SSL connections in production
- Use connection pooling and timeouts
- Regular security updates

### 4. Network Security
- Configure firewall rules
- Use HTTPS in production
- Implement rate limiting
- Monitor access logs
- Use VPN for database access in production

### 5. Secrets Management
- Use `.env` files for local development (never commit)
- Use secret managers in production (Azure Key Vault, AWS Secrets Manager)
- Implement key rotation policies
- Audit access to sensitive configuration

## 📝 Development Guide

### Adding New Microservice
1. Create project in `Microservices/` folder
2. Implement `Program.cs` with health checks
3. Create `Dockerfile`
4. Add to `docker-compose-sync.yml`
5. Update documentation

### Adding New API Endpoint
1. Create controller in `MainAPI/Controllers/`
2. Implement business logic in `MainAPI.Application/Queries/`
3. Add to Swagger documentation
4. Write unit tests
5. Update API documentation

## 📞 Support

### Configuration Help
- **Configuration Guide**: See `CONFIGURATION_GUIDE.md` for detailed setup instructions
- **Environment Template**: Use `env.template` as a starting point for secure configuration
- **Security Setup**: Follow security best practices in the Configuration Guide

### Health Check Endpoints
- Main API: `http://localhost:5000/health`
- Swagger: `http://localhost:5000/swagger`

### Logs and Debugging
- Logs: `logs/` directory
- Health checks: `/health/detailed`
- Database status: PostgreSQL queries

### Common Commands
```bash
# Restart all services
docker-compose -f docker-compose-api.yml restart
docker-compose -f docker-compose-sync.yml restart

# Update and rebuild
docker-compose -f docker-compose-api.yml up -d --build
docker-compose -f docker-compose-sync.yml up -d --build

# Clean up
docker system prune -f
```

## 📄 License

This project is licensed under the MIT License.

---

**Note**: Make sure to replace all placeholder values (YOUR_HOST, YOUR_USERNAME, YOUR_PASSWORD, YOUR_KOIOS_API_KEY) with your actual information before running the system.