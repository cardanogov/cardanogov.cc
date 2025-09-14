# HÀNH TRÌNH TỐI ƯU HÓA: TỪ KOIOS API ĐẾN DATABASE SYNC

## 1. TỔNG QUAN HỆ THỐNG

Hệ thống Cardano Platform được thiết kế theo kiến trúc microservices với 6 microservices chính và 1 MainAPI gateway, sử dụng .NET 8, Entity Framework Core, PostgreSQL và Redis. Ban đầu, hệ thống phụ thuộc hoàn toàn vào KOIOS API để lấy dữ liệu từ Cardano blockchain, dẫn đến hiệu suất chậm và không ổn định. Quá trình tối ưu hóa đã chuyển đổi hoàn toàn sang mô hình database sync với hiệu suất cải thiện đáng kể.

### 1.1. Công Nghệ Sử Dụng
- **Backend**: .NET 8, Entity Framework Core, Quartz.NET
- **Database**: PostgreSQL với Npgsql provider
- **Cache**: Redis cho distributed caching
- **Architecture**: Clean Architecture, CQRS pattern, MediatR
- **External APIs**: KOIOS API, Adastat API

## 2. KIẾN TRÚC MICROSERVICES

### 2.1. CommitteeSyncService
**Chức năng**: Đồng bộ hóa thông tin Committee và Treasury
- **Jobs**: CommitteeSyncJob, CommitteeVotesSyncJob, TreasuryWithdrawalsSyncJob, TotalsSyncJob
- **Bảng dữ liệu**: md_committee_information, md_committee_votes, md_treasury_withdrawals, md_totals
- **Tần suất**: Chạy hàng ngày với cron expression, có thể khởi động ngay khi service start

### 2.2. DrepSyncService  
**Chức năng**: Đồng bộ hóa thông tin DReps (Delegated Representatives)
- **Jobs**: DrepListSyncJob, DrepInfoSyncJob, DrepMetadataSyncJob, DrepEpochSummarySyncJob, DrepVotingPowerHistorySyncJob, DrepDelegatorsSyncJob, DrepUpdatesSyncJob, AccountUpdatesSyncJob
- **Bảng dữ liệu**: md_dreps_list, md_dreps_info, md_dreps_metadata, md_dreps_epoch_summary, md_dreps_voting_power_history, md_dreps_delegators, md_dreps_updates
- **Đặc điểm**: Có 8 jobs chạy tuần tự với delay để tránh xung đột

### 2.3. EpochSyncService
**Chức năng**: Đồng bộ hóa thông tin Epochs và Protocol Parameters
- **Jobs**: EpochSyncJob, EpochProtocolParametersSyncJob, AdastatEpochSyncJob, AdastatDrepsSyncJob
- **Bảng dữ liệu**: md_epoch, md_epoch_protocol_parameters, md_epochs, md_dreps
- **Tích hợp**: Sử dụng cả KOIOS API và Adastat API

### 2.4. PoolSyncService
**Chức năng**: Đồng bộ hóa thông tin Stake Pools
- **Jobs**: PoolListSyncJob, PoolMetadataSyncJob, PoolStakeSnapshotSyncJob, PoolDelegatorsSyncJob, PoolVotingPowerHistorySyncJob, UtxoInfoSyncJob
- **Bảng dữ liệu**: md_pool_list, md_pool_metadata, md_pool_stake_snapshot, md_pool_delegators, md_pools_voting_power_history, md_utxo_info

### 2.5. ProposalSyncService
**Chức năng**: Đồng bộ hóa thông tin Proposals và Voting
- **Jobs**: ProposalSyncJob, ProposalVotesSyncJob, ProposalVotingSummaryJob
- **Bảng dữ liệu**: md_proposals_list, md_proposal_votes, md_proposal_voting_summary

