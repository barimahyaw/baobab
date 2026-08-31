namespace Baobab.SharedKernel.Application.Abstractions.Data;

public interface ICacheManager
{
    bool Cache<T>(string key, T t, int seconds);
    T GetCache<T>(string key);
    (bool exist, bool success) Remove(string key);
}
