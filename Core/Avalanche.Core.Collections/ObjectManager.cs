namespace Avalanche.Core.Collections;

interface IObjectManager<T>
{
    T Rent();
    void Return(T obj);
}

public class ObjectManager<T> : IObjectManager<T>
{
    protected ConcurrentStack<T> Stack { get; set; } = new();
    protected Func<T> Allocator { get; set; }

    public ObjectManager(Func<T> allocator) => Allocator = allocator;

    public virtual T Rent()
    {
        if (!Stack.TryPop(out var obj))
            obj = Allocator();
        return obj;
    }

    public virtual void Return(T obj) => Stack.Push(obj);

    public void Clear() => Stack.Clear();

    public int Count => Stack.Count;

    public override string ToString() => $"ObjectManager<{typeof(T)}>: Count: {Count}";
}
