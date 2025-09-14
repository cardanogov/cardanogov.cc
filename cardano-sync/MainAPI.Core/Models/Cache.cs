namespace MainAPI.Core.Models
{
    /// <summary>
    /// Cache wrapper class similar to Express.js format
    /// </summary>
    public class CacheWrapper<T>
    {
        public T? Data { get; set; }
        public long Expiry { get; set; }
    }

    public class CacheResult<T>
    {
        public bool IsHit { get; set; }
        public T? Value { get; set; }
    }
}
