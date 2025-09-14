# Cardano Main API v1.0

Main API Gateway cho hệ thống Cardano blockchain data synchronization với đầy đủ tính năng API versioning, caching, rate limiting và health checks.

## 🚀 Tính năng chính

### ✅ API Versioning
- Hỗ trợ versioning qua URL: `/api/v1.0/`
- Hỗ trợ versioning qua header: `X-API-Version: 1.0`
- Hỗ trợ versioning qua media type
- Swagger documentation cho từng version

### ✅ Caching & Performance
- Redis caching với configurable expiration
- Rate limiting để bảo vệ API
- HTTP client với Polly retry và circuit breaker
- Response compression

### ✅ Monitoring & Health Checks
- Health checks cho database, Redis
- Readiness và liveness probes cho Kubernetes
- Structured logging với Serilog
- Request/response logging

### ✅ Security & CORS
- CORS configuration cho frontend
- Rate limiting per IP
- Request validation
- Error handling standardization

## 📋 API Endpoints

### Health Checks
```
GET /health                    # Basic health check
GET /health/detailed          # Detailed health with all checks
GET /health/ready             # Kubernetes readiness probe
GET /health/live              # Kubernetes liveness probe
GET /health/system            # System information
GET /health/cache             # Cache status
GET /health/config            # API configuration
```

### Information API (v1.0)
```
GET /api/v1.0/info            # API information
GET /api/v1.0/info/time       # Time utilities
GET /api/v1.0/info/cache      # Cache information
GET /api/v1.0/info/config     # Configuration info
GET /api/v1.0/info/pagination-test  # Pagination test
GET /api/v1.0/info/error-test # Error handling test
GET /api/v1.0/info/request-info # Request debugging
```

### Health Controller (v1.0)
```
GET /api/v1.0/health          # Health status
GET /api/v1.0/health/detailed # Detailed health
GET /api/v1.0/health/ready    # Readiness status
GET /api/v1.0/health/live     # Liveness status
GET /api/v1.0/health/system   # System info
GET /api/v1.0/health/cache    # Cache status
GET /api/v1.0/health/config   # API config
```

## 🛠️ Cài đặt và chạy

### Prerequisites
- .NET 8 SDK
- PostgreSQL 12+
- Redis 6+

### Configuration
Cập nhật `appsettings.json`:

```json
{
  "ApiSettings": {
    "Title": "Cardano Main API",
    "Version": "v1.0",
    "Description": "Main API for Cardano blockchain data synchronization"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cardano;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:4200"]
  }
}
```

### Chạy ứng dụng
```bash
cd MainAPI
dotnet restore
dotnet run
```

### Swagger Documentation
Truy cập: `http://localhost:5000` hoặc `https://localhost:5001`

## 📊 Response Format

### Standard Response
```json
{
  "success": true,
  "message": "Success",
  "data": { ... },
  "errors": null,
  "timestamp": "2024-01-01T00:00:00Z",
  "version": "v1.0",
  "requestId": "trace-id-123"
}
```

### Paginated Response
```json
{
  "success": true,
  "message": "Success",
  "data": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 100,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "timestamp": "2024-01-01T00:00:00Z",
  "version": "v1.0",
  "requestId": "trace-id-123"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": ["Field 'name' is required"],
  "timestamp": "2024-01-01T00:00:00Z",
  "version": "v1.0",
  "requestId": "trace-id-123"
}
```

## 🔧 Development

### Thêm Controller mới
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class NewController : BaseController
{
    public NewController(ILogger<NewController> logger) : base(logger)
    {
    }

    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Success(new { message = "Hello World" });
    }
}
```

### Sử dụng TimeUtils
```csharp
using MainAPI.Utils;

// Tính thời gian còn lại đến cuối ngày
var secondsUntilEndOfDay = TimeUtils.GetSecondsUntilEndOfDay();

// Format TimeSpan
var formattedTime = TimeUtils.FormatTimeSpan(TimeSpan.FromHours(2));
```

### Sử dụng Cache
```csharp
// Inject IDistributedCache
private readonly IDistributedCache _cache;

// Set cache
await _cache.SetStringAsync("key", "value", new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
});

// Get cache
var value = await _cache.GetStringAsync("key");
```

## 🚨 Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Kiểm tra connection string trong `appsettings.json`
   - Đảm bảo PostgreSQL đang chạy
   - Kiểm tra firewall settings

2. **Redis Connection Failed**
   - Kiểm tra Redis server
   - Kiểm tra connection string
   - Đảm bảo Redis port (6379) không bị block

3. **CORS Issues**
   - Kiểm tra CORS settings trong `appsettings.json`
   - Đảm bảo origin được allow trong `CorsSettings:AllowedOrigins`

4. **Rate Limiting**
   - Kiểm tra rate limit settings
   - Tăng `PermitLimit` nếu cần
   - Kiểm tra logs để debug

### Health Check Endpoints

- `/health` - Basic health check
- `/health/detailed` - Detailed health with all components
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe

### Logs
Logs được lưu trong:
- Console (Development)
- File: `Logs/log-YYYY-MM-DD.txt` (Production)

## 📈 Performance Optimization

### Caching Strategy
- Sử dụng Redis cho distributed caching
- Configurable cache expiration
- Cache key naming convention: `CardanoMainAPI:{service}:{key}`

### Rate Limiting
- IP-based rate limiting
- Configurable limits per window
- Graceful degradation

### Database Optimization
- Connection pooling
- Query optimization
- Use `AsNoTracking()` for read-only queries

## 🔒 Security

### Rate Limiting
- IP-based rate limiting
- Configurable limits
- Automatic blocking of abusive IPs

### CORS
- Whitelist specific origins
- Configurable methods and headers
- Secure by default

### Error Handling
- No sensitive information in error responses
- Structured error logging
- Request ID tracking

## 📝 API Versioning

### URL Versioning
```
GET /api/v1.0/health
GET /api/v1.1/health  # Future version
```

### Header Versioning
```
X-API-Version: 1.0
```

### Media Type Versioning
```
Accept: application/json; X-API-Version=1.0
```

## 🤝 Contributing

1. Fork repository
2. Tạo feature branch
3. Implement changes
4. Add tests
5. Update documentation
6. Create Pull Request

## 📄 License

This project is licensed under the MIT License. 