### 2.6. VotingSyncService
**Chức năng**: Đồng bộ hóa thông tin Voting
- **Jobs**: VoteListSyncJob, VoterProposalListSyncJob
- **Bảng dữ liệu**: md_vote_list, md_voters_proposal_list

## 3. MAINAPI GATEWAY

MainAPI được thiết kế theo Clean Architecture với các layer:
- **Controllers**: Xử lý HTTP requests, sử dụng MediatR pattern
- **Application Layer**: Chứa Queries và Handlers (CQRS pattern)
- **Infrastructure Layer**: Chứa Services và Data Access
- **Core Layer**: Chứa Interfaces và Models

### 3.1. Các Controllers Chính
- **CommitteeController**: 3 endpoints cho committee info, votes, totals
- **DrepController**: 20+ endpoints cho DRep data, voting power, delegation
- **PoolController**: 8 endpoints cho pool info, metadata, statistics
- **ProposalController**: 10 endpoints cho proposal management
- **VotingController**: Endpoints cho voting data
- **PerformanceController**: Monitoring và optimization

## 4. QUÁ TRÌNH TỐI ƯU HÓA CHI TIẾT

### 4.1. Giai Đoạn 1: Vấn Đề Ban Đầu với KOIOS API

**Vấn đề chính:**
1. **Hiệu suất chậm**: Mỗi request phải gọi KOIOS API, thời gian response 10+ giây
2. **Rate limiting**: KOIOS API có giới hạn request, gây timeout và lỗi
3. **Không ổn định**: Phụ thuộc vào network và external service
4. **Không có cache**: Mỗi request đều phải fetch data mới
5. **Cold start**: Application khởi động chậm do phải khởi tạo connections

**Tác động:**
- User experience kém với loading time dài
- High server load do phải xử lý nhiều external API calls
- Khó scale khi số lượng users tăng
- Chi phí infrastructure cao

### 4.2. Giai Đoạn 2: Chuyển Đổi Sang Database Sync

**Kiến trúc mới:**
1. **DatabaseSyncService**: Service chuyên dụng để đọc từ backup databases
2. **Failover mechanism**: Tự động chuyển đổi giữa các database khi có lỗi
3. **Circuit breaker**: Ngăn chặn cascade failure
4. **Connection pooling**: Tối ưu database connections
5. **Bulk operations**: Xử lý dữ liệu theo batch để tăng hiệu suất

**DatabaseSyncService Features:**
```csharp
// Failover với circuit breaker
private readonly Dictionary<string, DateTime> _databaseFailureTimestamps = new();
private readonly Dictionary<string, int> _databaseFailureCounts = new();
private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(5);
private readonly int _circuitBreakerThreshold = 3;

// Global DB operation throttling
private static SemaphoreSlim? _globalDbSemaphore;
private readonly int _maxConcurrentDbOps = 8;
```

### 4.3. Giai Đoạn 3: Tối Ưu Hóa Cache System

**Vấn đề cache ban đầu:**
- .NET cache sử dụng 2MB cho cùng data mà Express.js chỉ dùng 10KB
- Serialization không hiệu quả với camel case conversion
- Không có expiry management

**Giải pháp tối ưu:**
```csharp
// Optimized JSON serialization
_jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null, // Keep original property names
    WriteIndented = false, // Minimize JSON size
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
};

// Express.js compatible cache structure
public class CacheWrapper
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("expiry")]
    public long Expiry { get; set; }
}
```

**Kết quả**: Giảm 200x kích thước cache (từ 2MB xuống 10KB)

### 4.4. Giai Đoạn 4: Performance Optimization

**Application Warmup Service:**
```csharp
public class ApplicationWarmupService : IHostedService
{
    // Tự động warmup khi application khởi động
    // Khởi tạo Redis connection, Database connection, Koios API connection
    // Preload dữ liệu thường xuyên sử dụng
}
```

**Performance Monitoring:**
- Real-time response time tracking
- P95, P99 metrics calculation
- Slow endpoint detection
- Automatic optimization triggers

