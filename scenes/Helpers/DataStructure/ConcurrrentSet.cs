using System.Collections.Concurrent;

public class ConcurrentSet<T>
{
    private readonly ConcurrentDictionary<T, byte> backendDict;

    public ConcurrentSet()
    {
        backendDict = new ConcurrentDictionary<T, byte>();
    }

    public void Add(T item)
    {
        backendDict[item] = 1;
    }

    public bool Contains(T item)
    {
        return backendDict.ContainsKey(item);
    }

    public bool Remove(T item)
    {
        return backendDict.TryRemove(item, out byte _);
    }
}
