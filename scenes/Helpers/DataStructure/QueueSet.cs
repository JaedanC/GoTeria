using System.Collections.Generic;

public class QueueSet<T>
{
    private Queue<T> queue;
    private HashSet<object> set;
    public int Count { get { return queue.Count; }}


    public QueueSet()
    {
        queue = new Queue<T>();
        set = new HashSet<object>();
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
}
