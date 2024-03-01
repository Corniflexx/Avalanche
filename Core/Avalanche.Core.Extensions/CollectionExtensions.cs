namespace Avalanche.Core.Extensions;

public static class CollectionExtensions
{
    public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) where TKey : notnull =>
        self.TryRemove(key, out _);

    public static bool TryGetValue<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) where TKey : notnull =>
        self.TryGetValue(key, out _);
}
