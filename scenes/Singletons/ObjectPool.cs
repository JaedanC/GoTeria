using Godot;
using Godot.Collections;
using System;

public class ObjectPool<T> : Resource where T : IResettable, new()
{
    private Array<T> pool;

    public ObjectPool(int numberOfInstances)
    {
        pool = new Array<T>();
        for (int i = 0; i < numberOfInstances; i++)
        {
            GD.Print("Creating new instance");
            pool.Add(new T());
        }
    }

    public T GetInstance(params object[] resetParameters)
    {
        GD.Print("Getting instance from pool: ", pool.Count - 1);

        T item;
        if (pool.Count > 0)
        {
            item = pool[0];
            pool.RemoveAt(0);
        }
        else
        {
            GD.Print("Pool empty. Creating instance.");
            item = new T();
        }
        
        item.Reset(resetParameters);
        return item;
    }

    public void ReturnToPool(T instance)
    {
        GD.Print("Returning instance to pool: ", pool.Count + 1);

        pool.Add(instance);
    }
}
