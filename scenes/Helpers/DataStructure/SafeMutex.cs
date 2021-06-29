using Godot;


public class SafeMutex
{
    private Mutex mutex;
    private Mutex mutexMutex;
    public bool IsLocked;

    public SafeMutex()
    {
        mutex = new Mutex();
        mutexMutex = new Mutex();
        IsLocked = false;
    }

    public void Lock()
    {
        mutex.Lock();
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
        mutex.Unlock();
    }
}
