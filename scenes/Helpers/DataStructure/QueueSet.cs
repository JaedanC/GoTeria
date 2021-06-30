using System.Collections.Generic;
using System.Linq;

public class QueueSet<T>
{
    private readonly Queue<T> queue;
    private readonly HashSet<T> set;
    public int Count => queue.Count;


    public QueueSet(params T[] initialValues)
    {
        queue = new Queue<T>();
        set = new HashSet<T>();

        for (int i = 0; i < initialValues.Count<T>(); i++)
        {
            Enqueue(initialValues[i]);
        }
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
