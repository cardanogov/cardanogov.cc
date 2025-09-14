# Committee Sync Service - Database Sync Version

## Tổng quan

CommitteeSyncService đã được cập nhật để đọc dữ liệu từ các database backup thay vì gọi API Koios. Hệ thống này cung cấp cơ chế failover tự động giữa nhiều database backup để đảm bảo tính sẵn sàng cao.

**Lưu ý quan trọng**: Hệ thống sử dụng các function PostgreSQL với tên chính xác:
- `committee_info()` - không có prefix "get_"
- `committee_votes(_cc_hot_id)` - không có prefix "get_"
- `treasury_withdrawals()` - không có prefix "get_"
- `totals(_epoch_no)` - không có prefix "get_", yêu cầu tham số epoch

## Cấu hình

### Connection Strings

Hệ thống sử dụng 3 connection strings:

1. **DefaultConnection**: Database local để lưu trữ dữ liệu
   ```
   Host=localhost;Port=5432;Database=cardano;Username=postgres;Password=Cardan0@098;Include Error Detail=true;
   ```

2. **BackupDatabase1**: Database backup chính
   ```
   Host=host;Port=5432;Database=cexplorer;Username=user;Password=pwd;Include Error Detail=true;
   ```

3. **BackupDatabase2**: Database backup phụ
   ```
   Host=host;Port=5432;Database=cexplorer;Username=user;Password=pwd;Include Error Detail=true;
   ```

### Cấu hình DatabaseSync

```json
"DatabaseSync": {
  "Schema": "grest",
  "MaxRetries": 3,
  "RetryDelayMs": 1000,
  "ConnectionTimeoutSeconds": 30,
  "CommandTimeoutSeconds": 60,
  "EnableFailover": true,
  "FailoverOrder": ["BackupDatabase1"]
}
```

## Các Job

### 1. CommitteeSyncJob
- **Mục đích**: Đồng bộ thông tin committee từ database backup
- **Dữ liệu**: Function `grest.committee_info()`
- **Thời gian chạy**: 00:15 UTC (có thể cấu hình)

### 2. CommitteeVotesSyncJob
- **Mục đích**: Đồng bộ votes của committee từ database backup
- **Dữ liệu**: Function `grest.committee_votes(cc_hot_id)`
- **Thời gian chạy**: 00:10 UTC (có thể cấu hình)
- **Phụ thuộc**: Cần chạy sau CommitteeSyncJob

### 3. TreasuryWithdrawalsSyncJob
- **Mục đích**: Đồng bộ dữ liệu treasury withdrawals từ database backup
- **Dữ liệu**: Function `grest.treasury_withdrawals()`
- **Thời gian chạy**: 00:20 UTC (có thể cấu hình)

### 4. TotalsSyncJob
- **Mục đích**: Đồng bộ dữ liệu tổng hợp từ database backup
- **Dữ liệu**: Function `grest.totals(_epoch_no)` (hiện tại sử dụng epoch=1 để test)
- **Thời gian chạy**: 00:25 UTC (có thể cấu hình)
- **Lưu ý**: Function `totals(_epoch_no)` yêu cầu tham số epoch. Cần xác định cách lấy tất cả totals nếu muốn đồng bộ toàn bộ dữ liệu.

## Cơ chế Failover

### Hoạt động
1. Hệ thống sẽ thử kết nối đến database đầu tiên trong danh sách `FailoverOrder`
2. Nếu database đầu tiên không khả dụng, tự động chuyển sang database tiếp theo
3. Quá trình lặp lại cho đến khi tìm được database khả dụng
4. Nếu tất cả database đều không khả dụng, job sẽ thất bại

### Cấu hình Failover
- **EnableFailover**: Bật/tắt cơ chế failover
- **MaxRetries**: Số lần thử lại tối đa cho mỗi database
- **RetryDelayMs**: Thời gian chờ giữa các lần thử lại
- **FailoverOrder**: Thứ tự ưu tiên các database

## Monitoring và Health Check

### DatabaseHealthCheckService
Service này cung cấp thông tin về sức khỏe của các database backup:

- **Kết nối**: Kiểm tra khả năng kết nối
- **Response Time**: Thời gian phản hồi
- **Schema Access**: Quyền truy cập schema
- **Function Availability**: Sự có sẵn của các function cần thiết

### Logging
Hệ thống ghi log chi tiết cho:
- Trạng thái kết nối database
- Thời gian thực thi queries
- Lỗi và exceptions
- Thống kê đồng bộ dữ liệu

## Cách sử dụng

### 1. Khởi động Service
```bash
cd Microservices/CommitteeSyncService
dotnet run
```

### 2. Kiểm tra Health Status
Service sẽ tự động kiểm tra sức khỏe của các database backup khi khởi động và trước mỗi job.

### 3. Monitoring
Theo dõi logs để kiểm tra:
- Trạng thái kết nối database
- Hiệu suất đồng bộ dữ liệu
- Lỗi và cảnh báo

## Troubleshooting

### Lỗi kết nối database
1. Kiểm tra network connectivity
2. Xác minh thông tin đăng nhập
3. Kiểm tra firewall settings
4. Xác minh database đang hoạt động

### Lỗi schema/function
1. Kiểm tra quyền truy cập schema `grest`
2. Xác minh các function cần thiết tồn tại
3. Kiểm tra signature của function có khớp với model

### Performance Issues
1. Kiểm tra response time của database
2. Tối ưu hóa queries
3. Kiểm tra network latency
4. Xem xét tăng timeout values

## Lợi ích

1. **Tính sẵn sàng cao**: Failover tự động giữa nhiều database
2. **Hiệu suất tốt hơn**: Truy cập trực tiếp database thay vì API
3. **Độ tin cậy**: Không phụ thuộc vào API bên ngoài
4. **Monitoring**: Theo dõi sức khỏe database real-time
5. **Scalability**: Dễ dàng thêm database backup mới

## Tương lai

- Thêm metrics và alerting
- Implement connection pooling
- Thêm caching layer
- Support cho nhiều loại database khác nhau
