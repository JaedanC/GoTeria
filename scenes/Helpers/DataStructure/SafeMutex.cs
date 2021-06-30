using Godot;


public class SafeMutex
{
    private readonly Mutex mutex;
    public bool IsLocked;

    public SafeMutex()
    {
        mutex = new Mutex();
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
