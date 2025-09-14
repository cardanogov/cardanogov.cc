# Cardano Governance Platform ([cardanogov.cc](https://cardanogov.cc))

A comprehensive, full-stack Cardano governance platform providing real-time insights, monitoring, and participation tools for Cardano's decentralized governance system. This platform consists of a modern Angular frontend and a robust .NET microservices backend.

## 🌟 Overview

The Cardano Governance Platform is designed to provide transparency, accessibility, and powerful analytics for Cardano's governance ecosystem. It serves as a central hub for monitoring and participating in Cardano's decentralized governance processes.

### Key Features

- **📊 Real-time Governance Analytics** - Live monitoring of DReps, voting, proposals, and committee activities
- **🗳️ Interactive Voting Interface** - User-friendly voting tools with detailed proposal analysis
- **👥 DRep Management** - Comprehensive DRep monitoring and analysis tools
- **🏛️ Committee Tracking** - Real-time committee activities and decision tracking
- **💰 Treasury Monitoring** - Treasury allocation and spending transparency
- **🏊 Stake Pool Operations** - SPO monitoring and analytics
- **📈 Advanced Data Visualization** - Interactive charts and dashboards
- **🔐 Secure API System** - Rate-limited API with key-based authentication
- **⚡ Real-time Synchronization** - 6 microservices keeping data up-to-date

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Cardano Governance Platform                  │
├─────────────────────────────────────────────────────────────────┤
│  Frontend (Angular)           │  Backend (Microservices)       │
│  ┌─────────────────────────┐  │  ┌─────────────────────────────┐ │
│  │  Dashboard & Analytics  │  │  │     Main API Gateway        │ │
│  │  Voting Interface       │  │  │     (RESTful API v1.0)     │ │
│  │  DRep Management        │  │  │     Rate Limiting           │ │
│  │  Proposal Tracking      │  │  │     Authentication          │ │
│  │  Real-time Updates      │  │  │     Health Checks           │ │
│  └─────────────────────────┘  │  └─────────────────────────────┘ │
│                               │  ┌─────────────────────────────┐ │
│                               │  │    Data Sync Services       │ │
│                               │  │  ┌─────────────────────────┐ │ │
│                               │  │  │ CommitteeSyncService    │ │ │
│                               │  │  │ DrepSyncService         │ │ │
│                               │  │  │ EpochSyncService        │ │ │
│                               │  │  │ PoolSyncService         │ │ │
│                               │  │  │ ProposalSyncService     │ │ │
│                               │  │  │ VotingSyncService       │ │ │
│                               │  │  └─────────────────────────┘ │ │
│                               │  └─────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                         │
├─────────────────────────────────────────────────────────────────┤
│  PostgreSQL Database  │  Redis Cache  │  Koios API  │  Logging  │
│  (Cardano Data)       │  (Performance)│  (Blockchain)│  (Serilog)│
└─────────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
cardanogov.cc/
├── cardano-fe/                    # Angular Frontend Application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/             # Core functionality & services
│   │   │   ├── layout/           # Layout components
│   │   │   ├── pages/            # Feature modules
│   │   │   └── shared/           # Shared components & utilities
│   │   ├── assets/               # Static assets
│   │   └── environments/         # Environment configurations
│   ├── package.json              # Frontend dependencies
│   └── README.md                 # Frontend documentation
│
├── cardano-sync/                 # .NET Backend Services
│   ├── MainAPI/                  # Main API Gateway
│   ├── Microservices/            # Data synchronization services
│   │   ├── CommitteeSyncService/
│   │   ├── DrepSyncService/
│   │   ├── EpochSyncService/
│   │   ├── PoolSyncService/
│   │   ├── ProposalSyncService/
│   │   └── VotingSyncService/
│   ├── SharedLibrary/            # Shared libraries
│   ├── docker-compose-api.yml    # API & Redis deployment
│   ├── docker-compose-sync.yml   # Microservices deployment
│   ├── CONFIGURATION_GUIDE.md    # Security & setup guide
│   ├── env.template              # Environment variables template
│   └── README.md                 # Backend documentation
│
└── README.md                     # This file - Project overview
```

## 🚀 Technology Stack

### Frontend (cardano-fe)
- **Framework**: Angular 19.2.3 with TypeScript 5.5.4
- **UI Framework**: Nebular Theme 15.0.0 + Angular Material 19.2.3
- **Styling**: SCSS + Tailwind CSS 3.4.17 + Bootstrap 5.3.2
- **Charts**: ApexCharts 4.5.0 + Chart.js 4.4.8 + D3.js 7.9.0
- **State Management**: NgRx 19.0.1 (Effects, Entity)
- **Authentication**: Nebular Auth 15.0.0
- **Server-Side Rendering**: Angular SSR 19.2.5

### Backend (cardano-sync)
- **Framework**: .NET 8 with C#
- **Architecture**: Microservices + API Gateway
- **Database**: PostgreSQL 12+
- **Caching**: Redis 6+
- **Authentication**: API Key-based with rate limiting
- **Logging**: Serilog with structured logging
- **Containerization**: Docker + Docker Compose
- **Data Source**: Koios API for Cardano blockchain data

## 🛠️ Quick Start

### Prerequisites

- **Node.js 18+** and **npm 9+** (for frontend)
- **.NET 8 SDK** (for backend)
- **Docker & Docker Compose** (recommended)
- **PostgreSQL 12+** (database)
- **Redis 6+** (caching)
- **Koios API Key** (for Cardano data)

### Option 1: Docker Deployment (Recommended)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd cardanogov.cc
   ```

