using Microsoft.Extensions.Caching.Memory;

namespace CoffeSolution.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T Get<T>(string key)
    {
        return _memoryCache.TryGetValue(key, out T value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions();
        
        if (expirationTime.HasValue)
        {
            cacheEntryOptions.SetAbsoluteExpiration(expirationTime.Value);
        }
        else
        {
            // Mặc định cache 1 giờ nếu không chỉ định expiration
            cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromHours(1));
        }

        _memoryCache.Set(key, value, cacheEntryOptions);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
    
    public void Clear()
    {
        // TODO: Implement custom logic để clear cache group
    }
}
