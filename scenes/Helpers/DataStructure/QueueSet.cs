using System.Collections.Generic;
using System.Linq;

public class QueueSet<T>
{
    private Queue<T> queue;
    private HashSet<T> set;
    public int Count { get { return queue.Count; }}


    public QueueSet()
    {
        queue = new Queue<T>();
        set = new HashSet<T>();
    }

    public void Enqueue(T item)
    {
        if (set.Contains(item)) return;
        set.Add(item);
        queue.Enqueue(item);
    }
    
    public T Dequeue()
    {
        T item = queue.Dequeue();
        set.Remove(item);
        return item;
    }

    public List<T> ToList()
    {
        return set.ToList();
    }
}
