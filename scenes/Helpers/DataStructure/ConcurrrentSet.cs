using System.Collections.Concurrent;

public class ConcurrentSet<T>
{
    private ConcurrentDictionary<T, byte> backendDict;

    public ConcurrentSet()
    {
        this.backendDict = new ConcurrentDictionary<T, byte>();
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
        byte removed;
        return backendDict.TryRemove(item, out removed);
    }
}
