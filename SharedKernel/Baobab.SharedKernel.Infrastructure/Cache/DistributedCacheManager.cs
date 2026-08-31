using Baobab.SharedKernel.Application.Abstractions.Data;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Baobab.SharedKernel.Infrastructure.Cache;

public sealed class DistributedCacheManager(IDatabase cache) : ICacheManager
{
    private readonly IDatabase _cache = cache;

    public bool Cache<T>(string key, T value, int seconds)
    {
        var isSet = _cache.StringSet(key, JsonConvert.SerializeObject(value), TimeSpan.FromSeconds(seconds));
        return isSet;
    }

    public T GetCache<T>(string key)
    {
        var value = _cache.StringGet(key);

        if (value.IsNullOrEmpty) return default!;

        var result = JsonConvert.DeserializeObject<T>(value!);

        if (result is null) return default!;

        return result;
    }

    public (bool exist, bool success) Remove(string key)
    {
        if (_cache.KeyExists(key))
        {
            var removed = _cache.KeyDelete(key);
            return (true, removed);
        }
        return (false, false);
    }
}
