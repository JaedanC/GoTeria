using Godot;
using Godot.Collections;
using System;

/* This class is a generic ObjectPool. This class stores an Array of objects in a pool, and
when you request an instance with GetInstance(), an instance is created if the pool is empty,
OR an instance from the pool is popped and returned. In either case, the T.Reset() method is
called. Objects that wish to use this Class need to Implement to IResettable interface. They
also need to define a default constructor.
*/
public class ObjectPool<T> : Resource where T : Node, IResettable, new()
{
    private Array<T> pool;

    public ObjectPool(int numberOfInstances, params object[] memoryAllocationParameters)
    {
        pool = new Array<T>();
        for (int i = 0; i < numberOfInstances; i++)
        {
            T item = new T();
            item.AllocateMemory(memoryAllocationParameters);
            pool.Add(item);
        }
    }

    /* Unload the orphan T's when the game is closed. */
    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationPredelete)
        {
            foreach (T instance in pool)
            {
                instance.OnDeath();
                instance.QueueFree();
            }
        }
    }

    /* This method returns a new instance of T is the Pool is not empty. Otherwise it will
    return a reset old T from the pool. The either case, the T.Reset() method is called on T.
    The parameters to this function are passed in using an object[]. Note: You are still
    responsible for adding the instance to the tree, and allocating the memory this requires
    later on. */
    public T GetInstance(params object[] resetParameters)
    {
        T item;
        if (pool.Count > 0)
        {
            item = pool[0];
            pool.RemoveAt(0);
        }
        else
        {
            item = new T();
        }

        item.Initialise(resetParameters);
        return item;
    }

    /* This method returns an instance to the pool. It is not Reset yet. Note: You are still
    responsible for removing the instance from the tree! */
    public void Die(T instance)
    {
        pool.Add(instance);
        instance.OnDeath();
    }

}
