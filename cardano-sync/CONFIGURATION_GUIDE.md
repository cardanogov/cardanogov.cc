# Configuration Guide

This guide explains how to securely configure the Cardano Sync System with your own database credentials and API keys.

## 🔐 Security Overview

**IMPORTANT**: Never commit sensitive information like database passwords, API keys, or connection strings to version control. The configuration files have been updated to use placeholder values that you must replace with your actual credentials.

## 📋 Configuration Files Updated

The following files have been secured with placeholder values:

### Main API Configuration
- `MainAPI/appsettings.json` - Main API gateway configuration

### Microservices Configuration
- `Microservices/CommitteeSyncService/appsettings.json`
- `Microservices/DrepSyncService/appsettings.json`
- `Microservices/EpochSyncService/appsettings.json`
- `Microservices/PoolSyncService/appsettings.json`
- `Microservices/ProposalSyncService/appsettings.json`
- `Microservices/VotingSyncService/appsettings.json`
- `SharedLibrary/appsettings.json`

## 🔧 Configuration Methods

### Method 1: Environment Variables (Recommended)

1. **Copy the template file:**
   ```bash
   cp env.template .env
   ```

2. **Edit the .env file with your actual values:**
   ```bash
   # Database Configuration
   DB_HOST=your_actual_database_host
   DB_PORT=5432
   DB_NAME=cardano
   DB_USERNAME=your_actual_username
   DB_PASSWORD=your_actual_secure_password
   
   # API Keys
   KOIOS_API_KEY=your_actual_koios_api_key
   CARDANO_KEY=your_actual_cardano_key
   DEEP_AI_KEY=your_actual_deep_ai_key
   
   # Redis
   REDIS_CONNECTION_STRING=localhost:6379
   ```

3. **Load environment variables in your application startup**

### Method 2: Direct Configuration File Editing

Replace the placeholder values in each `appsettings.json` file:

#### Database Connection Strings
Replace these placeholders:
- `YOUR_DB_HOST` → Your database host
- `YOUR_DB_PORT` → Your database port (usually 5432)
- `YOUR_DB_NAME` → Your database name
- `YOUR_DB_USERNAME` → Your database username
- `YOUR_DB_PASSWORD` → Your database password

#### API Keys
Replace these placeholders:
- `YOUR_KOIOS_API_KEY` → Your Koios API key
- `YOUR_CARDANO_KEY` → Your Cardano key
- `YOUR_DEEP_AI_KEY` → Your DeepAI key

#### Redis Configuration
Replace these placeholders:
- `YOUR_REDIS_CONNECTION_STRING` → Your Redis connection string

## 🗄️ Database Setup

### PostgreSQL Database Creation
```sql
-- Create database
CREATE DATABASE cardano;

-- Create user
CREATE USER cardano_user WITH PASSWORD 'your_secure_password';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE cardano TO cardano_user;

-- Required extensions
\c cardano
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
```

### Connection String Format
```
Host=YOUR_DB_HOST;Port=YOUR_DB_PORT;Database=YOUR_DB_NAME;Username=YOUR_DB_USERNAME;Password=YOUR_DB_PASSWORD;Include Error Detail=true;
```

### Example Connection Strings

#### Local Development
```
Host=localhost;Port=5432;Database=cardano;Username=postgres;Password=yourpassword;Include Error Detail=true;
```

#### Production
```
Host=your-db-server.com;Port=5432;Database=cardano;Username=prod_user;Password=secure_password123;Include Error Detail=true;Maximum Pool Size=30;Minimum Pool Size=5;Timeout=60;CommandTimeout=120;
```

## 🔑 API Keys Setup

### Koios API Key
1. Visit [Koios API](https://api.koios.rest/)
2. Register for an API key
3. Replace `YOUR_KOIOS_API_KEY` with your actual key

### Other API Keys
- `YOUR_CARDANO_KEY`: Your Cardano-specific API key
- `YOUR_DEEP_AI_KEY`: Your DeepAI API key (if using AI features)

## 🚀 Deployment Options

### Option 1: Docker with Environment Variables
```bash
# Set environment variables
export DB_HOST=your_host
export DB_PASSWORD=your_password
export KOIOS_API_KEY=your_key

# Run with Docker
docker-compose -f docker-compose-api.yml up -d
docker-compose -f docker-compose-sync.yml up -d
```

### Option 2: Docker with .env file
```bash
# Create .env file with your values
cp env.template .env
# Edit .env with your actual values

# Run with Docker Compose
docker-compose --env-file .env -f docker-compose-api.yml up -d
docker-compose --env-file .env -f docker-compose-sync.yml up -d
```

### Option 3: Local Development
```bash
# Update appsettings.json files with your values
# Run each service
cd MainAPI && dotnet run
cd Microservices/CommitteeSyncService && dotnet run
# ... etc
```

## 🔒 Security Best Practices

### 1. Environment Variables
- Use environment variables for sensitive data
- Never hardcode passwords or API keys
- Use different credentials for different environments

### 2. Database Security
- Use strong, unique passwords
- Limit database user permissions
- Enable SSL connections in production
- Use connection pooling

### 3. API Key Security
- Rotate API keys regularly
- Monitor API key usage
- Use different keys for different environments
- Store keys securely (environment variables, secret managers)

### 4. Network Security
- Use HTTPS in production
- Configure firewall rules
- Limit database access by IP
- Use VPN for database access in production

## 🚨 Troubleshooting

### Common Configuration Issues

#### 1. Database Connection Failed
```bash
# Test connection
psql -h YOUR_HOST -p 5432 -U YOUR_USERNAME -d cardano -c "SELECT 1;"

# Check connection string format
# Ensure all required parameters are included
```

#### 2. API Key Invalid
```bash
# Test API key
curl -H "X-API-Key: YOUR_KEY" "https://api.koios.rest/api/v0/epoch_info"

# Check key format and expiration
```

#### 3. Redis Connection Failed
```bash
# Test Redis connection
redis-cli ping

# Check Redis server status
systemctl status redis
```

### Configuration Validation

#### Check Main API Configuration
```bash
curl http://localhost:5000/health/detailed
```

#### Check Microservice Configuration
```bash
# Check logs for configuration errors
docker logs committeesync
docker logs drepsync
# ... etc
```

## 📝 Configuration Checklist

- [ ] Database created and accessible
- [ ] Database user created with proper permissions
- [ ] Connection strings updated in all configuration files
- [ ] API keys obtained and configured
- [ ] Redis server running and accessible
- [ ] Environment variables set (if using)
- [ ] CORS origins configured for your frontend
- [ ] Health checks passing
- [ ] All services starting without errors

## 🔄 Configuration Updates

When updating configuration:

1. **Backup current configuration**
2. **Update configuration files**
3. **Restart affected services**
4. **Verify health checks**
5. **Monitor logs for errors**

## 📞 Support

If you encounter configuration issues:

1. Check the logs: `logs/` directory
2. Run health checks: `/health/detailed`
3. Verify database connectivity
4. Test API key validity
5. Check network connectivity

## 🔗 Additional Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)
- [Koios API Documentation](https://api.koios.rest/)
- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
