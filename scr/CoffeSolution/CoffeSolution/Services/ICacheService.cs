namespace CoffeSolution.Services;

public interface ICacheService
{
    T Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expirationTime = null);
    void Remove(string key);
    void Clear();
}
