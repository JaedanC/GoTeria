using Godot;
using Godot.Collections;


public class LazyVolatileDictionary<TKey, TValue> where TValue : class
{
    private Mutex volatileMutex;
    private Mutex lazyMutex;
    private bool volatileWasLocked;
    private bool lazyWasLocked;
    private Dictionary<TKey, TValue> volatileDictionary;
    private Dictionary<TKey, TValue> lazyDictionary;

    public LazyVolatileDictionary()
    {
        volatileMutex = new Mutex();
        lazyMutex = new Mutex();
        volatileDictionary = new Dictionary<TKey, TValue>();
    }

    public TValue Get(TKey key)
    {
        TValue found = null;
        volatileMutex.Lock();
        lazyMutex.Lock();
        if (volatileDictionary.ContainsKey(key))
        {
            found = volatileDictionary[key];
        }
        else if (lazyDictionary.ContainsKey(key))
        {
            found = lazyDictionary[key];
        }
        volatileMutex.Unlock();
        lazyMutex.Lock();
        return found;
    }

    public void LockVolatile()
    {
        volatileWasLocked = true;
        volatileMutex.Lock();
    }

    public void UnlockVolatile()
    {
        volatileWasLocked = false;
        volatileMutex.Unlock();
    }

    public Dictionary<TKey, TValue> GetVolatileDictionary()
    {
        Developer.AssertTrue(volatileWasLocked, "Make sure you lock and unlock the volatile dictionary if you want to do work with it");
        return volatileDictionary;
    }

    public void LockLazy()
    {
        lazyWasLocked = true;
        lazyMutex.Lock();
    }

    public void UnlockLazy()
    {
        lazyWasLocked = false;
        lazyMutex.Unlock();
    }

    public Dictionary<TKey, TValue> GetLazyDictionary()
    {
        Developer.AssertTrue(lazyWasLocked, "Make sure you lock and unlock the lazy dictionary if you want to do work with it");
        return lazyDictionary;
    }

    public void VolatileAdd(TKey key, TValue value)
    {
        volatileMutex.Lock();
        volatileDictionary[key] = value;
        volatileMutex.Unlock();
    }

    public void VolatileAdd(Dictionary<TKey, TValue> items)
    {
        volatileMutex.Lock();
        foreach (TKey key in items.Keys)
        {
            volatileDictionary[key] = items[key];
        }
        volatileMutex.Unlock();
    }

    public bool VolatileRemove(TKey key)
    {
        volatileMutex.Lock();
        bool removed = volatileDictionary.Remove(key);
        volatileMutex.Unlock();
        return removed;
    }

    public void VolatileRemove(Array<TKey> keys)
    {
        volatileMutex.Lock();
        foreach (TKey key in keys)
        {
            volatileDictionary.Remove(key);
        }
        volatileMutex.Unlock();
    }

    public Dictionary<TKey, TValue> VolatileKeepOnly(Array<TKey> keepKeys)
    {
        volatileMutex.Lock();
        Dictionary<TKey, TValue> toKeep = new Dictionary<TKey, TValue>();

        // Mark the keys we find to keep
        foreach (TKey keepKey in keepKeys)
        {
            if (volatileDictionary.ContainsKey(keepKey))
            {
                toKeep[keepKey] = volatileDictionary[keepKey];
                volatileDictionary.Remove(keepKey);
            }
        }

        // Swap the two dictionaries. Return the old one. These were deleted.
        Dictionary<TKey, TValue> weRemoved = volatileDictionary;
        volatileDictionary = toKeep;
        volatileMutex.Unlock();
        return weRemoved;
    }

    public void LazyAdd(TKey key, TValue value)
    {
        lazyMutex.Lock();
        lazyDictionary[key] = value;
        lazyMutex.Unlock();
    }

    public void LazyAdd(Dictionary<TKey, TValue> items)
    {
        lazyMutex.Lock();
        foreach (TKey key in items.Keys)
        {
            lazyDictionary[key] = items[key];
        }
        lazyMutex.Unlock();
    }

    public bool LazyRemove(TKey key)
    {
        lazyMutex.Lock();
        bool removed = lazyDictionary.Remove(key);
        lazyMutex.Unlock();
        return removed;
    }

    public void LazyRemove(Array<TKey> keys)
    {
        lazyMutex.Lock();
        foreach (TKey key in keys)
        {
            lazyDictionary.Remove(key);
        }
        lazyMutex.Unlock();
    }
}
