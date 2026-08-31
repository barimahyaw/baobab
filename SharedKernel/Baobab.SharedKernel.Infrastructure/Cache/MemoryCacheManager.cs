using Baobab.SharedKernel.Application.Abstractions.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Baobab.SharedKernel.Infrastructure.Cache;

public sealed class MemoryCacheManager(IMemoryCache memoryCache, ILogger<MemoryCacheManager> logger) : ICacheManager
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<MemoryCacheManager> _logger = logger;

    public bool Cache<T>(string key, T t, int seconds)
    {
        try
        {
            _memoryCache.Set(key, t, TimeSpan.FromSeconds(seconds));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }

    public T GetCache<T>(string key)
    {
        _memoryCache.TryGetValue(key, out T? val);

        if (val is null) return default!;

        return val;
    }

    public (bool exist, bool success) Remove(string key)
    {
        if (!_memoryCache.TryGetValue(key, out _)) return (false, false);

        _memoryCache.Remove(key);

        return (true, true);
    }
}
