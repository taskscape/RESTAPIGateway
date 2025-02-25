using Microsoft.Extensions.Caching.Memory;

namespace GenericTableAPI.Extensions
{
    public static class MemoryCacheExtension
    {
        public static int CacheDurationSeconds { get; set; } = 0;

        public static void SetCache(this IMemoryCache memoryCache, string cache, object? obj)
        {
            if (CacheDurationSeconds <= 0)
                return;
            memoryCache.Set(cache, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(CacheDurationSeconds)));
        }

        public static bool TryGetCache(this IMemoryCache memoryCache, string cache, out object? obj)
        {
            obj = null;
            if (CacheDurationSeconds <= 0)
                return false;
            return memoryCache.TryGetValue(cache, out obj);
        }
    }
}
