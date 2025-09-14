# API Key Rate Limiting System

## Tổng quan

Hệ thống API Key Rate Limiting đã được triển khai hoàn chỉnh với các tính năng:

- ✅ Quản lý API keys với 3 tiers: Free, Premium, Enterprise
- ✅ Rate limiting theo minute, hour, day
- ✅ Middleware tự động kiểm tra và enforce rate limits
- ✅ Database storage với caching
- ✅ RESTful API để quản lý API keys
- ✅ Unit tests và Integration tests
- ✅ Documentation đầy đủ

## Cấu trúc Files

```
MainAPI/
├── Controllers/
│   └── ApiKeyController.cs              # API endpoints cho quản lý API keys
├── Middlewares/
│   └── ApiKeyRateLimitMiddleware.cs     # Middleware xử lý rate limiting
├── Infrastructure/
│   └── Services/DataAccess/
│       └── ApiKeyService.cs             # Service xử lý business logic
├── SharedLibrary/
│   ├── Models/
│   │   ├── ApiKey.cs                    # Model cho API key
│   │   └── RateLimitConfig.cs           # Cấu hình rate limits
│   └── Interfaces/
│       └── IApiKeyService.cs            # Interface cho API key service
└── Tests/
    └── MainAPI.Tests/
        ├── Services/
        │   └── ApiKeyServiceTests.cs     # Unit tests
        └── Integration/
            └── ApiKeyIntegrationTests.cs # Integration tests
```

## Cài đặt và Chạy

### 1. Database Setup

Chạy SQL script để tạo bảng:

```sql
-- Chạy file: MainAPI.Infrastructure/Data/Migrations/CreateApiKeysTable.sql
```

### 2. Configuration

Cập nhật `appsettings.json`:

```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "EnableApiKeyRateLimiting": true
  },
  "ApiKeySettings": {
    "DefaultKeyLength": 32,
    "KeyPrefix": "cardano_",
    "EnableKeyRotation": true,
    "KeyRotationDays": 90
  }
}
```

### 3. Build và Run

```bash
cd MainAPI
dotnet build
dotnet run
```

## Sử dụng API

### 1. Tạo API Key

```bash
curl -X POST "https://localhost:7001/api/apikey/create" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Application",
    "description": "API key for my application",
    "type": 0,
    "createdBy": "user@example.com"
  }'
```

### 2. Sử dụng API Key

#### Header Method (Recommended)
```bash
curl -H "X-API-Key: your_api_key_here" \
  "https://localhost:7001/api/drep/total_drep"
```

#### Query String Method
```bash
curl "https://localhost:7001/api/drep/total_drep?api_key=your_api_key_here"
```

#### Authorization Header Method
```bash
curl -H "Authorization: Bearer your_api_key_here" \
  "https://localhost:7001/api/drep/total_drep"
```

### 3. Kiểm tra Rate Limit

```bash
curl "https://localhost:7001/api/apikey/rate-limit/your_api_key_here"
```

## Rate Limits

| Plan | Per Minute | Per Hour | Per Day | Burst |
|------|------------|----------|---------|-------|
| Free | 60 | 1,000 | 10,000 | 10 |
| Premium | 300 | 10,000 | 100,000 | 50 |
| Enterprise | 1,000 | 50,000 | 1,000,000 | 200 |

## Response Headers

Khi gọi API, bạn sẽ nhận được:

```
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9995
X-RateLimit-Reset: 2024-01-02T00:00:00Z
X-RateLimit-Type: Free
```

## Error Handling

### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "API key is required",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### 429 Too Many Requests
```json
{
  "error": "Rate Limit Exceeded",
  "message": "Rate limit exceeded for Free plan. Limit: 10000 requests per day",
  "reset_time": "2024-01-02T00:00:00Z",
  "remaining_requests": 0,
  "key_type": "Free"
}
```

## Test API Keys

### Free Test Key
```
free_test_key_123456789
```

