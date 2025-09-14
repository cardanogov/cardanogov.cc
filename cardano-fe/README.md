# Cardano Governance Frontend (cardano-fe)

A modern Angular application for Cardano governance platform, providing comprehensive tools for monitoring and participating in Cardano's decentralized governance system.

## 🚀 Overview

This frontend application serves as the user interface for the Cardano governance platform, offering real-time insights into:

- **DReps (Delegated Representatives)** - Monitor and analyze DRep activities
- **Voting** - Participate in governance voting processes
- **Proposals** - Track and analyze governance proposals
- **SPO (Stake Pool Operators)** - Monitor stake pool operations
- **Committee** - Track committee activities and decisions
- **Treasury** - Monitor treasury allocations and spending
- **Activity** - Real-time governance activity feed

## 🏗️ Architecture

### Technology Stack

- **Framework**: Angular 19.2.3 with TypeScript 5.5.4
- **UI Framework**: Nebular Theme 15.0.0 + Angular Material 19.2.3
- **Styling**: SCSS + Tailwind CSS 3.4.17 + Bootstrap 5.3.2
- **Charts**: ApexCharts 4.5.0 + Chart.js 4.4.8 + D3.js 7.9.0
- **State Management**: NgRx 19.0.1 (Effects, Entity)
- **Authentication**: Nebular Auth 15.0.0
- **HTTP Client**: Angular HttpClient with custom interceptors
- **Server-Side Rendering**: Angular SSR 19.2.5

### Project Structure

```
src/
├── app/
│   ├── core/                    # Core functionality
│   │   ├── guards/             # Route guards (auth, admin)
│   │   ├── interceptors/       # HTTP interceptors
│   │   ├── services/           # Core services
│   │   ├── helpers/            # Utility helpers
│   │   └── models/             # Core data models
│   ├── layout/                 # Layout components
│   │   ├── header/             # Application header
│   │   ├── sidebar/            # Navigation sidebar
│   │   └── footer/             # Application footer
│   ├── pages/                  # Feature modules
│   │   ├── dashboard/          # Main dashboard
│   │   ├── dreps/              # DRep management
│   │   ├── voting/             # Voting interface
│   │   ├── proposals/          # Proposal tracking
│   │   ├── spo/                # Stake pool operators
│   │   ├── cc/                 # Constitutional committee
│   │   ├── activity/           # Activity feed
│   │   └── more/               # Additional features
│   ├── shared/                 # Shared components & utilities
│   │   ├── components/         # Reusable UI components
│   │   ├── models/             # Shared data models
│   │   ├── pipes/              # Custom pipes
│   │   ├── directives/         # Custom directives
│   │   └── services/           # Shared services
│   └── styles/                 # Global styles
├── assets/                     # Static assets
│   ├── images/                 # Image assets
│   ├── icons/                  # Icon assets
│   └── fonts/                  # Font assets
└── environments/               # Environment configurations
```

## 🔧 Core Services

### API Services
- **ApiService**: Base HTTP service with standardized request/response handling
- **AccountService**: User account management
- **AuthService**: Authentication and authorization
- **DrepService**: DRep data management
- **VotingService**: Voting operations
- **ProposalService**: Proposal management
- **PoolService**: Stake pool data
- **CommitteeService**: Committee operations
- **TreasuryService**: Treasury monitoring
- **EpochService**: Epoch data tracking

### Utility Services
- **CacheService**: Data caching with TTL support
- **LoadingService**: Global loading state management
- **LoggerService**: Centralized logging
- **WebSocketService**: Real-time data updates
- **SearchService**: Global search functionality

## 🎨 UI Components

### Shared Components
- **Card**: Reusable card component with multiple variants
- **Table**: Data table with sorting, filtering, and pagination
- **Chart**: Chart wrapper supporting ApexCharts and Chart.js
- **Modal**: Modal dialogs with customizable content
- **Button**: Standardized button component
- **Search**: Global search component
- **Skeleton**: Loading skeleton components
- **Breadcrumb**: Navigation breadcrumb
- **Menu**: Navigation menu component

### Specialized Components
- **VennDiagram**: Interactive Venn diagram for data visualization
- **CarouselTag**: Tag carousel for category display
- **FullscreenSearch**: Full-screen search interface
- **ApexModal**: Modal wrapper for ApexCharts

## 🔐 Authentication & Security