2. **Configure environment variables**
   ```bash
   # Copy and edit environment template
   cp cardano-sync/env.template cardano-sync/.env
   nano cardano-sync/.env
   ```

3. **Start the backend services**
   ```bash
   cd cardano-sync
   docker-compose -f docker-compose-api.yml up -d
   docker-compose -f docker-compose-sync.yml up -d
   ```

4. **Start the frontend**
   ```bash
   cd ../cardano-fe
   npm install
   npm start
   ```

5. **Access the application**
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:5000
   - Swagger Documentation: http://localhost:5000/swagger

### Option 2: Local Development

1. **Setup Database**
   ```bash
   # Create PostgreSQL database
   createdb cardano
   
   # Run migration scripts
   psql -d cardano -f cardano-sync/Database/Scripts/003_unique_indexes.sql
   psql -d cardano -f cardano-sync/Database/Scripts/004_proposals_withdrawal_hash.sql
   ```

2. **Start Redis**
   ```bash
   redis-server
   ```

3. **Configure Backend**
   ```bash
   cd cardano-sync
   # Update appsettings.json files with your database credentials
   # See CONFIGURATION_GUIDE.md for detailed instructions
   ```

4. **Run Backend Services**
   ```bash
   # Start Main API
   cd MainAPI && dotnet run
   
   # Start Microservices (in separate terminals)
   cd Microservices/CommitteeSyncService && dotnet run
   cd Microservices/DrepSyncService && dotnet run
   # ... continue for other services
   ```

5. **Run Frontend**
   ```bash
   cd cardano-fe
   npm install
   npm start
   ```

## 📊 Core Features

### Frontend Features

#### 🏠 Dashboard
- Real-time governance metrics
- Interactive charts and visualizations
- Activity feed with live updates
- Key performance indicators

#### 👥 DRep Management
- DRep listing and search
- Voting power analysis
- Delegation tracking
- Performance metrics

#### 🗳️ Voting Interface
- Proposal browsing and filtering
- Detailed proposal analysis
- Voting history tracking
- Voting power calculator

#### 📋 Proposal Tracking
- Proposal lifecycle monitoring
- Voting statistics
- Impact analysis
- Historical data

#### 🏛️ Committee Operations
- Committee member tracking
- Decision monitoring
- Activity timeline
- Treasury oversight

#### 🏊 Stake Pool Analytics
- Pool performance metrics
- Delegation analytics
- Rewards tracking
- Pool ranking system

### Backend Features

#### 🔌 API Gateway
- RESTful API with versioning (v1.0)
- Rate limiting (Free, Premium, Enterprise tiers)
- API key authentication
- Health checks and monitoring
- Swagger documentation

#### 🔄 Data Synchronization
- **CommitteeSyncService**: Committee and treasury data
- **DrepSyncService**: DRep information and voting power
- **EpochSyncService**: Epoch data and protocol parameters
- **PoolSyncService**: Stake pool information
- **ProposalSyncService**: Proposals and governance data
- **VotingSyncService**: Voting data and results

#### 🚀 Performance Features
- Redis caching for optimal performance
- Connection pooling
- Circuit breaker pattern
- Health monitoring
- Structured logging

## 🔐 Security

### API Security
- API key-based authentication
- Rate limiting per tier (Free: 10K/day, Premium: 100K/day, Enterprise: 1M/day)
- CORS configuration
- Input validation and sanitization

### Data Security
- Environment variable configuration
- No hardcoded credentials
- Secure database connections
- Encrypted data transmission

### Best Practices
- Regular security updates
- Monitoring and alerting
- Access logging
- Key rotation policies

## 📈 API Endpoints

### Core APIs
```bash
# Health & Monitoring
GET /health                    # Basic health check
GET /health/detailed          # Detailed health status

# DRep APIs
GET /api/v1.0/drep/list       # DRep listing
GET /api/v1.0/drep/info/{id}  # DRep information
GET /api/v1.0/drep/voting-power # Voting power data

# Committee APIs
GET /api/v1.0/committee/info  # Committee information
GET /api/v1.0/committee/votes # Committee votes

# Proposal APIs
GET /api/v1.0/proposal/list   # Proposal listing
GET /api/v1.0/proposal/info/{id} # Proposal details

# Pool APIs
GET /api/v1.0/pool/list       # Pool listing
GET /api/v1.0/pool/statistics # Pool statistics

# API Key Management
POST /api/apikey/create       # Create API key
GET /api/apikey/validate/{key} # Validate API key
```

