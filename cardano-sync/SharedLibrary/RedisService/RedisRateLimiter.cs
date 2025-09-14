using SharedLibrary.Interfaces;
using StackExchange.Redis;

namespace SharedLibrary.RedisService
{
    public class RedisRateLimiter : IRedisRateLimiter
    {
        private readonly IDatabase _db;
        public RedisRateLimiter(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<bool> WaitForQuotaAsync(string key, int maxRequests, int perSeconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowStart = now - perSeconds;
            var redisKey = $"ratelimit:{key}";

            // Xóa các request cũ ngoài window
            await _db.SortedSetRemoveRangeByScoreAsync(redisKey, 0, windowStart);

            // Đếm số request trong window
            var count = await _db.SortedSetLengthAsync(redisKey);

            if (count < maxRequests)
            {
                // Thêm request mới
                await _db.SortedSetAddAsync(redisKey, Guid.NewGuid().ToString(), now);
                // Đặt TTL
                await _db.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(perSeconds));
                return true;
            }

            // Với rate limit cao, giảm thời gian chờ và retry nhanh hơn
            var maxWaitTime = perSeconds * 1000 / maxRequests; // Tính thời gian chờ dựa trên rate
            var waitTime = Math.Min(maxWaitTime, 100); // Tối đa 100ms, tối thiểu theo rate

            // Nếu vượt quota, chờ đến khi có slot với timeout
            var retries = 0;
            var maxRetries = 50; // Tối đa 50 lần retry (5 giây với 100ms mỗi lần)

            while (count >= maxRequests && retries < maxRetries)
            {
                await Task.Delay((int)waitTime);
                await _db.SortedSetRemoveRangeByScoreAsync(redisKey, 0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - perSeconds);
                count = await _db.SortedSetLengthAsync(redisKey);
                retries++;
            }

            if (retries >= maxRetries)
            {
                return false; // Timeout, không thể lấy quota
            }

            await _db.SortedSetAddAsync(redisKey, Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await _db.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(perSeconds));
            return true;
        }
    }
}