- **Nebular Auth**: Complete authentication system
- **Route Guards**: Protected routes with AuthGuard and AdminGuard
- **HTTP Interceptors**: 
  - Authentication interceptor for token management
  - Error interceptor for centralized error handling
  - Loading interceptor for request state management
- **Security Headers**: CSRF protection and secure headers

## 📊 Data Visualization

### Chart Libraries Integration
- **ApexCharts**: Advanced interactive charts
- **Chart.js**: Lightweight chart library
- **D3.js**: Custom data visualizations
- **NgxGauge**: Gauge charts for metrics

### Chart Types
- Line charts for time series data
- Bar charts for comparative analysis
- Pie charts for distribution data
- Gauge charts for KPI metrics
- Venn diagrams for relationship visualization

## 🚀 Getting Started

### Prerequisites
- Node.js 18+ 
- npm 9+
- Angular CLI 19.2.5+

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd cardano-fe
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure environment**
   ```bash
   # Copy environment template
   cp src/environments/environment.ts.example src/environments/environment.ts
   
   # Update API URLs and configuration
   nano src/environments/environment.ts
   ```

4. **Start development server**
   ```bash
   npm start
   # or
   ng serve
   ```

5. **Access the application**
   ```
   http://localhost:4200
   ```

### Build for Production

```bash
# Build for production
npm run build

# Build with SSR
npm run build:ssr

# Serve SSR build
npm run serve:ssr
```

## 🔧 Development

### Available Scripts

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm run watch` - Build in watch mode
- `npm test` - Run unit tests
- `npm run serve:ssr` - Serve SSR build

### Code Style

The project follows Angular style guide with:
- **TypeScript strict mode** enabled
- **ESLint** for code quality
- **Prettier** for code formatting
- **SCSS** for styling with BEM methodology
- **Kebab-case** for file naming
- **PascalCase** for components and services

### Key Development Principles

1. **Component Composition**: Favor composition over inheritance
2. **Type Safety**: Strict TypeScript usage, avoid `any`
3. **Reactive Programming**: RxJS for data flow management
4. **Performance**: OnPush change detection, lazy loading
5. **Accessibility**: ARIA attributes and semantic HTML
6. **Responsive Design**: Mobile-first approach

## 🌐 API Integration

The application integrates with the Cardano governance backend API:

- **Base URL**: Configurable via environment
- **Authentication**: JWT token-based
- **Rate Limiting**: Built-in rate limiting support
- **Caching**: Intelligent caching with TTL
- **Error Handling**: Centralized error management

### API Endpoints

- `/api/account` - Account management
- `/api/drep` - DRep operations
- `/api/voting` - Voting functionality
- `/api/proposal` - Proposal management
- `/api/pool` - Stake pool data
- `/api/committee` - Committee operations
- `/api/treasury` - Treasury monitoring
- `/api/epoch` - Epoch information

## 📱 Responsive Design

- **Mobile-first** approach
- **Breakpoint system** using Angular CDK
- **Adaptive sidebar** (collapsible on mobile)
- **Touch-friendly** interface
- **Progressive Web App** ready

## 🔄 State Management

- **NgRx Store** for global state
- **NgRx Effects** for side effects
- **NgRx Entity** for normalized data
- **Reactive forms** for form state
- **Service-based** state for component-specific data

## 🧪 Testing

- **Jasmine** for unit testing
- **Karma** for test runner
- **Angular Testing Utilities** for component testing
- **Coverage reporting** with Istanbul

## 🚀 Performance Optimizations

- **Lazy loading** for feature modules
- **OnPush change detection** strategy
- **TrackBy functions** for ngFor optimization
- **Image optimization** with NgOptimizedImage
- **Bundle splitting** and code splitting
- **Service worker** for caching (PWA ready)

## 📦 Dependencies

### Core Dependencies
- Angular 19.2.3 ecosystem
- Nebular UI framework
- Angular Material
- RxJS 7.8.0
- TypeScript 5.5.4

### UI & Styling
- Bootstrap 5.3.2
- Tailwind CSS 3.4.17
- Eva Icons
- Angular CDK

### Charts & Visualization
- ApexCharts 4.5.0
- Chart.js 4.4.8
- D3.js 7.9.0
- NgxGauge

### Utilities
- NgxSpinner for loading states
- NgxCookieService for cookie management
- NgxMarkdown for markdown rendering
- TsCacheable for caching

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards
4. Write tests for new features
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the documentation
- Review the API documentation

---

**Built with ❤️ for the Cardano community**