**Database Query Optimization:**
```csharp
// Parallel database queries
var totalCountTask = query.CountAsync();
var currentEpochTask = _context.dreps_epoch_summary
    .AsNoTracking()
    .OrderByDescending(e => e.epoch_no)
    .FirstOrDefaultAsync();

await Task.WhenAll(totalCountTask, currentEpochTask);
```

### 4.5. Giai Đoạn 5: Service Layer Optimization

**DrepService Optimization:**
- Sử dụng `IDbContextFactory` để quản lý connections hiệu quả
- Parallel queries với `Task.WhenAll`
- In-memory joins thay vì multiple database calls
- Efficient JSON parsing với `JsonUtils`

**CommitteeService Optimization:**
- Load raw data trước, parse JSON trong memory
- Sử dụng `AsNoTracking()` cho read-only queries
- Optimized LINQ queries với proper indexing

**PoolService Optimization:**
- HttpClient injection thay vì tạo mới mỗi request
- Early return pattern để tránh unnecessary processing
- Proper resource disposal với `using` statements
- Parallel metadata fetching

## 5. KẾT QUẢ TỐI ƯU HÓA

### 5.1. Hiệu Suất
- **Response time**: Giảm từ 10+ giây xuống 2-3 giây (cải thiện 70-80%)
- **Cold start**: Giảm từ 10s xuống 2-3s
- **Cache efficiency**: Giảm 200x kích thước storage
- **Database load**: Giảm số lượng queries và tối ưu execution time

### 5.2. Độ Ổn Định
- **Uptime**: Tăng đáng kể nhờ failover mechanism
- **Error handling**: Comprehensive error handling với circuit breaker
- **Resource management**: Proper connection pooling và resource disposal

### 5.3. Khả Năng Mở Rộng
- **Horizontal scaling**: Microservices có thể scale độc lập
- **Load distribution**: Database sync giảm tải cho external APIs
- **Caching strategy**: Multi-level caching với Redis và memory cache

### 5.4. Chi Phí
- **Infrastructure cost**: Giảm do ít external API calls
- **Development cost**: Dễ maintain và debug hơn
- **Operational cost**: Ít monitoring và troubleshooting cần thiết

## 6. KIẾN TRÚC HIỆN TẠI

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │    │    MainAPI       │    │  MicroServices  │
│                 │◄──►│   (Gateway)      │◄──►│                 │
│                 │    │                  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                        │
                                ▼                        ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │   Redis Cache    │    │   PostgreSQL    │
                       │                  │    │   (Cardanogov.cc DB)    │
                       └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
                                               ┌─────────────────┐
                                               │  Backup DBs     │
                                               │  (Cardano Node) │
                                               └─────────────────┘
```

## 7. BÀI HỌC KINH NGHIỆM

1. **Database-first approach**: Luôn ưu tiên local database thay vì external APIs
2. **Microservices architecture**: Cho phép scale và maintain dễ dàng
3. **Caching strategy**: Multi-level caching là chìa khóa cho performance
4. **Error handling**: Circuit breaker và failover mechanism là bắt buộc
5. **Monitoring**: Real-time monitoring giúp phát hiện vấn đề sớm
6. **Resource management**: Proper connection pooling và resource disposal
7. **Code optimization**: Parallel processing và efficient algorithms

## 8. TƯƠNG LAI

Hệ thống đã được tối ưu hóa toàn diện từ external API dependency sang database sync architecture. Với kiến trúc hiện tại, hệ thống có thể:
- Handle high traffic với response time ổn định
- Scale horizontally khi cần thiết  
- Maintain high availability với failover mechanisms
- Provide real-time monitoring và optimization
- Support future enhancements một cách dễ dàng

Quá trình tối ưu hóa này đã chuyển đổi hoàn toàn hệ thống từ một ứng dụng phụ thuộc external APIs thành một platform mạnh mẽ, ổn định và có khả năng mở rộng cao.
