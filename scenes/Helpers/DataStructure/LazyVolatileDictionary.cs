using System.Collections.Concurrent;
using System.Collections.Generic;


public class LazyVolatileDictionary<TKey, TValue> where TValue : class
{
    private ConcurrentDictionary<TKey, TValue> volatileDictionary;
    private ConcurrentDictionary<TKey, TValue> lazyDictionary;
    private SafeMutex mutex;
    public bool IsLocked { get { return mutex.IsLocked; } }

    public LazyVolatileDictionary()
    {
        volatileDictionary = new ConcurrentDictionary<TKey, TValue>();
        lazyDictionary = new ConcurrentDictionary<TKey, TValue>();
        mutex = new SafeMutex();
    }

    public bool ContainsKey(TKey key)
    {
        Developer.AssertTrue(IsLocked, "Must use ContainsKey() while locked.");
        return volatileDictionary.ContainsKey(key) || lazyDictionary.ContainsKey(key);
    }

    public TValue Get(TKey key)
    {
        Developer.AssertTrue(IsLocked, "Must use Get() while locked.");

        TValue found = null;
        if (volatileDictionary.ContainsKey(key))
        {
            found = volatileDictionary[key];
        }
        else if (lazyDictionary.ContainsKey(key))
        {
            found = lazyDictionary[key];
        }
        return found;
    }

    public void Lock()
    {
        mutex.Lock();
    }

    public void Unlock()
    {
        mutex.Unlock();
    }

    public void Add(TKey key, TValue value, bool intoLazy)
    {
        if (!intoLazy)
        {
            volatileDictionary[key] = value;
        }
        else
        {
            lazyDictionary[key] = value;
        }
    }

    public void Add(IDictionary<TKey, TValue> items)
    {
        foreach (TKey key in items.Keys)
        {
            volatileDictionary[key] = items[key];
        }
    }

    public bool VolatileRemove(TKey key)
    {
        TValue temp;
        return volatileDictionary.TryRemove(key, out temp);;
    }

    public void VolatileRemove(IEnumerable<TKey> keys)
    {
        foreach (TKey key in keys)
        {
            TValue temp;
            volatileDictionary.TryRemove(key, out temp);
        }
    }

    public bool VolatileContainsKey(TKey key)
    {
        return volatileDictionary.ContainsKey(key);
    }

    public IDictionary<TKey, TValue> VolatileKeepOnly(IEnumerable<TKey> keepKeys)
    {
        Developer.AssertTrue(IsLocked, "Must use VolatileKeepOnly() while locked.");

        // Create a temporary dictionary to store elements that need to stay
        ConcurrentDictionary<TKey, TValue> toKeep = new ConcurrentDictionary<TKey, TValue>();

        // Mark the keys we find to keep
        foreach (TKey keepKey in keepKeys)
        {
            if (volatileDictionary.ContainsKey(keepKey))
            {
                toKeep[keepKey] = volatileDictionary[keepKey];

                // Erase the ones to keep. This means that we will be left with
                // the ones the replace.
                TValue temp;
                volatileDictionary.TryRemove(keepKey, out temp);
            }
        }

        // Swap the two dictionaries and return the old one. These were deleted.
        IDictionary<TKey, TValue> weRemoved = volatileDictionary;
        volatileDictionary = toKeep;
        return weRemoved;
    }

    public void LazyAdd(IDictionary<TKey, TValue> items)
    {
        foreach (TKey key in items.Keys)
        {
            lazyDictionary[key] = items[key];
        }
    }

    public bool LazyRemove(TKey key)
    {
         TValue temp;
         return volatileDictionary.TryRemove(key, out temp);
    }

    public void LazyRemove(IEnumerable<TKey> keys)
    {
        foreach (TKey key in keys)
        {
            TValue temp;
            volatileDictionary.TryRemove(key, out temp);
        }
    }
}