### Premium Test Key
```
premium_test_key_987654321
```

### Enterprise Test Key
```
enterprise_test_key_456789123
```

## Monitoring và Logging

### 1. Health Checks
```bash
curl "https://localhost:7001/health"
```

### 2. API Key Usage Logs
Tất cả API key usage được log trong:
- Console logs
- File logs: `Logs/log-YYYY-MM-DD.txt`

### 3. Database Monitoring
```sql
-- Kiểm tra API key usage
SELECT 
    ak.Name,
    ak.Type,
    ak.TotalRequests,
    ak.DailyRequests,
    ak.LastUsedAt
FROM ApiKeys ak
WHERE ak.IsActive = 1
ORDER BY ak.TotalRequests DESC;
```

## Security Best Practices

1. **API Key Storage**
   - Không commit API keys vào source code
   - Sử dụng environment variables hoặc secure vault
   - Rotate keys định kỳ

2. **Rate Limiting**
   - Monitor usage patterns
   - Set appropriate limits cho từng tier
   - Implement alerting cho unusual usage

3. **Access Control**
   - Validate API keys trên mọi request
   - Log tất cả access attempts
   - Implement IP whitelisting nếu cần

## Performance Optimization

1. **Caching**
   - API key validation được cache trong memory
   - Rate limit counters được cache
   - Cache expiration: 5 minutes

2. **Database Optimization**
   - Indexes trên Key, IsActive, Type
   - Connection pooling
   - Query optimization với Dapper

3. **Middleware Optimization**
   - Early return cho invalid requests
   - Minimal database calls
   - Efficient header parsing

## Troubleshooting

### 1. API Key không hoạt động
```bash
# Kiểm tra API key
curl "https://localhost:7001/api/apikey/validate/your_key"

# Kiểm tra logs
tail -f Logs/log-$(date +%Y-%m-%d).txt
```

### 2. Rate limit errors
```bash
# Kiểm tra rate limit info
curl "https://localhost:7001/api/apikey/rate-limit/your_key"

# Reset daily count (nếu cần)
# Chạy SQL: UPDATE ApiKeys SET DailyRequests = 0 WHERE [Key] = 'your_key'
```

### 3. Database connection issues
```bash
# Kiểm tra health check
curl "https://localhost:7001/health"

# Kiểm tra connection string trong appsettings.json
```

## Development

### 1. Running Tests
```bash
# Unit tests
dotnet test Tests/MainAPI.Tests/Services/ApiKeyServiceTests.cs

# Integration tests
dotnet test Tests/MainAPI.Tests/Integration/ApiKeyIntegrationTests.cs
```

### 2. Adding New Rate Limit Tiers
1. Cập nhật `ApiKeyType` enum
2. Thêm settings trong `RateLimitConfig`
3. Cập nhật tests
4. Update documentation

### 3. Customizing Rate Limits
```csharp
// Trong RateLimitConfig.cs
public static readonly Dictionary<ApiKeyType, RateLimitSettings> Settings = new()
{
    [ApiKeyType.Free] = new RateLimitSettings
    {
        RequestsPerMinute = 60,
        RequestsPerHour = 1000,
        RequestsPerDay = 10000,
        BurstLimit = 10
    },
    // Add new tiers here
};
```

## Support

Nếu gặp vấn đề, vui lòng:

1. Kiểm tra logs trong `Logs/` directory
2. Chạy health checks
3. Verify database connection
4. Check API key validity
5. Review rate limit settings

## Changelog

### v1.0.0 (Current)
- ✅ Initial implementation
- ✅ Free, Premium, Enterprise tiers
- ✅ Rate limiting middleware
- ✅ Database storage
- ✅ RESTful API
- ✅ Unit and integration tests
- ✅ Documentation

### Future Enhancements
- 🔄 API key analytics dashboard
- 🔄 Advanced rate limiting (per endpoint)
- 🔄 Webhook notifications
- 🔄 API key usage reports
- 🔄 Multi-tenant support 