## 🔧 Configuration

### Environment Variables
```bash
# Database
DB_HOST=your_database_host
DB_PORT=5432
DB_NAME=cardano
DB_USERNAME=your_username
DB_PASSWORD=your_password

# API Keys
KOIOS_API_KEY=your_koios_api_key
CARDANO_KEY=your_cardano_key

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# CORS
ALLOWED_ORIGINS=http://localhost:4200,https://yourdomain.com
```

### Configuration Files
- **Frontend**: `cardano-fe/src/environments/environment.ts`
- **Backend**: `cardano-sync/MainAPI/appsettings.json`
- **Microservices**: `cardano-sync/Microservices/*/appsettings.json`

## 📊 Monitoring & Logging

### Health Checks
- Main API health: `/health/detailed`
- Microservice health: Built-in health checks
- Database connectivity monitoring
- Redis connectivity monitoring

### Logging
- Structured logging with Serilog
- Log files: `logs/` directory
- Console logging for development
- Log rotation and retention

### Performance Monitoring
- Request/response timing
- Database query performance
- Cache hit rates
- Memory usage monitoring

## 🚀 Deployment

### Production Deployment

1. **Database Setup**
   ```sql
   CREATE DATABASE cardano;
   CREATE USER cardano_user WITH PASSWORD 'secure_password';
   GRANT ALL PRIVILEGES ON DATABASE cardano TO cardano_user;
   ```

2. **Environment Configuration**
   ```bash
   # Set production environment variables
   export ASPNETCORE_ENVIRONMENT=Production
   export DB_HOST=your_production_db_host
   export KOIOS_API_KEY=your_production_api_key
   ```

3. **Docker Deployment**
   ```bash
   # Build and deploy
   docker-compose -f docker-compose-api.yml up -d --build
   docker-compose -f docker-compose-sync.yml up -d --build
   ```

4. **Frontend Build**
   ```bash
   cd cardano-fe
   npm run build
   # Deploy dist/ folder to your web server
   ```

### Scaling Considerations
- Horizontal scaling of microservices
- Database connection pooling
- Redis clustering for high availability
- Load balancing for API gateway
- CDN for frontend assets

## 🧪 Testing

### Frontend Testing
```bash
cd cardano-fe
npm test                    # Unit tests
npm run e2e                # End-to-end tests
npm run test:coverage      # Coverage report
```

### Backend Testing
```bash
cd cardano-sync
dotnet test                # Run all tests
dotnet test --logger trx   # Generate test results
```

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Follow coding standards**
   - Frontend: Angular style guide
   - Backend: .NET coding conventions
4. **Write tests** for new features
5. **Update documentation**
6. **Submit a pull request**

### Development Guidelines
- **Frontend**: Follow Angular best practices, use TypeScript strictly
- **Backend**: Follow SOLID principles, implement proper error handling
- **Security**: Never commit sensitive data, use environment variables
- **Performance**: Optimize queries, implement caching, monitor performance

## 📚 Documentation

- **Frontend**: [cardano-fe/README.md](cardano-fe/README.md)
- **Backend**: [cardano-sync/README.md](cardano-sync/README.md)
- **Configuration**: [cardano-sync/CONFIGURATION_GUIDE.md](cardano-sync/CONFIGURATION_GUIDE.md)
- **API Documentation**: Available at `/swagger` endpoint when running

## 🔗 External Dependencies

- **Koios API**: Primary data source for Cardano blockchain data
- **PostgreSQL**: Primary database for data storage
- **Redis**: Caching and session storage
- **Docker**: Containerization platform

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

### Getting Help
- **Documentation**: Check the README files in each subdirectory
- **Configuration Issues**: See `CONFIGURATION_GUIDE.md`
- **API Issues**: Check Swagger documentation at `/swagger`
- **Health Checks**: Monitor `/health/detailed` endpoint

### Troubleshooting
1. **Check logs**: `logs/` directory in backend
2. **Verify configuration**: Environment variables and appsettings.json
3. **Test connectivity**: Database and Redis connections
4. **Monitor health**: Health check endpoints
5. **Review documentation**: Configuration and setup guides

### Community
- Create issues in the repository for bugs or feature requests
- Check existing documentation for common solutions
- Review the API documentation for integration help

---

**Built with ❤️ for the Cardano community**

*This platform provides transparency and accessibility to Cardano's governance system, empowering users to participate effectively in decentralized decision-making processes.*
