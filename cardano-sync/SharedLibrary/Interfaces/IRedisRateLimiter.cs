namespace SharedLibrary.Interfaces
{
    public interface IRedisRateLimiter
    {
        /// <summary>
        /// Kiểm tra và chờ cho đến khi có quota, trả về true nếu được phép gửi request.
        /// </summary>
        /// <param name="key">Tên nhóm rate limit (ví dụ: "koios")</param>
        /// <param name="maxRequests">Số request tối đa trong khoảng thời gian</param>
        /// <param name="perSeconds">Khoảng thời gian tính bằng giây</param>
        /// <returns></returns>
        Task<bool> WaitForQuotaAsync(string key, int maxRequests, int perSeconds);
    }
}